import { useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { gameModifierCatalogQueryOptions } from '../game-modifiers/api/game-modifier-queries.ts'
import { gameSetupDraftQueryOptions } from '../game-setup/api/game-setup-queries.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../shared/realtime/index.ts'
import { modifierHistoryRootQueryOptions } from './api/modifier-history-queries.ts'

const EVENT = realtimeHubs.gameBoard.events.modifierCatalogChanged

export function ModifierCatalogRealtimeSync() {
  const queryClient = useQueryClient()
  const invalidate = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: gameModifierCatalogQueryOptions.queryKey }),
      queryClient.invalidateQueries({ queryKey: modifierHistoryRootQueryOptions.queryKey }),
      queryClient.invalidateQueries({ queryKey: gameSetupDraftQueryOptions.queryKey }),
    ])
  }, [queryClient])

  const registerEventHandlers = useCallback(
    (connection: {
      on: (eventName: string, handler: () => void) => void
      off: (eventName: string, handler: () => void) => void
    }) => {
      const handler = () => void invalidate()
      connection.on(EVENT, handler)
      return () => connection.off(EVENT, handler)
    },
    [invalidate],
  )

  useSignalrHubLifecycle({
    hub: 'gameBoard',
    logLabel: 'Modifier catalog',
    registerEventHandlers,
  })
  return null
}
