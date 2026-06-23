import { unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import {
  apiClient,
  ensureOpenApiSuccess,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  CreateGameQuestionCategoryRequest,
  GameQuestionCategoryItem,
} from '../../../shared/api/contracts/index.ts'

export const questionCategoryQueryKey = ['gameQuestionCategories'] as const

export function fetchQuestionCategories(): Promise<GameQuestionCategoryItem[]> {
  return unwrapOpenApiData(apiClient.GET('/game/questions/categories'))
}

export function createQuestionCategory(
  request: CreateGameQuestionCategoryRequest,
): Promise<GameQuestionCategoryItem> {
  return unwrapOpenApiData(
    apiClient.POST('/game/questions/categories', {
      body: request,
    }),
  )
}

export function updateQuestionCategory(
  categoryId: string,
  request: CreateGameQuestionCategoryRequest,
): Promise<GameQuestionCategoryItem> {
  return unwrapOpenApiData(
    apiClient.PUT('/game/questions/categories/{categoryId}', {
      params: {
        path: { categoryId },
      },
      body: request,
    }),
  )
}

export function deleteQuestionCategory(categoryId: string) {
  return ensureOpenApiSuccess(
    apiClient.DELETE('/game/questions/categories/{categoryId}', {
      params: {
        path: { categoryId },
      },
    }),
  )
}
