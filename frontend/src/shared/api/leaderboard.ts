import { httpClient } from './client/httpClient.ts'
import type { LeaderboardSummary, TeamId } from './contracts/index.ts'

export interface UpdateLeaderboardTeamScoreInput {
  teamId: TeamId
  scoreDelta?: number
  penaltyDelta?: number
}

export interface LeaderboardApi {
  getLeaderboard: () => Promise<LeaderboardSummary>
  updateTeamScore: (input: UpdateLeaderboardTeamScoreInput) => Promise<LeaderboardSummary>
}

export const leaderboardApi: LeaderboardApi = {
  getLeaderboard: () => httpClient.get<LeaderboardSummary>('/leaderboard'),
  updateTeamScore: (input) =>
    Promise.reject(new Error(`Leaderboard score updates are not implemented over HTTP yet for ${input.teamId}`)),
}
