import {
  apiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  CreateGameQuestionRequest,
  UpdateGameQuestionRequest,
} from '../../../shared/api/contracts/index.ts'
import type { operations } from '../../../shared/api/contracts/generated.ts'

export type GameQuestionCatalogFilters = NonNullable<
  operations['getGameQuestionCatalog']['parameters']['query']
>

export function fetchGameQuestionCatalog(filters: GameQuestionCatalogFilters = {}) {
  return unwrapOpenApiData(
    apiClient.GET('/game/questions/catalog', {
      params: {
        query: {
          ...filters,
          includeDisabled: filters.includeDisabled ?? true,
        },
      },
    }),
  )
}

export function createGameQuestion(request: CreateGameQuestionRequest) {
  return unwrapOpenApiData(
    apiClient.POST('/game/questions', {
      body: request,
    }),
  )
}

export function updateGameQuestion(questionId: string, request: UpdateGameQuestionRequest) {
  return unwrapOpenApiData(
    apiClient.PUT('/game/questions/{questionId}', {
      params: {
        path: { questionId },
      },
      body: request,
    }),
  )
}

export function deleteGameQuestion(questionId: string) {
  return ensureOpenApiSuccess(
    apiClient.DELETE('/game/questions/{questionId}', {
      params: {
        path: { questionId },
      },
    }),
  )
}
