import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { GameSetupSnapshot } from '../../shared/api/contracts/index.ts'
import { gameSetupDraftQueryOptions } from './api/game-setup-queries.ts'
import { createLoadedDraftState } from './model/game-setup-query-state.ts'
import { useGameSetupCellMedia } from './use-game-setup-cell-media.ts'

const apiMocks = vi.hoisted(() => ({
  uploadDraftGameSetupCellMedia: vi.fn(),
  deleteDraftGameSetupCellMedia: vi.fn(),
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
      media: [{ url: '/media/original.png' }],
    },
  ],
  enabledModifierIds: [],
  enabledQuestionIds: [],
}

function renderMediaHook() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  queryClient.setQueryData(gameSetupDraftQueryOptions.queryKey, createLoadedDraftState(snapshot))

  function QueryWrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }

  return {
    queryClient,
    ...renderHook(() => useGameSetupCellMedia(snapshot), { wrapper: QueryWrapper }),
  }
}

describe('useGameSetupCellMedia', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn(() => 'blob:preview'),
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: vi.fn(),
    })
  })

  it('rejects unsupported files before starting an upload', async () => {
    const { result } = renderMediaHook()

    await act(async () => {
      await result.current.uploadCellMedia(
        'cell-1',
        new File(['not-an-image'], 'notes.txt', { type: 'text/plain' }),
      )
    })

    expect(result.current.cellMediaErrorKey).toBe('invalidType')
    expect(apiMocks.uploadDraftGameSetupCellMedia).not.toHaveBeenCalled()
  })

  it('patches the cached snapshot after a successful upload', async () => {
    apiMocks.uploadDraftGameSetupCellMedia.mockResolvedValue({ url: '/media/new.png' })
    const { result, queryClient } = renderMediaHook()

    await act(async () => {
      await result.current.uploadCellMedia(
        'cell-1',
        new File(['image'], 'cell.png', { type: 'image/png' }),
      )
    })

    await waitFor(() => {
      const cached = queryClient.getQueryData<ReturnType<typeof createLoadedDraftState>>(
        gameSetupDraftQueryOptions.queryKey,
      )
      expect(cached?.snapshot?.cells[0]?.media).toEqual([{ url: '/media/new.png' }])
    })
    expect(result.current.cellMediaDisplayByCellId['cell-1']).toMatchObject({
      phase: 'idle',
      previewUrl: null,
      cacheRevision: 1,
    })
  })

  it('rolls an optimistic media deletion back when the request fails', async () => {
    apiMocks.deleteDraftGameSetupCellMedia.mockRejectedValue(new Error('Unavailable'))
    const { result, queryClient } = renderMediaHook()

    act(() => {
      result.current.deleteCellMedia('cell-1')
    })

    await waitFor(() => {
      const cached = queryClient.getQueryData<ReturnType<typeof createLoadedDraftState>>(
        gameSetupDraftQueryOptions.queryKey,
      )
      expect(cached?.snapshot?.cells[0]?.media).toEqual([{ url: '/media/original.png' }])
      expect(result.current.cellMediaErrorKey).toBe('deleteFailed')
    })
  })
})
