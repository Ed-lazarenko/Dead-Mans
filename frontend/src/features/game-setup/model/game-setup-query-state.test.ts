import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { GameSetupSnapshot } from '../../../shared/api/contracts/index.ts'
import {
  createLoadedDraftState,
  getSnapshotDraftKey,
  loadGameSetupDraftQueryState,
} from './game-setup-query-state.ts'

const apiMocks = vi.hoisted(() => ({
  fetchDraftGameSetupSnapshot: vi.fn(),
}))

vi.mock('../api/game-setup-api.ts', () => apiMocks)

const snapshot = {
  gameId: 'game-1',
  title: 'Draft',
  status: 'draft',
  version: 1,
  rows: 0,
  cols: 0,
  rowLabels: [],
  colLabels: [],
  cells: [],
  enabledModifierIds: [],
  enabledQuestionIds: [],
} satisfies GameSetupSnapshot

describe('game setup query state', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates empty and loaded controller state', () => {
    expect(createLoadedDraftState(null)).toEqual({
      snapshot: null,
      savedDraft: null,
      initialDraft: null,
    })

    const loaded = createLoadedDraftState(snapshot)
    expect(getSnapshotDraftKey(snapshot)).toBe('game-1')
    expect(loaded.snapshot).toBe(snapshot)
    expect(loaded.savedDraft).toEqual(loaded.initialDraft)
    expect(loaded.savedDraft?.title).toBe('Draft')
  })

  it('loads the snapshot through the typed API adapter', async () => {
    apiMocks.fetchDraftGameSetupSnapshot.mockResolvedValue(snapshot)

    await expect(loadGameSetupDraftQueryState()).resolves.toEqual(createLoadedDraftState(snapshot))
  })
})
