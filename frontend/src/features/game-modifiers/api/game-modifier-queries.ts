import { queryOptions } from '@tanstack/react-query'
import { fetchGameModifierCatalog, fetchUserGameHistory } from './game-modifiers-api.ts'

const gameModifierQueryKeys = {
  all: ['gameModifiers'] as const,
  catalog: () => [...gameModifierQueryKeys.all, 'catalog'] as const,
  userHistory: (userId: string) => [...gameModifierQueryKeys.all, 'userHistory', userId] as const,
}

export const gameModifierCatalogQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.catalog(),
  queryFn: fetchGameModifierCatalog,
})

export const userGameHistoryQueryOptions = (userId: string) =>
  queryOptions({
    queryKey: gameModifierQueryKeys.userHistory(userId),
    queryFn: () => fetchUserGameHistory(userId),
  })
