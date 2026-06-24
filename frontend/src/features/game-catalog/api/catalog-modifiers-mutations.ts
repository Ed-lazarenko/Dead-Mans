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
      modifierId,
      request,
    }: {
      modifierId: string
      request: UpdateGameModifierRequest
    }) => updateGameModifier(modifierId, request),
    onSuccess: () => invalidateModifierCatalog(queryClient),
  })
}

export function deleteGameModifierMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: (modifierId: string) => deleteGameModifier(modifierId),
    onSuccess: () => invalidateModifierCatalog(queryClient),
  })
}
