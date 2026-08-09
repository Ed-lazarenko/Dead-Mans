import { describe, expect, it } from 'vitest'
import {
  buildGameBoardCellPlayResultMap,
  type GameBoardCellPlayResultRound,
} from './game-board-cell-results.ts'

describe('buildGameBoardCellPlayResultMap', () => {
  it('keeps the latest finalized result for each allowed cell', () => {
    const results = buildGameBoardCellPlayResultMap(
      [
        createRound({
          roundId: 'older',
          cellId: 'cell-1',
          finalScore: 100,
          finishedAtUtc: '2026-07-23T10:00:00Z',
        }),
        createRound({
          roundId: 'unfinished',
          cellId: 'cell-1',
          status: 'reviewing_results',
          finalScore: null,
          finishedAtUtc: null,
        }),
        createRound({
          roundId: 'latest',
          cellId: 'cell-1',
          finalScore: 160,
          finishedAtUtc: '2026-07-23T11:00:00Z',
        }),
        createRound({
          roundId: 'other-cell',
          cellId: 'cell-2',
          finalScore: 200,
          finishedAtUtc: '2026-07-23T12:00:00Z',
        }),
      ],
      new Set(['cell-1']),
    )

    expect(results.get('cell-1')?.roundId).toBe('latest')
    expect(results.get('cell-1')?.finalScore).toBe(160)
    expect(results.has('cell-2')).toBe(false)
  })
})

function createRound(
  overrides: Partial<GameBoardCellPlayResultRound> = {},
): GameBoardCellPlayResultRound {
  return {
    roundId: 'round-1',
    teamId: 'team-1',
    teamName: 'Dead Mans',
    teamSlotIndex: 1,
    status: 'completed',
    startedAtUtc: '2026-07-23T09:00:00Z',
    finishedAtUtc: '2026-07-23T09:10:00Z',
    baseScore: 100,
    finalScore: 100,
    emptyCardPenaltyApplied: false,
    killsCount: 1,
    bountyCount: 0,
    cellId: 'cell-1',
    cellRowIndex: 0,
    cellColIndex: 0,
    cellType: 'question',
    cellTitle: 'Card',
    cellDescription: null,
    cellCost: 100,
    notes: null,
    cellMedia: [],
    participants: [],
    modifiers: [],
    ...overrides,
  }
}
