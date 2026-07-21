import { queryOptions } from '@tanstack/react-query'
import { fetchGameHistoryGameDetails } from './game-history-api.ts'

export const gameHistoryQueryKeys = {
  all: ['gameHistory'] as const,
  gameDetails: (gameId: string) => [...gameHistoryQueryKeys.all, 'games', gameId] as const,
  userHistory: (userId: string) => [...gameHistoryQueryKeys.all, 'users', userId] as const,
}

export const gameHistoryGameDetailsQueryOptions = (gameId: string) =>
  queryOptions({
    queryKey: gameHistoryQueryKeys.gameDetails(gameId),
    queryFn: () => fetchGameHistoryGameDetails(gameId),
  })
