import { describe, expect, it } from 'vitest'
import type { components } from '../../../shared/api/contracts/generated'
import {
  buildCompleteRoundInput,
  buildGameCardRunScorePreview,
  buildGameCardRunSummaryDefaultValues,
} from './game-card-run-summary-form.ts'

type GameCardRunDetails = components['schemas']['GameCardRunDetailsDto']

function createRun(overrides: Partial<GameCardRunDetails> = {}): GameCardRunDetails {
  return {
    cardRunId: 'run-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    status: 'reviewing_results',
    startedAtUtc: '2026-07-23T10:00:00Z',
    finishedAtUtc: null,
    baseScore: 100,
    finalScore: null,
    killsCount: 2,
    bountyCount: 1,
    notes: null,
    participants: [],
    modifierResults: [
      {
        modifierResultId: 'modifier-result-1',
        modifierId: 'modifier-1',
        modifierName: 'Momentum',
        modifierCategory: 'round',
        modifierMechanicType: 'rule_only',
        outcomeStatus: 'pending',
        scoreDelta: 30,
        killDelta: 1,
        multiplierApplied: null,
        resolutionDataJson: null,
        resolvedByUserId: null,
        resolvedAtUtc: null,
      },
    ],
    ...overrides,
  }
}

describe('game-card-run-summary-form', () => {
  it('hydrates defaults from the active run snapshot', () => {
    const defaults = buildGameCardRunSummaryDefaultValues(createRun())

    expect(defaults.killsCount).toBe(2)
    expect(defaults.bountyCount).toBe(1)
    expect(defaults.modifiers).toEqual([
      {
        modifierResultId: 'modifier-result-1',
        modifierName: 'Momentum',
        outcomeStatus: 'completed',
        scoreDelta: 30,
        killDelta: 1,
      },
    ])
  })

  it('builds score preview and finalize payload from manual results', () => {
    const run = createRun()
    const values = {
      killsCount: 2,
      bountyCount: 1,
      modifiers: [
        {
          modifierResultId: 'modifier-result-1',
          modifierName: 'Momentum',
          outcomeStatus: 'completed' as const,
          scoreDelta: 30,
          killDelta: 1,
        },
      ],
    }

    const preview = buildGameCardRunScorePreview(run.baseScore, values)
    const payload = buildCompleteRoundInput(run, values)

    expect(preview.finalScore).toBe(430)
    expect(preview.totalKillCount).toBe(3)
    expect(payload).toEqual({
      cardRunId: 'run-1',
      finalScore: 430,
      killsCount: 2,
      bountyCount: 1,
      modifierResults: [
        {
          modifierResultId: 'modifier-result-1',
          outcomeStatus: 'completed',
          scoreDelta: 30,
          killDelta: 1,
          multiplierApplied: null,
          resolutionDataJson: null,
        },
      ],
    })
  })
})
