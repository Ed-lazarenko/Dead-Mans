import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const manualQuizAwardApiClient =
  createApiClient<Pick<paths, '/game/quiz/manual-awards' | '/game/quiz/manual-awards/players'>>()

export interface ManualQuizAwardInput {
  awardedToUserId: string
  points: number
}

export function awardManualQuizPoints(input: ManualQuizAwardInput) {
  return unwrapOpenApiData(
    manualQuizAwardApiClient.POST('/game/quiz/manual-awards', {
      body: input,
    }),
  )
}

export function fetchManualQuizAwardPlayers() {
  return unwrapOpenApiData(manualQuizAwardApiClient.GET('/game/quiz/manual-awards/players'))
}
