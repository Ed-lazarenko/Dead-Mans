import { apiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(apiClient.GET('/game/modifiers/catalog'))
}

export function fetchUserGameHistory(userId: string) {
  return unwrapOpenApiData(
    apiClient.GET('/game/history/users/{userId}', {
      params: { path: { userId } },
    }),
  )
}
