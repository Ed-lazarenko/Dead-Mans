import { mutationOptions, type QueryClient } from '@tanstack/react-query'
import type {
  CreateGameModifierRequest,
  UpdateGameModifierRequest,
} from '../../../shared/api/contracts/index.ts'
import { gameModifierCatalogQueryOptions } from '../../game-modifiers/index.ts'
import {
  createGameModifier,
  deleteGameModifier,
  updateGameModifier,
} from './catalog-modifiers-api.ts'

function invalidateModifierCatalog(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: gameModifierCatalogQueryOptions.queryKey })
}

export function createGameModifierMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: (request: CreateGameModifierRequest) => createGameModifier(request),
    onSuccess: () => invalidateModifierCatalog(queryClient),
  })
}

export function updateGameModifierMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: ({
      modifierCode,
      request,
    }: {
      modifierCode: string
      request: UpdateGameModifierRequest
    }) => updateGameModifier(modifierCode, request),
    onSuccess: () => invalidateModifierCatalog(queryClient),
  })
}

export function deleteGameModifierMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: (modifierCode: string) => deleteGameModifier(modifierCode),
    onSuccess: () => invalidateModifierCatalog(queryClient),
  })
}
