import { queryOptions } from '@tanstack/react-query'
import { fetchGameModifierCatalog } from './game-modifiers-api.ts'

const gameModifierQueryKeys = {
  all: ['gameModifiers'] as const,
  catalog: () => [...gameModifierQueryKeys.all, 'catalog'] as const,
}

export const gameModifierCatalogQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.catalog(),
  queryFn: fetchGameModifierCatalog,
})
