import {
  createApiClient,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type { components } from '../../../shared/api/contracts/generated'
import type { paths } from '../../../shared/api/contracts/generated'

type StartGameCardRunRequest = components['schemas']['StartGameCardRunRequestDto']
type FinalizeGameCardRunRequest = components['schemas']['FinalizeGameCardRunRequestDto']

const gameCardRunsApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/card-runs'
      | '/game/card-runs/active'
      | '/game/card-runs/{cardRunId}/review'
      | '/game/card-runs/{cardRunId}/finalize'
    >
  >()

export function fetchActiveGameCardRun() {
  return unwrapOpenApiDataOrNullOnNoContent(gameCardRunsApiClient.GET('/game/card-runs/active'))
}

export function startGameCardRun(request: StartGameCardRunRequest) {
  return unwrapOpenApiData(gameCardRunsApiClient.POST('/game/card-runs', { body: request }))
}

export function reviewGameCardRun(cardRunId: string) {
  return unwrapOpenApiData(
    gameCardRunsApiClient.POST('/game/card-runs/{cardRunId}/review', {
      params: { path: { cardRunId } },
    }),
  )
}

export function finalizeGameCardRun(cardRunId: string, request: FinalizeGameCardRunRequest) {
  return unwrapOpenApiData(
    gameCardRunsApiClient.POST('/game/card-runs/{cardRunId}/finalize', {
      params: { path: { cardRunId } },
      body: request,
    }),
  )
}
