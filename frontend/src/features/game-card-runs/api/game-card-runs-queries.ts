import { queryOptions } from '@tanstack/react-query'
import { fetchActiveGameCardRun, fetchGameCardRunEligibleTeams } from './game-card-runs-api.ts'

const gameCardRunQueryKeys = {
  all: ['gameCardRuns'] as const,
  active: () => [...gameCardRunQueryKeys.all, 'active'] as const,
  eligibleTeams: () => [...gameCardRunQueryKeys.all, 'eligibleTeams'] as const,
}

export const activeGameCardRunQueryOptions = queryOptions({
  queryKey: gameCardRunQueryKeys.active(),
  queryFn: fetchActiveGameCardRun,
})

export const gameCardRunEligibleTeamsQueryOptions = queryOptions({
  queryKey: gameCardRunQueryKeys.eligibleTeams(),
  queryFn: fetchGameCardRunEligibleTeams,
})
