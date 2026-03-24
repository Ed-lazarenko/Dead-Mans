import { httpClient } from './client/httpClient.ts'
import { isHttpApiMode } from './config.ts'
import type { LeaderboardSummary, TeamId } from './contracts/index.ts'
import * as leaderboardMockApi from './mocks/leaderboardMock.ts'

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
  getLeaderboard: () =>
    isHttpApiMode()
      ? httpClient.get<LeaderboardSummary>('/leaderboard')
      : leaderboardMockApi.getLeaderboard(),
  updateTeamScore: (input) =>
    isHttpApiMode()
      ? Promise.reject(new Error(`Leaderboard score updates are not implemented over HTTP yet for ${input.teamId}`))
      : leaderboardMockApi.updateTeamScore(input),
}
