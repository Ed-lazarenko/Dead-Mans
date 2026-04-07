import { useEffect } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../../shared/api/query-keys.ts'
import { getBackendOrigin } from '../../../shared/api/config.ts'
import type { GameBoardCell, GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import { fetchCurrentGameBoardSnapshot } from '../api/game-board-data-access.ts'

const GAME_BOARD_HUB_URL = `${getBackendOrigin()}/hubs/game-board`
const CELL_OPENED_EVENT = 'cellOpened'

interface CellOpenedEvent {
  gameId: string
  version: number
  cell: GameBoardCell
}

function patchSnapshotWithOpenedCell(
  current: GameBoardSnapshot,
  event: CellOpenedEvent,
): GameBoardSnapshot {
  if (current.gameId !== event.gameId || event.version <= current.version) {
    return current
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
    return current
  }

  return {
    ...current,
    version: event.version,
    cells,
  }
}

export function GameBoardRealtimeSync() {
  const queryClient = useQueryClient()

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(GAME_BOARD_HUB_URL, { withCredentials: true })
      .withAutomaticReconnect()
      .build()

    const syncFromServerIfNewer = async () => {
      const freshSnapshot = await fetchCurrentGameBoardSnapshot().catch(() => null)
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

    connection.on(CELL_OPENED_EVENT, (event: CellOpenedEvent) => {
      queryClient.setQueryData<GameBoardSnapshot | null>(
        queryKeys.gameBoard.currentSnapshot(),
        (current) => {
          if (!current) {
            return current
          }

          return patchSnapshotWithOpenedCell(current, event)
        },
      )
    })

    connection.onreconnected(() => syncFromServerIfNewer())
    void connection.start().then(syncFromServerIfNewer).catch(() => undefined)

    return () => {
      connection.off(CELL_OPENED_EVENT)
      void connection.stop()
    }
  }, [queryClient])

  return null
}
