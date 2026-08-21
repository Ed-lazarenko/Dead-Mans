import {
  createApiClient,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type { components } from '../../../shared/api/contracts/generated'
import type { paths } from '../../../shared/api/contracts/generated'

type FinalizeGameRoundRequest = components['schemas']['FinalizeGameRoundRequestDto']
type GameRoundVersionCommandRequest = components['schemas']['GameRoundVersionCommandRequestDto']
type TechnicalCancelGameRoundRequest = components['schemas']['TechnicalCancelGameRoundRequestDto']

const gameRoundsApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/rounds/active'
      | '/game/rounds/{roundId}/review'
      | '/game/rounds/{roundId}/prepare'
      | '/game/rounds/{roundId}/begin-gameplay'
      | '/game/rounds/{roundId}/rebuild'
      | '/game/rounds/{roundId}/technical-cancel'
      | '/game/rounds/{roundId}/finalize'
      | '/game/rounds/{roundId}/score-preview'
    >
  >()

export function fetchActiveGameRound() {
  return unwrapOpenApiDataOrNullOnNoContent(gameRoundsApiClient.GET('/game/rounds/active'))
}

export function prepareGameRound(roundId: string, request: GameRoundVersionCommandRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/prepare', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}

export function beginGameRoundGameplay(roundId: string, request: GameRoundVersionCommandRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/begin-gameplay', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}

export function rebuildGameRound(roundId: string, request: GameRoundVersionCommandRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/rebuild', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}

export function technicalCancelGameRound(
  roundId: string,
  request: TechnicalCancelGameRoundRequest,
) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/technical-cancel', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}

export function reviewGameRound(roundId: string, request: GameRoundVersionCommandRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/review', {
      params: { path: { roundId } },
      body: request,
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

export function previewGameRoundScore(roundId: string, request: FinalizeGameRoundRequest) {
  return unwrapOpenApiData(
    gameRoundsApiClient.POST('/game/rounds/{roundId}/score-preview', {
      params: { path: { roundId } },
      body: request,
    }),
  )
}
