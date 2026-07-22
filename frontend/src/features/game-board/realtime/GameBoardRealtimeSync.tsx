import { useCallback } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import { logger } from '../../../shared/lib/logger.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../../shared/realtime/index.ts'
import { activeGameCardRunQueryOptions } from '../../game-card-runs/api/game-card-runs-queries.ts'
import { gameModifierQueryKeys } from '../../game-modifiers/api/game-modifier-queries.ts'
import { fetchCurrentGameBoardSnapshot } from '../api/game-board-data-access.ts'
import { currentGameBoardQueryOptions } from '../api/game-board-queries.ts'
import {
  applyCellOpenedEvent,
  applyModifierActivationCancelledEvent,
  applyModifierActivatedEvent,
  selectNewerGameBoardSnapshot,
  type CellOpenedEvent,
  type ModifierActivationCancelledEvent,
  type ModifierActivatedEvent,
} from './game-board-realtime-model.ts'

const CELL_OPENED_EVENT = realtimeHubs.gameBoard.events.cellOpened
const CARD_RUN_STATE_CHANGED_EVENT = realtimeHubs.gameBoard.events.cardRunStateChanged
const MODIFIER_ACTIVATED_EVENT = realtimeHubs.gameBoard.events.modifierActivated
const MODIFIER_CANCELLED_EVENT = realtimeHubs.gameBoard.events.modifierActivationCancelled

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
        void queryClient.invalidateQueries({ queryKey: activeGameCardRunQueryOptions.queryKey })
        void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
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

      const handleCardRunStateChanged = () => {
        logger.debug('Game board round state realtime event received')
        void queryClient.invalidateQueries({ queryKey: activeGameCardRunQueryOptions.queryKey })
        void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
        void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      }

      const handleModifierActivated = (event: ModifierActivatedEvent) => {
        logger.debug('Game board modifier realtime event received', event)
        void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
        queryClient.setQueryData<GameBoardSnapshot | null>(
          currentGameBoardQueryOptions.queryKey,
          (current) => {
            const patchResult = applyModifierActivatedEvent(current, event)
            if (patchResult.requiresResync) {
              void syncFromServerIfNewer()
            }

            return patchResult.nextSnapshot ?? null
          },
        )
      }

      const handleModifierCancelled = (event: ModifierActivationCancelledEvent) => {
        logger.debug('Game board modifier cancel realtime event received', event)
        void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
        queryClient.setQueryData<GameBoardSnapshot | null>(
          currentGameBoardQueryOptions.queryKey,
          (current) => {
            const patchResult = applyModifierActivationCancelledEvent(current, event)
            if (patchResult.requiresResync) {
              void syncFromServerIfNewer()
            }

            return patchResult.nextSnapshot ?? null
          },
        )
      }

      connection.on(CELL_OPENED_EVENT, handleCellOpened)
      connection.on(CARD_RUN_STATE_CHANGED_EVENT, handleCardRunStateChanged)
      connection.on(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
      connection.on(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)

      return () => {
        connection.off(CELL_OPENED_EVENT, handleCellOpened)
        connection.off(CARD_RUN_STATE_CHANGED_EVENT, handleCardRunStateChanged)
        connection.off(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
        connection.off(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)
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
