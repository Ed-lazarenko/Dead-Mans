import {
  createApiClient,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameCardRunsApiClient = createApiClient<Pick<paths, '/game/card-runs/active'>>()

export function fetchActiveGameCardRun() {
  return unwrapOpenApiDataOrNullOnNoContent(gameCardRunsApiClient.GET('/game/card-runs/active'))
}
