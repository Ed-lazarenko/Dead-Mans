import { describe, expect, it } from 'vitest'
import { buildGameTeamLeaderboard, getCardRunBonusDelta } from './game-history-team-leaderboard.ts'

describe('buildGameTeamLeaderboard', () => {
  it('ranks teams by their best round result instead of total score', () => {
    const leaderboard = buildGameTeamLeaderboard([
      createRun({
        cardRunId: 'run-a-1',
        teamId: 'team-a',
        teamSlotIndex: 1,
        baseScore: 100,
        finalScore: 170,
        finishedAtUtc: '2026-07-23T10:00:00Z',
      }),
      createRun({
        cardRunId: 'run-a-2',
        teamId: 'team-a',
        teamSlotIndex: 1,
        baseScore: 100,
        finalScore: 120,
        finishedAtUtc: '2026-07-23T11:00:00Z',
      }),
      createRun({
        cardRunId: 'run-b-1',
        teamId: 'team-b',
        teamSlotIndex: 2,
        baseScore: 150,
        finalScore: 160,
        finishedAtUtc: '2026-07-23T12:00:00Z',
      }),
      createRun({
        cardRunId: 'run-c-1',
        teamId: 'team-c',
        teamSlotIndex: 3,
        status: 'in_progress',
        baseScore: 200,
        finalScore: 999,
        finishedAtUtc: null,
      }),
    ])

    expect(leaderboard).toHaveLength(2)
    expect(leaderboard[0]?.teamId).toBe('team-a')
    expect(leaderboard[0]?.bestScore).toBe(170)
    expect(leaderboard[0]?.roundsPlayed).toBe(2)
    expect(leaderboard[0]?.averageScore).toBe(145)
    expect(leaderboard[0]?.latestRun.cardRunId).toBe('run-a-2')
    expect(leaderboard[0]?.runs.map((run) => run.cardRunId)).toEqual(['run-a-2', 'run-a-1'])
    expect(leaderboard[1]?.teamId).toBe('team-b')
    expect(leaderboard[1]?.bestScore).toBe(160)
  })

  it('calculates bonus delta from the finalized score', () => {
    expect(
      getCardRunBonusDelta(
        createRun({
          baseScore: 100,
          finalScore: 145,
        }),
      ),
    ).toBe(45)
  })
})

function createRun(overrides: Partial<Parameters<typeof buildGameTeamLeaderboard>[0][number]> = {}) {
  return {
    cardRunId: 'run-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
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
