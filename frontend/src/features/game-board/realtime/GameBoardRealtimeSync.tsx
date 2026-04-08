import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../../shared/api/query-keys.ts'
import { getBackendOrigin } from '../../../shared/api/config.ts'
import type { GameBoardCell, GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import { logger } from '../../../shared/lib/logger.ts'
import { fetchCurrentGameBoardSnapshot } from '../api/game-board-data-access.ts'

const GAME_BOARD_HUB_URL = `${getBackendOrigin()}/hubs/game-board`
const CELL_OPENED_EVENT = 'cellOpened'

interface CellOpenedEvent {
  gameId: string
  version: number
  cell: GameBoardCell
}

interface CellOpenedPatchResult {
  nextSnapshot: GameBoardSnapshot | null
  requiresResync: boolean
}

function applyCellOpenedEvent(
  current: GameBoardSnapshot | null | undefined,
  event: CellOpenedEvent,
): CellOpenedPatchResult {
  if (!current) {
    return { nextSnapshot: null, requiresResync: true }
  }

  if (current.gameId !== event.gameId || event.version <= current.version) {
    return { nextSnapshot: current, requiresResync: false }
  }

  let updated = false
  const cells = current.cells.map((cell) => {
    if (cell.id !== event.cell.id) {
      return cell
    }

    updated = true
    return event.cell
  })

  if (!updated) {
    return { nextSnapshot: current, requiresResync: true }
  }

  return {
    nextSnapshot: {
      ...current,
      version: event.version,
      cells,
    },
    requiresResync: false,
  }
}

export function GameBoardRealtimeSync() {
  const queryClient = useQueryClient()

  useEffect(() => {
    let disposed = false
    const connection = new HubConnectionBuilder()
      .withUrl(GAME_BOARD_HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .build()

    const syncFromServerIfNewer = async () => {
      const freshSnapshot = await fetchCurrentGameBoardSnapshot().catch((error) => {
        logger.warn('Game board realtime resync failed', error)
        return null
      })
      if (!freshSnapshot) {
        return
      }

      queryClient.setQueryData<GameBoardSnapshot | null>(
        queryKeys.gameBoard.currentSnapshot(),
        (current) => {
          if (!current) {
            return freshSnapshot
          }

          return freshSnapshot.version > current.version ? freshSnapshot : current
        },
      )
    }

    connection.onreconnecting((error) => {
      logger.warn('Game board realtime reconnecting', error)
    })

    connection.on(CELL_OPENED_EVENT, (event: CellOpenedEvent) => {
      logger.debug('Game board realtime event received', event)
      queryClient.setQueryData<GameBoardSnapshot | null>(
        queryKeys.gameBoard.currentSnapshot(),
        (current) => {
          const patchResult = applyCellOpenedEvent(current, event)
          if (patchResult.requiresResync) {
            void syncFromServerIfNewer()
          }

          return patchResult.nextSnapshot ?? null
        },
      )
    })

    connection.onreconnected(() => {
      logger.info('Game board realtime reconnected')
      return syncFromServerIfNewer()
    })
    connection.onclose((error) => {
      if (!disposed && error) {
        logger.warn('Game board realtime connection closed', error)
      }
    })

    const startPromise = (async () => {
      try {
        await connection.start()
        if (disposed) {
          await connection.stop()
          return
        }

        logger.info('Game board realtime connected')
        await syncFromServerIfNewer()
      } catch (error) {
        if (disposed || isExpectedNegotiationShutdown(error)) {
          return
        }

        logger.error('Game board realtime failed to start', error)
      }
    })()

    return () => {
      disposed = true
      connection.off(CELL_OPENED_EVENT)
      // React StrictMode can unmount while start() is still negotiating.
      // Await startup first to avoid stop() interrupting negotiate.
      void (async () => {
        await startPromise.catch(() => undefined)
        if (connection.state !== HubConnectionState.Disconnected) {
          await connection.stop()
        }
      })()
    }
  }, [queryClient])

  return null
}

function isExpectedNegotiationShutdown(error: unknown): boolean {
  if (!(error instanceof Error)) {
    return false
  }

  const message = error.message.toLowerCase()
  return (
    message.includes('stopped during negotiation')
    || message.includes('connection was stopped')
  )
}
