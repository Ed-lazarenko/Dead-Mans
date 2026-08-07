import {
  createApiClient,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type { components } from '../../../shared/api/contracts/generated'
import type { paths } from '../../../shared/api/contracts/generated'

type StartGameRoundRequest = components['schemas']['StartGameRoundRequestDto']
type FinalizeGameRoundRequest = components['schemas']['FinalizeGameRoundRequestDto']

const gameRoundsApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/rounds'
      | '/game/rounds/active'
      | '/game/rounds/{roundId}/review'
      | '/game/rounds/{roundId}/finalize'
    >
  >()

export function fetchActiveGameRound() {
  return unwrapOpenApiDataOrNullOnNoContent(gameRoundsApiClient.GET('/game/rounds/active'))
}

export function startGameRound(request: StartGameRoundRequest) {
  return unwrapOpenApiData(gameRoundsApiClient.POST('/game/rounds', { body: request }))
}

export function reviewGameRound(roundId: string) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/review', {
      params: { path: { roundId } },
    }),
  )
}

export function finalizeGameRound(roundId: string, request: FinalizeGameRoundRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/finalize', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}
