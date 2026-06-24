import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { GameSetupSnapshot } from '../../shared/api/contracts/index.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { createDraftFromSnapshot } from './model/game-setup-draft.ts'
import { createLoadedDraftState } from './model/game-setup-query-state.ts'
import { useGameSetupSave } from './use-game-setup-save.ts'

const apiMocks = vi.hoisted(() => ({
  saveDraftGameSetup: vi.fn(),
}))
const queryStateMocks = vi.hoisted(() => ({
  loadGameSetupDraftQueryState: vi.fn(),
}))

vi.mock('./api/game-setup-api.ts', () => apiMocks)
vi.mock('./model/game-setup-query-state.ts', async (importOriginal) => {
  const actual = await importOriginal<typeof import('./model/game-setup-query-state.ts')>()
  return {
    ...actual,
    loadGameSetupDraftQueryState: queryStateMocks.loadGameSetupDraftQueryState,
  }
})

const snapshot: GameSetupSnapshot = {
  gameId: 'game-1',
  title: 'Draft game',
  status: 'draft',
  version: 3,
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

function createQueryWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })

  return function QueryWrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }
}

function renderSaveHook() {
  const draft = { ...createDraftFromSnapshot(snapshot), title: 'Edited title' }
  const applyLoadedDraftState = vi.fn()
  const setDraftOverride = vi.fn()
  const setRemoteChangeNotice = vi.fn()
  const hook = renderHook(
    () =>
      useGameSetupSave({
        draft,
        snapshot,
        snapshotDraftKey: snapshot.gameId,
        isDirty: true,
        applyLoadedDraftState,
        setDraftOverride,
        setRemoteChangeNotice,
      }),
    { wrapper: createQueryWrapper() },
  )

  return {
    ...hook,
    draft,
    applyLoadedDraftState,
    setDraftOverride,
    setRemoteChangeNotice,
  }
}

describe('useGameSetupSave', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('saves the current draft through the typed update request', async () => {
    const savedSnapshot = { ...snapshot, title: 'Edited title', version: 4 }
    apiMocks.saveDraftGameSetup.mockResolvedValue(savedSnapshot)
    const { result, applyLoadedDraftState, setRemoteChangeNotice } = renderSaveHook()

    await act(async () => {
      await result.current.saveDraft()
    })

    expect(apiMocks.saveDraftGameSetup).toHaveBeenCalledWith(
      expect.objectContaining({
        expectedVersion: snapshot.version,
        title: 'Edited title',
      }),
    )
    expect(applyLoadedDraftState).toHaveBeenCalledWith(createLoadedDraftState(savedSnapshot))
    expect(setRemoteChangeNotice).toHaveBeenLastCalledWith(false)
    expect(result.current.saveErrorMessage).toBeNull()
  })

  it('loads the server state and exposes a conflict on stale version', async () => {
    const remoteSnapshot = { ...snapshot, title: 'Remote title', version: 4 }
    apiMocks.saveDraftGameSetup.mockRejectedValue(
      new ApiError('Conflict', {
        status: 409,
        details: { code: API_ERROR_CODES.gameSetupStaleVersion },
      }),
    )
    queryStateMocks.loadGameSetupDraftQueryState.mockResolvedValue(
      createLoadedDraftState(remoteSnapshot),
    )
    const { result, applyLoadedDraftState, setRemoteChangeNotice } = renderSaveHook()

    await act(async () => {
      await expect(result.current.saveDraft()).rejects.toThrow('Conflict')
    })

    expect(applyLoadedDraftState).toHaveBeenCalledWith(createLoadedDraftState(remoteSnapshot))
    expect(setRemoteChangeNotice).toHaveBeenLastCalledWith(true)
    expect(result.current.syncStatus).toBe('conflict')
  })

  it('rolls an optimistic layout edit back when saving fails', async () => {
    apiMocks.saveDraftGameSetup.mockRejectedValue(new ApiError('Unavailable', { status: 503 }))
    const { result, draft, setDraftOverride } = renderSaveHook()
    const nextDraft = { ...draft, rowLabels: ['200'] }

    act(() => {
      result.current.applyLayoutChange(() => nextDraft)
    })

    expect(setDraftOverride).toHaveBeenNthCalledWith(1, {
      key: snapshot.gameId,
      draft: nextDraft,
    })
    await waitFor(() =>
      expect(setDraftOverride).toHaveBeenLastCalledWith({
        key: snapshot.gameId,
        draft,
      }),
    )
    expect(result.current.syncStatus).toBe('error')
    expect(result.current.saveErrorMessage).toBe('saveFailed')
  })
})
