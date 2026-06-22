import type { GameBoardCellId } from '../../../shared/api/contracts/index.ts'
import {
  apiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiDataOrNullOn404,
} from '../../../shared/api/client/openApiClient.ts'

export async function fetchCurrentGameBoardSnapshot() {
  return unwrapOpenApiDataOrNullOn404(apiClient.GET('/game'))
}

export async function openGameBoardCell(cellId: GameBoardCellId): Promise<void> {
  await ensureOpenApiSuccess(
    apiClient.POST('/game/cells/{cellId}/open', {
      params: {
        path: { cellId },
      },
    }),
  )
}
