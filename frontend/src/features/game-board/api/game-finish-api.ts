import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameFinishApiClient =
  createApiClient<
    Pick<
      paths,
      '/game/lifecycle/games/{gameId}/finish-preview' | '/game/lifecycle/games/{gameId}/finish'
    >
  >()

export function fetchGameFinishPreview(gameId: string) {
  return unwrapOpenApiData(
    gameFinishApiClient.GET('/game/lifecycle/games/{gameId}/finish-preview', {
      params: { path: { gameId } },
    }),
  )
}

export function finishGame(
  gameId: string,
  input: {
    expectedBoardVersion: number
    requestId: string
    acknowledgedWarningCodes: string[]
    note: string | null
  },
) {
  return unwrapOpenApiData(
    gameFinishApiClient.POST('/game/lifecycle/games/{gameId}/finish', {
      params: { path: { gameId } },
      body: input,
    }),
  )
}
