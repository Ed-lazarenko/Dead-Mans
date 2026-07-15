import { queryOptions } from '@tanstack/react-query'
import { fetchGameQuestionCatalog, type GameQuestionCatalogFilters } from './game-questions-api.ts'

export const gameQuestionQueryKeys = {
  all: ['gameQuestions'] as const,
  catalog: (filters: GameQuestionCatalogFilters) =>
    [...gameQuestionQueryKeys.all, 'catalog', filters] as const,
}

export function gameQuestionCatalogQueryOptions(filters: GameQuestionCatalogFilters = {}) {
  const normalizedFilters = {
    ...filters,
    includeDisabled: filters.includeDisabled ?? true,
  }

  return queryOptions({
    queryKey: gameQuestionQueryKeys.catalog(normalizedFilters),
    queryFn: () => fetchGameQuestionCatalog(normalizedFilters),
  })
}
