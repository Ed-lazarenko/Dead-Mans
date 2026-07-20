import { useCallback } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import { logger } from '../../../shared/lib/logger.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../../shared/realtime/index.ts'
import { gameModifierQueryKeys } from '../../game-modifiers/api/game-modifier-queries.ts'
import { fetchCurrentGameBoardSnapshot } from '../api/game-board-data-access.ts'
import { currentGameBoardQueryOptions } from '../api/game-board-queries.ts'
import {
  applyCellOpenedEvent,
  selectNewerGameBoardSnapshot,
  type CellOpenedEvent,
  type ModifierActivatedEvent,
} from './game-board-realtime-model.ts'

const CELL_OPENED_EVENT = realtimeHubs.gameBoard.events.cellOpened
const MODIFIER_ACTIVATED_EVENT = realtimeHubs.gameBoard.events.modifierActivated

export function GameBoardRealtimeSync() {
  const queryClient = useQueryClient()

  const syncFromServerIfNewer = useCallback(async () => {
    const freshSnapshot = await fetchCurrentGameBoardSnapshot().catch((error) => {
      logger.warn('Game board realtime resync failed', error)
      return null
    })
    if (!freshSnapshot) {
      return
    }

    queryClient.setQueryData<GameBoardSnapshot | null>(
      currentGameBoardQueryOptions.queryKey,
      (current) => selectNewerGameBoardSnapshot(current, freshSnapshot),
    )
  }, [queryClient])

  const registerEventHandlers = useCallback(
    (connection: HubConnection) => {
      const handleCellOpened = (event: CellOpenedEvent) => {
        logger.debug('Game board realtime event received', event)
        queryClient.setQueryData<GameBoardSnapshot | null>(
          currentGameBoardQueryOptions.queryKey,
          (current) => {
            const patchResult = applyCellOpenedEvent(current, event)
            if (patchResult.requiresResync) {
              void syncFromServerIfNewer()
            }

            return patchResult.nextSnapshot ?? null
          },
        )
      }

      const handleModifierActivated = (event: ModifierActivatedEvent) => {
        logger.debug('Game board modifier realtime event received', event)
        void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
        void syncFromServerIfNewer()
      }

      connection.on(CELL_OPENED_EVENT, handleCellOpened)
      connection.on(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)

      return () => {
        connection.off(CELL_OPENED_EVENT, handleCellOpened)
        connection.off(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
      }
    },
    [queryClient, syncFromServerIfNewer],
  )

  useSignalrHubLifecycle({
    hub: 'gameBoard',
    logLabel: 'Game board',
    onConnected: syncFromServerIfNewer,
    registerEventHandlers,
  })

  return null
}
