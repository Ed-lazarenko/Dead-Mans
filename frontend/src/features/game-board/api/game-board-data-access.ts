import type { GameBoardCellId } from '../../../shared/api/contracts/index.ts'
import {
  createApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiDataOrNullOn404,
} from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameBoardApiClient = createApiClient<Pick<paths, '/game' | '/game/cells/{cellId}/open'>>()

export async function fetchCurrentGameBoardSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameBoardApiClient.GET('/game'))
}

export async function openGameBoardCell(cellId: GameBoardCellId): Promise<void> {
  await ensureOpenApiSuccess(
    gameBoardApiClient.POST('/game/cells/{cellId}/open', {
      params: {
        path: { cellId },
      },
    }),
  )
}
