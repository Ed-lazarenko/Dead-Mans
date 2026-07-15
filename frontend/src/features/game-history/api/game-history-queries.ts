import { queryOptions } from '@tanstack/react-query'
import { fetchUserGameHistory } from './game-history-api.ts'

const gameHistoryQueryKeys = {
  all: ['gameHistory'] as const,
  userHistory: (userId: string) => [...gameHistoryQueryKeys.all, 'users', userId] as const,
}

export const userGameHistoryQueryOptions = (userId: string) =>
  queryOptions({
    queryKey: gameHistoryQueryKeys.userHistory(userId),
    queryFn: () => fetchUserGameHistory(userId),
  })
