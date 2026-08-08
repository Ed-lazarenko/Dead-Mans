import { describe, expect, it } from 'vitest'
import {
  findLatestCardPlayResultRound,
  type GameBoardCardPlayResultRound,
} from './use-card-play-result.ts'

describe('findLatestCardPlayResultRound', () => {
  it('returns the latest played round for the selected cell', () => {
    const round = findLatestCardPlayResultRound(
      [
        createRound({
          roundId: 'older',
          cellId: 'cell-1',
          finishedAtUtc: '2026-07-23T10:00:00Z',
        }),
        createRound({
          roundId: 'other-cell',
          cellId: 'cell-2',
          finishedAtUtc: '2026-07-23T12:00:00Z',
        }),
        createRound({
          roundId: 'latest',
          cellId: 'cell-1',
          finishedAtUtc: '2026-07-23T11:00:00Z',
        }),
      ],
      'cell-1',
    )

    expect(round?.roundId).toBe('latest')
  })

  it('falls back to the start time when a round has no finish time', () => {
    const round = findLatestCardPlayResultRound(
      [
        createRound({
          roundId: 'finished',
          finishedAtUtc: '2026-07-23T10:00:00Z',
        }),
        createRound({
          roundId: 'started-later',
          startedAtUtc: '2026-07-23T11:00:00Z',
          finishedAtUtc: null,
        }),
      ],
      'cell-1',
    )

    expect(round?.roundId).toBe('started-later')
  })
})

function createRound(
  overrides: Partial<GameBoardCardPlayResultRound> = {},
): GameBoardCardPlayResultRound {
  return {
    roundId: 'round-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    teamName: null,
    status: 'completed',
    startedAtUtc: '2026-07-23T09:00:00Z',
    finishedAtUtc: '2026-07-23T09:10:00Z',
    baseScore: 100,
    finalScore: 100,
    killsCount: 0,
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
