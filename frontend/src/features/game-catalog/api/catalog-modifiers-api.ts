import {
  apiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  CreateGameModifierRequest,
  UpdateGameModifierRequest,
} from '../../../shared/api/contracts/index.ts'

export function createGameModifier(request: CreateGameModifierRequest) {
  return unwrapOpenApiData(
    apiClient.POST('/game/modifiers', {
      body: request,
    }),
  )
}

export function updateGameModifier(modifierCode: string, request: UpdateGameModifierRequest) {
  return unwrapOpenApiData(
    apiClient.PUT('/game/modifiers/{modifierCode}', {
      params: {
        path: { modifierCode },
      },
      body: request,
    }),
  )
}

export function deleteGameModifier(modifierCode: string) {
  return ensureOpenApiSuccess(
    apiClient.DELETE('/game/modifiers/{modifierCode}', {
      params: {
        path: { modifierCode },
      },
    }),
  )
}
