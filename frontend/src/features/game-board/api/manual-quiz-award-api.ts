import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const manualQuizAwardApiClient =
  createApiClient<
    Pick<paths, '/game/questions/manual-awards' | '/game/questions/manual-awards/players'>
  >()

export interface ManualQuizAwardInput {
  awardedToUserId: string
  points: number
}

export function awardManualQuizPoints(input: ManualQuizAwardInput) {
  return unwrapOpenApiData(
    manualQuizAwardApiClient.POST('/game/questions/manual-awards', {
      body: input,
    }),
  )
}

export function fetchManualQuizAwardPlayers() {
  return unwrapOpenApiData(manualQuizAwardApiClient.GET('/game/questions/manual-awards/players'))
}
