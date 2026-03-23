import * as leaderboardMockApi from './leaderboardMock.ts'
import type { LeaderboardSummary, TeamId } from './contracts.ts'

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
  getLeaderboard: () => leaderboardMockApi.getLeaderboard(),
  updateTeamScore: (input) => leaderboardMockApi.updateTeamScore(input),
}
