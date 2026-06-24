import { describe, expect, it } from 'vitest'
import type { GameSetupSnapshot } from '../../../shared/api/contracts/index.ts'
import { createLoadedDraftState } from '../model/game-setup-query-state.ts'
import { selectNewerGameSetupState } from './game-setup-realtime-model.ts'

const snapshot: GameSetupSnapshot = {
  gameId: 'game-1',
  title: 'Draft',
  description: null,
  status: 'draft',
  version: 2,
  rows: 1,
  cols: 1,
  rowLabels: ['100'],
  colLabels: ['A'],
  cells: [],
  enabledModifierIds: [],
  enabledQuestionIds: [],
}

describe('game setup realtime model', () => {
  it('accepts initial, removed and equally new server states', () => {
    const current = createLoadedDraftState(snapshot)
    const removed = createLoadedDraftState(null)
    const sameVersion = createLoadedDraftState({ ...snapshot, title: 'Server title' })

    expect(selectNewerGameSetupState(undefined, current)).toBe(current)
    expect(selectNewerGameSetupState(current, removed)).toBe(removed)
    expect(selectNewerGameSetupState(current, sameVersion)).toBe(sameVersion)
  })

  it('keeps the current draft state when a stale resync finishes later', () => {
    const current = createLoadedDraftState(snapshot)
    const stale = createLoadedDraftState({ ...snapshot, version: 1 })

    expect(selectNewerGameSetupState(current, stale)).toBe(current)
  })
})
