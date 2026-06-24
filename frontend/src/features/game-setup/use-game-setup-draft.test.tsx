import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { GameSetupSnapshot } from '../../shared/api/contracts/index.ts'
import { gameSetupDraftQueryOptions } from './api/game-setup-queries.ts'
import { createLoadedDraftState } from './model/game-setup-query-state.ts'
import { useGameSetupDraft } from './use-game-setup-draft.ts'

const apiMocks = vi.hoisted(() => ({
  fetchDraftGameSetupSnapshot: vi.fn(),
  createDraftGameSetup: vi.fn(),
  deleteDraftGameSetup: vi.fn(),
}))

vi.mock('./api/game-setup-api.ts', () => apiMocks)

const snapshot: GameSetupSnapshot = {
  gameId: 'game-1',
  title: 'Draft game',
  status: 'draft',
  version: 1,
  rows: 1,
  cols: 1,
  rowLabels: ['100'],
  colLabels: ['A'],
  cells: [
    {
      id: 'cell-1',
      row: 0,
      col: 0,
      cellType: 'question',
      title: 'First cell',
      description: null,
      cost: 100,
      state: 'closed',
      media: [],
    },
  ],
  enabledModifierIds: [],
  enabledQuestionIds: [],
}

function createQueryWrapper(queryClient: QueryClient) {
  return function QueryWrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
}

describe('useGameSetupDraft', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('keeps create and delete lifecycle state inside the draft controller', async () => {
    apiMocks.fetchDraftGameSetupSnapshot.mockResolvedValue(null)
    apiMocks.createDraftGameSetup.mockResolvedValue(snapshot)
    apiMocks.deleteDraftGameSetup.mockResolvedValue(undefined)
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    const { result } = renderHook(() => useGameSetupDraft(), {
      wrapper: createQueryWrapper(queryClient),
    })

    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(result.current.isEmpty).toBe(true)

    await act(async () => {
      await result.current.createDraft({ title: snapshot.title })
    })

    expect(apiMocks.createDraftGameSetup).toHaveBeenCalledWith(
      { title: snapshot.title },
      expect.anything(),
    )
    await waitFor(() => expect(result.current.snapshot).toEqual(snapshot))
    expect(result.current.draft?.title).toBe(snapshot.title)
    expect(result.current.draftRemovedNotice).toBe(false)

    await act(async () => {
      await result.current.deleteDraft()
    })

    expect(apiMocks.deleteDraftGameSetup).toHaveBeenCalledOnce()
    await waitFor(() => expect(result.current.snapshot).toBeNull())
    expect(result.current.draft).toBeNull()
    expect(result.current.isEmpty).toBe(true)
    expect(result.current.draftRemovedNotice).toBe(false)
  })

  it('preserves local edits and reports a newer remote snapshot', async () => {
    apiMocks.fetchDraftGameSetupSnapshot.mockResolvedValue(snapshot)
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    const { result } = renderHook(() => useGameSetupDraft(), {
      wrapper: createQueryWrapper(queryClient),
    })

    await waitFor(() => expect(result.current.snapshot).toEqual(snapshot))

    act(() => {
      result.current.updateDraft((current) => ({ ...current, title: 'Local title' }))
    })

    expect(result.current.isDirty).toBe(true)
    expect(result.current.draft?.title).toBe('Local title')

    const remoteSnapshot = { ...snapshot, title: 'Remote title', version: 2 }
    act(() => {
      queryClient.setQueryData(
        gameSetupDraftQueryOptions.queryKey,
        createLoadedDraftState(remoteSnapshot),
      )
    })

    await waitFor(() => expect(result.current.remoteChangeNotice).toBe(true))
    expect(result.current.draft?.title).toBe('Local title')
    expect(result.current.snapshot).toEqual(remoteSnapshot)
  })
})
