import { queryOptions } from '@tanstack/react-query'
import {
  fetchAdminActiveGameModifierActivations,
  fetchAdminGameModifierPlayers,
  fetchAdminGameModifierState,
  fetchGameModifierCatalog,
  fetchGameModifierState,
} from './game-modifiers-api.ts'

export const gameModifierQueryKeys = {
  all: ['gameModifiers'] as const,
  catalog: () => [...gameModifierQueryKeys.all, 'catalog'] as const,
  state: () => [...gameModifierQueryKeys.all, 'state'] as const,
  adminPlayers: () => [...gameModifierQueryKeys.all, 'adminPlayers'] as const,
  adminState: (userId: string) => [...gameModifierQueryKeys.all, 'adminState', userId] as const,
  adminActivations: () => [...gameModifierQueryKeys.all, 'adminActivations'] as const,
}

export const gameModifierCatalogQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.catalog(),
  queryFn: fetchGameModifierCatalog,
})

export const gameModifierStateQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.state(),
  queryFn: fetchGameModifierState,
  staleTime: 0,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})

export const adminGameModifierPlayersQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.adminPlayers(),
  queryFn: fetchAdminGameModifierPlayers,
  staleTime: 0,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})

export function adminGameModifierStateQueryOptions(userId: string) {
  return queryOptions({
    queryKey: gameModifierQueryKeys.adminState(userId),
    queryFn: () => fetchAdminGameModifierState(userId),
    staleTime: 0,
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
  })
}

export const adminGameModifierActivationsQueryOptions = queryOptions({
  queryKey: gameModifierQueryKeys.adminActivations(),
  queryFn: fetchAdminActiveGameModifierActivations,
  staleTime: 0,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})
