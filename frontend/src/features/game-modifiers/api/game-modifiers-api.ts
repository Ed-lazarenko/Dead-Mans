import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameModifiersApiClient = createApiClient<Pick<paths, '/game/modifiers/catalog'>>()

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/catalog'))
}
