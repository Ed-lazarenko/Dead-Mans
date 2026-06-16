import { apiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'

export function fetchGameQuestionHistory(gameId: string) {
  return unwrapOpenApiData(
    apiClient.GET('/game/questions/games/{gameId}/history', {
      params: { path: { gameId } },
    }),
  )
}
