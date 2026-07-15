import { useCallback, useRef } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { logger } from '../../../shared/lib/logger.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../../shared/realtime/index.ts'
import { gameSetupDraftQueryOptions } from '../api/game-setup-queries.ts'
import {
  loadGameSetupDraftQueryState,
  type LoadedGameSetupDraftState,
} from '../model/game-setup-query-state.ts'
import { selectNewerGameSetupState } from './game-setup-realtime-model.ts'

const DRAFT_CHANGED_EVENT = realtimeHubs.gameSetup.events.draftChanged

export function GameSetupRealtimeSync() {
  const queryClient = useQueryClient()
  const latestRequestIdRef = useRef(0)

  const syncFromServer = useCallback(async () => {
    const requestId = latestRequestIdRef.current + 1
    latestRequestIdRef.current = requestId

    const loaded = await loadGameSetupDraftQueryState(requestId).catch((error) => {
      logger.warn('Game setup realtime resync failed', error)
      return null
    })
    if (!loaded) {
      return
    }

    queryClient.setQueryData<LoadedGameSetupDraftState>(
      gameSetupDraftQueryOptions.queryKey,
      (current) => selectNewerGameSetupState(current, loaded),
    )
  }, [queryClient])

  const registerEventHandlers = useCallback(
    (connection: HubConnection) => {
      const handleDraftChanged = () => {
        logger.debug('Game setup realtime event received')
        void syncFromServer()
      }

      connection.on(DRAFT_CHANGED_EVENT, handleDraftChanged)

      return () => {
        connection.off(DRAFT_CHANGED_EVENT, handleDraftChanged)
      }
    },
    [syncFromServer],
  )

  useSignalrHubLifecycle({
    hub: 'gameSetup',
    logLabel: 'Game setup',
    onConnected: syncFromServer,
    registerEventHandlers,
  })

  return null
}
