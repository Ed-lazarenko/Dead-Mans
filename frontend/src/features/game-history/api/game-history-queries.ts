import { queryOptions } from '@tanstack/react-query'
import { fetchGameHistoryGameDetails, fetchGameHistoryGames } from './game-history-api.ts'

export const gameHistoryQueryKeys = {
  all: ['gameHistory'] as const,
  games: () => [...gameHistoryQueryKeys.all, 'games'] as const,
  gameDetails: (gameId: string) => [...gameHistoryQueryKeys.all, 'games', gameId] as const,
  userHistory: (userId: string) => [...gameHistoryQueryKeys.all, 'users', userId] as const,
}

export const gameHistoryGamesQueryOptions = queryOptions({
  queryKey: gameHistoryQueryKeys.games(),
  queryFn: fetchGameHistoryGames,
})

export const gameHistoryGameDetailsQueryOptions = (gameId: string) =>
  queryOptions({
    queryKey: gameHistoryQueryKeys.gameDetails(gameId),
    queryFn: () => fetchGameHistoryGameDetails(gameId),
  })
