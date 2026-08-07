import { useCallback } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { logger } from '../../../shared/lib/logger.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../../shared/realtime/index.ts'
import { gameModifierQueryKeys } from '../api/game-modifier-queries.ts'

const MODIFIER_ACTIVATED_EVENT = realtimeHubs.gameBoard.events.modifierActivated
const MODIFIER_CANCELLED_EVENT = realtimeHubs.gameBoard.events.modifierActivationCancelled
const CELL_OPENED_EVENT = realtimeHubs.gameBoard.events.cellOpened
const CARD_RUN_STATE_CHANGED_EVENT = realtimeHubs.gameBoard.events.roundStateChanged

export function GameModifiersRealtimeSync() {
  const queryClient = useQueryClient()

  const syncState = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
  }, [queryClient])

  const registerEventHandlers = useCallback(
    (connection: HubConnection) => {
      const handleModifierActivated = () => {
        logger.debug('Game modifiers realtime event received')
        void syncState()
      }

      const handleModifierCancelled = () => {
        logger.debug('Game modifiers cancel realtime event received')
        void syncState()
      }

      const handleCellOpened = () => {
        logger.debug('Game modifiers cell opened realtime event received')
        void syncState()
      }

      const handleRoundStateChanged = () => {
        logger.debug('Game modifiers round state realtime event received')
        void syncState()
      }

      connection.on(CELL_OPENED_EVENT, handleCellOpened)
      connection.on(CARD_RUN_STATE_CHANGED_EVENT, handleRoundStateChanged)
      connection.on(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
      connection.on(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)

      return () => {
        connection.off(CELL_OPENED_EVENT, handleCellOpened)
        connection.off(CARD_RUN_STATE_CHANGED_EVENT, handleRoundStateChanged)
        connection.off(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
        connection.off(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)
      }
    },
    [syncState],
  )

  useSignalrHubLifecycle({
    hub: 'gameBoard',
    logLabel: 'Game modifiers',
    onConnected: syncState,
    registerEventHandlers,
  })

  return null
}
