import { queryOptions } from '@tanstack/react-query'
import { fetchActiveGameCardRun } from './game-card-runs-api.ts'

const gameCardRunQueryKeys = {
  all: ['gameCardRuns'] as const,
  active: () => [...gameCardRunQueryKeys.all, 'active'] as const,
}

export const activeGameCardRunQueryOptions = queryOptions({
  queryKey: gameCardRunQueryKeys.active(),
  queryFn: fetchActiveGameCardRun,
})
