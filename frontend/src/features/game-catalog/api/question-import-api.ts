import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { ImportGameQuestionsResult } from '../../../shared/api/contracts/index.ts'
import type { paths } from '../../../shared/api/contracts/generated'
import { getApiBaseUrl } from '../../../shared/api/config.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'

const questionImportApiClient = createApiClient<Pick<paths, '/game/questions/import'>>()

export function importQuestionsFile(file: File): Promise<ImportGameQuestionsResult> {
  return unwrapOpenApiData(
    questionImportApiClient.POST('/game/questions/import', {
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

export function downloadQuestionImportTemplate(locale?: string): Promise<string> {
  const url = new URL(`${getApiBaseUrl()}/game/questions/import-template`, window.location.origin)
  if (locale) {
    url.searchParams.set('locale', locale)
  }

  return fetch(url.toString(), {
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
