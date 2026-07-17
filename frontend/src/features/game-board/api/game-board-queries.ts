import { queryOptions } from '@tanstack/react-query'
import {
  fetchCurrentGameBoardSnapshot,
  fetchCurrentGameTeamQueue,
} from './game-board-data-access.ts'
import { fetchManualQuizAwardPlayers } from './manual-quiz-award-api.ts'

const gameBoardQueryKeys = {
  all: ['gameBoard'] as const,
  currentSnapshot: () => [...gameBoardQueryKeys.all, 'currentSnapshot'] as const,
  currentTeamQueue: () => [...gameBoardQueryKeys.all, 'currentTeamQueue'] as const,
  manualQuizAwardPlayers: () => [...gameBoardQueryKeys.all, 'manualQuizAwardPlayers'] as const,
}

export const currentGameBoardQueryOptions = queryOptions({
  queryKey: gameBoardQueryKeys.currentSnapshot(),
  queryFn: fetchCurrentGameBoardSnapshot,
})

export const currentGameTeamQueueQueryOptions = queryOptions({
  queryKey: gameBoardQueryKeys.currentTeamQueue(),
  queryFn: fetchCurrentGameTeamQueue,
  staleTime: 0,
  refetchInterval: 5_000,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})

export const manualGameQuestionAwardPlayersQueryOptions = queryOptions({
  queryKey: gameBoardQueryKeys.manualQuizAwardPlayers(),
  queryFn: fetchManualQuizAwardPlayers,
  staleTime: 30_000,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})
