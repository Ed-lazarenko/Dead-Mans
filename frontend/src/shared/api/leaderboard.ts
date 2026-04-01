import { httpClient } from './client/httpClient.ts'
import type { LeaderboardSummary } from './contracts/index.ts'

export interface LeaderboardApi {
  getLeaderboard: () => Promise<LeaderboardSummary>
}

export const leaderboardApi: LeaderboardApi = {
  getLeaderboard: () => httpClient.get<LeaderboardSummary>('/leaderboard'),
}
