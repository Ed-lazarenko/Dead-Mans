import { leaderboardApi } from '../../../shared/api/leaderboard.ts'

export function getLeaderboardSummary() {
  return leaderboardApi.getLeaderboard()
}
