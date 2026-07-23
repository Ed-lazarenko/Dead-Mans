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

export function fetchGameHistoryGameDetails(gameId: string) {
  return unwrapOpenApiData(
    gameHistoryApiClient.GET('/game/history/games/{gameId}', {
      params: { path: { gameId } },
    }),
  )
}

export function fetchGameHistoryGames() {
  return unwrapOpenApiData(gameHistoryApiClient.GET('/game/history/games'))
}
