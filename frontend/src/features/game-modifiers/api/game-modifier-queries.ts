import { queryOptions } from '@tanstack/react-query'
import { fetchGameModifierCatalog, fetchGameModifierState } from './game-modifiers-api.ts'

export const gameModifierQueryKeys = {
  all: ['gameModifiers'] as const,
  catalog: () => [...gameModifierQueryKeys.all, 'catalog'] as const,
  state: () => [...gameModifierQueryKeys.all, 'state'] as const,
}

export const gameModifierCatalogQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.catalog(),
  queryFn: fetchGameModifierCatalog,
})

export const gameModifierStateQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.state(),
  queryFn: fetchGameModifierState,
  staleTime: 0,
  refetchInterval: 5_000,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})
