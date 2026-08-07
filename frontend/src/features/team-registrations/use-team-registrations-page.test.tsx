import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { gameRegistrationAdminSnapshotQueryOptions } from '../game-registration/index.ts'
import { useTeamRegistrationsPage } from './use-team-registrations-page.ts'

vi.mock('../game-registration/index.ts', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../game-registration/index.ts')>()
  const mutation = () => ({ isPending: false, variables: undefined, mutate: vi.fn() })

  return {
    ...actual,
    useAssignGameRegistrationPlayerToTeamMutation: mutation,
    useCancelGameRegistrationTeamInvitationMutation: mutation,
    useConfirmGameRegistrationTeamMutation: mutation,
    useCreateAdminGameRegistrationInvitationMutation: mutation,
    useCreateAdminGameRegistrationTeamMutation: mutation,
    useDisbandConfirmedGameRegistrationTeamMutation: mutation,
    useGameRegistrationToast: () => ({
      toastMessage: null,
      onMutationError: vi.fn(),
      dismissToast: vi.fn(),
    }),
    useMoveGameRegistrationTeamToSlotMutation: mutation,
    useRejectGameRegistrationTeamMutation: mutation,
    useRemoveGameRegistrationPlayerFromTeamMutation: mutation,
  }
})

function createWrapper(queryClient: QueryClient) {
  return function QueryWrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
}

describe('useTeamRegistrationsPage', () => {
  it('keeps the admin registration query available when the current game is active', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          staleTime: Number.POSITIVE_INFINITY,
        },
      },
    })
    const adminSnapshot = {
      gameId: 'game-1',
      gameStatus: 'active',
      minPlayersPerTeam: 1,
      maxPlayersPerTeam: 2,
      teamSlots: [],
      teams: [],
      availablePlayers: [],
    }
    queryClient.setQueryData(currentGameBoardQueryOptions.queryKey, {
      gameId: 'game-1',
      title: 'Current game',
      description: null,
      status: 'active',
      version: 1,
      rows: 1,
      cols: 1,
      rowLabels: ['A'],
      colLabels: ['1'],
      cells: [],
      enabledModifierIds: [],
      activeModifiers: [],
    })
    queryClient.setQueryData(gameRegistrationAdminSnapshotQueryOptions.queryKey, adminSnapshot)

    const { result } = renderHook(() => useTeamRegistrationsPage(), {
      wrapper: createWrapper(queryClient),
    })

    expect(result.current.adminSnapshotQuery.data).toBe(adminSnapshot)
    expect(
      queryClient.getQueryState(gameRegistrationAdminSnapshotQueryOptions.queryKey)?.fetchStatus,
    ).toBe('idle')
  })

  it('keeps the admin registration query idle when the current game is finished', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          staleTime: Number.POSITIVE_INFINITY,
        },
      },
    })
    queryClient.setQueryData(currentGameBoardQueryOptions.queryKey, {
      gameId: 'game-1',
      title: 'Current game',
      description: null,
      status: 'finished',
      version: 1,
      rows: 1,
      cols: 1,
      rowLabels: ['A'],
      colLabels: ['1'],
      cells: [],
      enabledModifierIds: [],
      activeModifiers: [],
    })

    const { result } = renderHook(() => useTeamRegistrationsPage(), {
      wrapper: createWrapper(queryClient),
    })

    expect(result.current.adminSnapshotQuery.data).toBeNull()
    expect(
      queryClient.getQueryState(gameRegistrationAdminSnapshotQueryOptions.queryKey)?.fetchStatus,
    ).toBe('idle')
  })
})
