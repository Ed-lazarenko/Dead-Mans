import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameHistoryApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/history/leaderboard'
      | '/game/history/games'
      | '/game/history/games/{gameId}'
      | '/game/history/users/{userId}'
    >
  >()

export function fetchUserGameHistory(userId: string) {
  return unwrapOpenApiData(
    gameHistoryApiClient.GET('/game/history/users/{userId}', {
      params: { path: { userId } },
    }),
  )
}
