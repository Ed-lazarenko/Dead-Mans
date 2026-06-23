import {
  apiClient,
  unwrapOpenApiData,
} from '../../../shared/api/client/openApiClient.ts'
import type { ImportGameQuestionsResult } from '../../../shared/api/contracts/index.ts'
import { getApiBaseUrl } from '../../../shared/api/config.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'

export function importQuestionsFile(file: File): Promise<ImportGameQuestionsResult> {
  return unwrapOpenApiData(
    apiClient.POST('/game/questions/import', {
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

export function downloadQuestionImportTemplate(): Promise<string> {
  return fetch(`${getApiBaseUrl()}/game/questions/import-template`, {
    credentials: 'include',
    headers: {
      'X-Dead-Mans-Api-Client': '1',
    },
  }).then(async (response) => {
    if (!response.ok) {
      let details: unknown
      try {
        details = await response.json()
      } catch {
        details = undefined
      }

      throw new ApiError(`HTTP ${response.status}`, {
        status: response.status,
        details,
      })
    }

    return await response.text()
  })
}
