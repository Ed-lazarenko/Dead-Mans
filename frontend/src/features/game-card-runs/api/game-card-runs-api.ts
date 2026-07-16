import {
  createApiClient,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameCardRunsApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/card-runs'
      | '/game/card-runs/active'
      | '/game/card-runs/teams'
      | '/game/card-runs/{cardRunId}/finalize'
    >
  >()

export function fetchActiveGameCardRun() {
  return unwrapOpenApiDataOrNullOnNoContent(gameCardRunsApiClient.GET('/game/card-runs/active'))
}

export function fetchGameCardRunEligibleTeams() {
  return unwrapOpenApiData(gameCardRunsApiClient.GET('/game/card-runs/teams'))
}

export function startGameCardRun(input: { cellId: string; teamId: string }) {
  return unwrapOpenApiData(
    gameCardRunsApiClient.POST('/game/card-runs', {
      body: input,
    }),
  )
}

export function finalizeGameCardRun(
  cardRunId: string,
  input: {
    status: string
    finalScore?: number
    notes?: string
  },
) {
  return unwrapOpenApiData(
    gameCardRunsApiClient.POST('/game/card-runs/{cardRunId}/finalize', {
      params: {
        path: { cardRunId },
      },
      body: {
        status: input.status,
        ...(input.finalScore !== undefined ? { finalScore: input.finalScore } : {}),
        ...(input.notes ? { notes: input.notes } : {}),
      },
    }),
  )
}
