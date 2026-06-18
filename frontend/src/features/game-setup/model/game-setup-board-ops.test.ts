import { describe, expect, it } from 'vitest'
import type { GameSetupDraftState } from './game-setup-draft.ts'
import {
  applyGameSetupBoardLayoutChange,
  canApplyBoardLayoutChange,
  getBoardLayoutPositionIndexes,
  getBoardLayoutTargetLabel,
  insertGameSetupRowAt,
  rebuildGameSetupCells,
  removeGameSetupColumn,
} from './game-setup-board-ops.ts'
import { GAME_SETUP_MAX_COLS, GAME_SETUP_MAX_ROWS } from './game-setup-limits.ts'

function createDraft(): GameSetupDraftState {
  return {
    title: 'Draft',
    rowLabels: ['100', '200'],
    colLabels: ['A', 'B'],
    enabledModifierCodes: [],
    enabledQuestionIds: [],
    cells: [
      { id: 'a', row: 0, col: 0, title: 'A', cost: 100 },
      { id: 'b', row: 0, col: 1, title: 'B', cost: 100 },
      { id: 'c', row: 1, col: 0, title: 'C', cost: 200 },
      { id: 'd', row: 1, col: 1, title: 'D', cost: 200 },
    ],
  }
}

describe('game setup board operations', () => {
  it('preserves existing cells when inserting a row', () => {
    const next = insertGameSetupRowAt(createDraft(), 1)

    expect(next.rowLabels).toEqual(['100', '125', '200'])
    expect(next.cells.find((cell) => cell.id === 'c')).toMatchObject({ row: 2, col: 0 })
    expect(next.cells).toHaveLength(6)
  })

  it('removes cells from a deleted column and shifts later columns', () => {
    const next = removeGameSetupColumn(createDraft(), 0)

    expect(next.colLabels).toEqual(['B'])
    expect(next.cells.map((cell) => cell.id)).toEqual(['b', 'd'])
    expect(next.cells.every((cell) => cell.col === 0)).toBe(true)
  })

  it('rebuilds missing cells with row defaults while preserving existing cells', () => {
    const cells = rebuildGameSetupCells(
      ['100', '200'],
      ['A'],
      [{ id: 'existing', row: 0, col: 0, title: 'Existing', cost: 300 }],
    )

    expect(cells).toEqual([
      { id: 'existing', row: 0, col: 0, title: 'Existing', cost: 300 },
      { row: 1, col: 0, title: '', cost: 125 },
    ])
  })

  it('applies row and column changes through the common dispatcher', () => {
    const withRow = applyGameSetupBoardLayoutChange(createDraft(), 'add', 'row', -10)
    const withoutRow = applyGameSetupBoardLayoutChange(withRow, 'remove', 'row', 0)
    const withColumn = applyGameSetupBoardLayoutChange(withoutRow, 'add', 'column', 1)
    const withoutColumn = applyGameSetupBoardLayoutChange(withColumn, 'remove', 'column', 1)

    expect(withRow.rowLabels).toHaveLength(3)
    expect(withRow.cells.find((cell) => cell.id === 'a')).toMatchObject({ row: 1, col: 0 })
    expect(withoutRow.rowLabels).toEqual(['100', '200'])
    expect(withColumn.colLabels).toHaveLength(3)
    expect(withColumn.cells.find((cell) => cell.id === 'b')).toMatchObject({ row: 0, col: 2 })
    expect(withoutColumn.colLabels).toEqual(['A', 'B'])
  })

  it('ignores invalid removals and changes outside layout limits', () => {
    const singleCellDraft: GameSetupDraftState = {
      ...createDraft(),
      rowLabels: ['100'],
      colLabels: ['A'],
      cells: [{ row: 0, col: 0, title: '', cost: 100 }],
    }
    const maxRowsDraft = {
      ...createDraft(),
      rowLabels: Array.from({ length: GAME_SETUP_MAX_ROWS }, (_, index) => String(index)),
    }
    const maxColsDraft = {
      ...createDraft(),
      colLabels: Array.from({ length: GAME_SETUP_MAX_COLS }, (_, index) => String(index)),
    }

    expect(applyGameSetupBoardLayoutChange(singleCellDraft, 'remove', 'row', 0)).toBe(
      singleCellDraft,
    )
    expect(removeGameSetupColumn(singleCellDraft, 5)).toBe(singleCellDraft)
    expect(applyGameSetupBoardLayoutChange(maxRowsDraft, 'add', 'row', 0)).toBe(maxRowsDraft)
    expect(applyGameSetupBoardLayoutChange(maxColsDraft, 'add', 'column', 0)).toBe(maxColsDraft)
  })

  it('describes available positions, labels, and layout limits', () => {
    const draft = createDraft()
    const maxRowsDraft = {
      ...draft,
      rowLabels: Array.from({ length: GAME_SETUP_MAX_ROWS }, (_, index) => String(index)),
    }

    expect(getBoardLayoutPositionIndexes(draft, 'add', 'row')).toEqual([0, 1, 2])
    expect(getBoardLayoutPositionIndexes(draft, 'remove', 'column')).toEqual([0, 1])
    expect(getBoardLayoutTargetLabel(draft, 'row', 1)).toBe('200')
    expect(getBoardLayoutTargetLabel(draft, 'column', 0)).toBe('A')
    expect(getBoardLayoutTargetLabel(draft, 'column', 10)).toBe('')
    expect(canApplyBoardLayoutChange(draft, 'add', 'column')).toBe(true)
    expect(canApplyBoardLayoutChange(draft, 'remove', 'row')).toBe(true)
    expect(canApplyBoardLayoutChange(maxRowsDraft, 'add', 'row')).toBe(false)
    expect(canApplyBoardLayoutChange({ ...draft, colLabels: ['A'] }, 'remove', 'column')).toBe(
      false,
    )
  })
})
