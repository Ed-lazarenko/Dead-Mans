import { useCallback } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { gameHistoryQueryKeys } from '../game-history/api/game-history-queries.ts'
import {
  currentGameBoardQueryOptions,
  manualGameQuizAwardPlayersQueryOptions,
} from '../game-board/index.ts'
import { realtimeHubs, useSignalrHubLifecycle } from '../../shared/realtime/index.ts'

const QUIZ_STATE_CHANGED_EVENT = realtimeHubs.gameBoard.events.quizStateChanged

export function GameQuizRealtimeSync() {
  const queryClient = useQueryClient()

  const syncQuizState = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey }),
      queryClient.invalidateQueries({ queryKey: gameHistoryQueryKeys.all }),
      queryClient.invalidateQueries({
        queryKey: manualGameQuizAwardPlayersQueryOptions.queryKey,
      }),
    ])
  }, [queryClient])

  const registerEventHandlers = useCallback(
    (connection: HubConnection) => {
      const handleQuizStateChanged = () => {
        void syncQuizState()
      }

      connection.on(QUIZ_STATE_CHANGED_EVENT, handleQuizStateChanged)

      return () => {
        connection.off(QUIZ_STATE_CHANGED_EVENT, handleQuizStateChanged)
      }
    },
    [syncQuizState],
  )

  useSignalrHubLifecycle({
    hub: 'gameBoard',
    logLabel: 'Game quiz',
    onConnected: syncQuizState,
    registerEventHandlers,
  })

  return null
}
