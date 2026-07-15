import {
  createApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  CreateGameSetupRequest,
  UpdateGameSetupRequest,
} from '../../../shared/api/contracts/index.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameSetupApiClient =
  createApiClient<Pick<paths, '/game/setup' | '/game/setup/cells/{cellId}/media'>>()

export function fetchDraftGameSetupSnapshot() {
  return unwrapOpenApiDataOrNullOnNoContent(gameSetupApiClient.GET('/game/setup'))
}

export function createDraftGameSetup(request: CreateGameSetupRequest) {
  return unwrapOpenApiData(
    gameSetupApiClient.POST('/game/setup', {
      body: request,
    }),
  )
}

export function saveDraftGameSetup(request: UpdateGameSetupRequest) {
  return unwrapOpenApiData(
    gameSetupApiClient.PUT('/game/setup', {
      body: request,
    }),
  )
}

export function deleteDraftGameSetup() {
  return ensureOpenApiSuccess(gameSetupApiClient.DELETE('/game/setup'))
}

export function uploadDraftGameSetupCellMedia(cellId: string, file: File) {
  return unwrapOpenApiData(
    gameSetupApiClient.POST('/game/setup/cells/{cellId}/media', {
      params: {
        path: { cellId },
      },
      body: {
        file: file.name,
      },
      bodySerializer: () => {
        const formData = new FormData()
        formData.append('file', file)
        return formData
      },
    }),
  )
}

export function deleteDraftGameSetupCellMedia(cellId: string) {
  return ensureOpenApiSuccess(
    gameSetupApiClient.DELETE('/game/setup/cells/{cellId}/media', {
      params: {
        path: { cellId },
      },
    }),
  )
}
