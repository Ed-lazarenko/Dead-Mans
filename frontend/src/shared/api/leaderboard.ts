import { httpClient } from './client/httpClient.ts'
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

function shouldUseMockApi() {
  return (import.meta.env.VITE_API_MODE ?? 'mock') !== 'http'
}

export const leaderboardApi: LeaderboardApi = {
  getLeaderboard: () =>
    shouldUseMockApi()
      ? leaderboardMockApi.getLeaderboard()
      : httpClient.get<LeaderboardSummary>('/leaderboard'),
  updateTeamScore: (input) =>
    shouldUseMockApi()
      ? leaderboardMockApi.updateTeamScore(input)
      : Promise.reject(new Error(`Leaderboard score updates are not implemented over HTTP yet for ${input.teamId}`)),
}
