import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import { getLeaderboardSummary } from './api/leaderboardDataAccess.ts'

export function useLeaderboardPage() {
  const query = useQuery({
    queryKey: queryKeys.leaderboard.summary(),
    queryFn: getLeaderboardSummary,
    select: (summary) => ({
      ...summary,
      teams: summary.teams.map((team) => ({
        ...team,
        total: team.score - team.penalty,
      })),
    }),
  })

  return query
}


