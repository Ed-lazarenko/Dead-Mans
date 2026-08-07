import { queryOptions } from '@tanstack/react-query'
import { fetchActiveGameRound } from './game-rounds-api.ts'

const gameRoundQueryKeys = {
  all: ['gameRounds'] as const,
  active: () => [...gameRoundQueryKeys.all, 'active'] as const,
}

export const activeGameRoundQueryOptions = queryOptions({
  queryKey: gameRoundQueryKeys.active(),
  queryFn: fetchActiveGameRound,
})
