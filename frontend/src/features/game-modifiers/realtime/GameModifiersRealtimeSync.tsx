import { useCallback } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { logger } from '../../../shared/lib/logger.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../../shared/realtime/index.ts'
import { currentGameBoardQueryOptions } from '../../game-board/index.ts'
import { activeGameRoundQueryOptions } from '../../game-rounds/api/game-rounds-queries.ts'
import { gameModifierQueryKeys } from '../api/game-modifier-queries.ts'

const MODIFIER_ACTIVATED_EVENT = realtimeHubs.gameBoard.events.modifierActivated
const MODIFIER_CANCELLED_EVENT = realtimeHubs.gameBoard.events.modifierActivationCancelled
const MODIFIER_AVAILABILITY_CHANGED_EVENT =
  realtimeHubs.gameBoard.events.modifierAvailabilityChanged
const CELL_OPENED_EVENT = realtimeHubs.gameBoard.events.cellOpened
const ROUND_STATE_CHANGED_EVENT = realtimeHubs.gameBoard.events.roundStateChanged

export function GameModifiersRealtimeSync() {
  const queryClient = useQueryClient()

  const syncState = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
  }, [queryClient])

  const syncRoundContext = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey }),
      queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey }),
    ])
  }, [queryClient])

  const syncAll = useCallback(async () => {
    await Promise.all([syncState(), syncRoundContext()])
  }, [syncRoundContext, syncState])

  const registerEventHandlers = useCallback(
    (connection: HubConnection) => {
      const handleModifierActivated = () => {
        logger.debug('Game modifiers realtime event received')
        void syncAll()
      }

      const handleModifierCancelled = () => {
        logger.debug('Game modifiers cancel realtime event received')
        void syncAll()
      }

      const handleModifierAvailabilityChanged = () => {
        logger.debug('Game modifier availability realtime event received')
        void syncAll()
      }

      const handleCellOpened = () => {
        logger.debug('Game modifiers cell opened realtime event received')
        void syncAll()
      }

      const handleRoundStateChanged = () => {
        logger.debug('Game modifiers round state realtime event received')
        void syncAll()
      }

      connection.on(CELL_OPENED_EVENT, handleCellOpened)
      connection.on(ROUND_STATE_CHANGED_EVENT, handleRoundStateChanged)
      connection.on(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
      connection.on(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)
      connection.on(MODIFIER_AVAILABILITY_CHANGED_EVENT, handleModifierAvailabilityChanged)

      return () => {
        connection.off(CELL_OPENED_EVENT, handleCellOpened)
        connection.off(ROUND_STATE_CHANGED_EVENT, handleRoundStateChanged)
        connection.off(MODIFIER_ACTIVATED_EVENT, handleModifierActivated)
        connection.off(MODIFIER_CANCELLED_EVENT, handleModifierCancelled)
        connection.off(MODIFIER_AVAILABILITY_CHANGED_EVENT, handleModifierAvailabilityChanged)
      }
    },
    [syncAll],
  )

  useSignalrHubLifecycle({
    hub: 'gameBoard',
    logLabel: 'Game modifiers',
    onConnected: syncAll,
    registerEventHandlers,
  })

  return null
}
