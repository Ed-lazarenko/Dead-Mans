import {
  apiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  CreateGameSetupRequest,
  UpdateGameSetupRequest,
} from '../../../shared/api/contracts/index.ts'

export function fetchDraftGameSetupSnapshot() {
  return unwrapOpenApiDataOrNullOnNoContent(apiClient.GET('/game/setup'))
}

export function createDraftGameSetup(request: CreateGameSetupRequest) {
  return unwrapOpenApiData(
    apiClient.POST('/game/setup', {
      body: request,
    }),
  )
}

export function saveDraftGameSetup(request: UpdateGameSetupRequest) {
  return unwrapOpenApiData(
    apiClient.PUT('/game/setup', {
      body: request,
    }),
  )
}

export function deleteDraftGameSetup() {
  return ensureOpenApiSuccess(apiClient.DELETE('/game/setup'))
}

export function uploadDraftGameSetupCellMedia(cellId: string, file: File) {
  return unwrapOpenApiData(
    apiClient.POST('/game/setup/cells/{cellId}/media', {
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
    apiClient.DELETE('/game/setup/cells/{cellId}/media', {
      params: {
        path: { cellId },
      },
    }),
  )
}
