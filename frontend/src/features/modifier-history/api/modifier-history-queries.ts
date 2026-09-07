import { infiniteQueryOptions, queryOptions } from '@tanstack/react-query'
import {
  fetchModifierHistory,
  fetchModifierVersion,
  fetchModifierVersionGames,
  fetchModifierVersions,
} from './modifier-history-api.ts'

const modifierHistoryQueryKeys = {
  all: ['modifierHistory'] as const,
  list: (search: string, status: string) => ['modifierHistory', 'list', search, status] as const,
  versions: (modifierId: string) => ['modifierHistory', 'versions', modifierId] as const,
  detail: (modifierId: string, revision: number) =>
    ['modifierHistory', 'detail', modifierId, revision] as const,
  games: (modifierId: string, revision: number) =>
    ['modifierHistory', 'games', modifierId, revision] as const,
}

export const modifierHistoryRootQueryOptions = queryOptions({
  queryKey: modifierHistoryQueryKeys.all,
})

export function modifierHistoryQueryOptions(search: string, status: 'active' | 'archived' | 'all') {
  return infiniteQueryOptions({
    queryKey: modifierHistoryQueryKeys.list(search, status),
    queryFn: ({ pageParam }) => fetchModifierHistory(search, status, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  })
}

export const modifierVersionsQueryOptions = (modifierId: string) =>
  infiniteQueryOptions({
    queryKey: modifierHistoryQueryKeys.versions(modifierId),
    queryFn: ({ pageParam }) => fetchModifierVersions(modifierId, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: Boolean(modifierId),
  })

export const modifierVersionQueryOptions = (modifierId: string, revision: number) =>
  queryOptions({
    queryKey: modifierHistoryQueryKeys.detail(modifierId, revision),
    queryFn: () => fetchModifierVersion(modifierId, revision),
    enabled: Boolean(modifierId) && revision > 0,
  })

export const modifierVersionGamesQueryOptions = (modifierId: string, revision: number) =>
  infiniteQueryOptions({
    queryKey: modifierHistoryQueryKeys.games(modifierId, revision),
    queryFn: ({ pageParam }) => fetchModifierVersionGames(modifierId, revision, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    enabled: Boolean(modifierId) && revision > 0,
  })
