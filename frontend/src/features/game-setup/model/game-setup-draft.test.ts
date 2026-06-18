import { describe, expect, it } from 'vitest'
import type { GameSetupSnapshot } from '../../../shared/api/contracts/index.ts'
import {
  buildUpdateGameSetupRequest,
  createDraftFromSnapshot,
  getGameSetupCellAt,
  isGameSetupDraftDirty,
  type GameSetupDraftState,
  upsertGameSetupCellDraft,
} from './game-setup-draft.ts'

const snapshot: GameSetupSnapshot = {
  gameId: 'game-1',
  title: 'Draft',
  status: 'draft',
  version: 4,
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
      title: null,
      description: null,
      cost: 100,
      state: 'closed',
      media: [],
    },
  ],
  enabledModifierCodes: ['double'],
  enabledQuestionIds: [],
}

function createDraft(): GameSetupDraftState {
  return createDraftFromSnapshot(snapshot)
}

describe('game setup draft', () => {
  it('creates an editable copy from a server snapshot', () => {
    const draft = createDraftFromSnapshot(snapshot)

    expect(draft).toEqual({
      title: 'Draft',
      rowLabels: ['100'],
      colLabels: ['A'],
      cells: [{ id: 'cell-1', row: 0, col: 0, title: '', cost: 100 }],
      enabledModifierCodes: ['double'],
      enabledQuestionIds: [],
    })
    expect(draft.rowLabels).not.toBe(snapshot.rowLabels)
    expect(draft.enabledModifierCodes).not.toBe(snapshot.enabledModifierCodes)
  })

  it('finds, updates, and creates cells without mutating the draft', () => {
    const draft = createDraft()
    const updated = upsertGameSetupCellDraft(draft, 0, 0, { title: 'Updated' })
    const created = upsertGameSetupCellDraft(updated, 1, 0, {})

    expect(getGameSetupCellAt(updated, 0, 0)?.title).toBe('Updated')
    expect(getGameSetupCellAt(draft, 0, 0)?.title).toBe('')
    expect(getGameSetupCellAt(created, 1, 0)).toEqual({
      row: 1,
      col: 0,
      title: '',
      cost: 125,
    })
    expect(getGameSetupCellAt(created, 5, 5)).toBeUndefined()
  })

  it('detects structural, modifier, question, and cell changes', () => {
    const saved = createDraft()

    expect(isGameSetupDraftDirty(saved, createDraft())).toBe(false)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), title: 'Other' })).toBe(true)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), rowLabels: ['100', '200'] })).toBe(true)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), rowLabels: ['200'] })).toBe(true)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), colLabels: ['A', 'B'] })).toBe(true)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), colLabels: ['B'] })).toBe(true)
    expect(isGameSetupDraftDirty(saved, { ...createDraft(), cells: [] })).toBe(true)
    expect(
      isGameSetupDraftDirty(saved, {
        ...createDraft(),
        enabledModifierCodes: ['double', 'steal'],
        enabledQuestionIds: [],
      }),
    ).toBe(true)
    expect(
      isGameSetupDraftDirty(saved, { ...createDraft(), enabledModifierCodes: ['steal'] }),
    ).toBe(true)
    expect(
      isGameSetupDraftDirty(saved, {
        ...createDraft(),
        enabledQuestionIds: ['11111111-1111-1111-1111-111111111111'],
      }),
    ).toBe(true)
    expect(
      isGameSetupDraftDirty(saved, {
        ...createDraft(),
        cells: [{ ...createDraft().cells[0]!, title: 'Changed' }],
      }),
    ).toBe(true)
  })

  it('normalizes a draft into a safe update request', () => {
    const request = buildUpdateGameSetupRequest(
      {
        title: '  Final title  ',
        rowLabels: ['  ', ' 250 '],
        colLabels: [' '],
        cells: [
          { id: 'cell-1', row: 0, col: 0, title: '  ', cost: -2.4 },
          { row: 1, col: 0, title: ' Question ', cost: Number.NaN },
        ],
        enabledModifierCodes: ['steal', 'double'],
        enabledQuestionIds: [],
      },
      7,
    )

    expect(request).toEqual({
      expectedVersion: 7,
      title: 'Final title',
      rowLabels: ['100', '250'],
      colLabels: ['1'],
      cells: [
        { id: 'cell-1', row: 0, col: 0, title: null, cost: 0 },
        { row: 1, col: 0, title: 'Question', cost: 0 },
      ],
      enabledModifierCodes: ['double', 'steal'],
      enabledQuestionIds: [],
    })
  })
})
