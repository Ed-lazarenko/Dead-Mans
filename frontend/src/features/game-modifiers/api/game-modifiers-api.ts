import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameModifiersApiClient =
  createApiClient<Pick<paths, '/game/modifiers/catalog' | '/game/history/users/{userId}'>>()

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/catalog'))
}

export function fetchUserGameHistory(userId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.GET('/game/history/users/{userId}', {
      params: { path: { userId } },
    }),
  )
}
