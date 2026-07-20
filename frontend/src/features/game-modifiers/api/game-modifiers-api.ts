import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameModifiersApiClient =
  createApiClient<
    Pick<
      paths,
      '/game/modifiers/catalog' | '/game/modifiers/state' | '/game/modifiers/{modifierId}/activate'
    >
  >()

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/catalog'))
}

export function fetchGameModifierState() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/state'))
}

export function activateGameModifier(modifierId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/{modifierId}/activate', {
      params: { path: { modifierId } },
    }),
  )
}
