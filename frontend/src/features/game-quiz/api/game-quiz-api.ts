import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameQuizApiClient = createApiClient<Pick<paths, '/game/questions/games/{gameId}/history'>>()

export function fetchGameQuestionHistory(gameId: string) {
  return unwrapOpenApiData(
    gameQuizApiClient.GET('/game/questions/games/{gameId}/history', {
      params: { path: { gameId } },
    }),
  )
}
