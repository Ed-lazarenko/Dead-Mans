import { describe, expect, it } from 'vitest'
import type { components } from '../../../shared/api/contracts/generated'
import {
  buildCompleteRoundInput,
  buildGameRoundSummaryDefaultValues,
} from './game-round-summary-form.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

function createRound(overrides: Partial<GameRoundDetails> = {}): GameRoundDetails {
  return {
    roundId: 'round-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamName: null,
    teamSlotIndex: 1,
    status: 'reviewing_results',
    startedAtUtc: '2026-07-23T10:00:00Z',
    finishedAtUtc: null,
    baseScore: 100,
    finalScore: null,
    emptyCardPenaltyApplied: false,
    scoreDetails: createScoreDetails(),
    killsCount: 2,
    bountyCount: 1,
    notes: null,
    participants: [],
    modifierResults: [createModifier()],
    ...overrides,
  }
}

describe('game-round-summary-form', () => {
  it('hydrates defaults from the active round snapshot', () => {
    const defaults = buildGameRoundSummaryDefaultValues(createRound())

    expect(defaults.killsCount).toBe(2)
    expect(defaults.bountyCount).toBe(1)
    expect(defaults.modifiers).toEqual([
      expect.objectContaining({
        modifierResultIds: ['modifier-result-1'],
        modifierId: 'modifier-1',
        modifierName: 'Momentum',
        roundSummaryType: 'manual_points',
        outcomeStatus: 'completed',
        manualScoreDelta: 30,
        manualKillDelta: 1,
      }),
    ])
  })

  it('builds finalize payload with source facts instead of calculated deltas', () => {
    const payload = buildCompleteRoundInput(createRound(), {
      killsCount: 2,
      bountyCount: 1,
      postRoundAction: 'continue',
      modifiers: [
        {
          modifierResultIds: ['modifier-result-1'],
          modifierId: 'modifier-1',
          modifierName: 'Momentum',
          modifierDescription: null,
          activationCount: 1,
          roundSummaryType: 'manual_points',
          outcomeStatus: 'completed',
          countInput: null,
          autoResultFormula: null,
          autoResultSuccessExpression: null,
          autoResultFailureExpression: null,
          countValue: 0,
          conditionType: null,
          isConditionMet: true,
          manualScoreDelta: 30,
          manualKillDelta: 1,
          perKillBonus: null,
          failurePenaltyPoints: null,
          killDeltaValue: 1,
          multiplierDelta: null,
        },
      ],
    })

    expect(payload).toEqual({
      roundId: 'round-1',
      killsCount: 2,
      bountyCount: 1,
      modifierResults: [
        {
          modifierResultId: 'modifier-result-1',
          outcomeStatus: 'completed',
          countValue: null,
          isConditionMet: null,
          manualScoreDelta: 30,
          manualKillDelta: 1,
          resolutionDataJson: null,
        },
      ],
    })
  })

  it('keeps duplicate automatic modifiers grouped and sends only ids for server scoring', () => {
    const round = createRound({
      modifierResults: [
        createModifier({
          modifierResultId: 'modifier-result-1',
          modifierId: 'modifier-zhazhda',
          modifierName: 'Жажда',
          modifierMechanicType: 'restriction_with_reward',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: createAutoResultEffect(),
          scoreDelta: 0,
          killDelta: 0,
        }),
        createModifier({
          modifierResultId: 'modifier-result-2',
          modifierId: 'modifier-zhazhda',
          modifierName: 'Жажда',
          modifierMechanicType: 'restriction_with_reward',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: createAutoResultEffect(),
          scoreDelta: 0,
          killDelta: 0,
        }),
      ],
    })

    const defaults = buildGameRoundSummaryDefaultValues(round)
    const payload = buildCompleteRoundInput(round, defaults)

    expect(defaults.modifiers).toEqual([
      expect.objectContaining({
        modifierResultIds: ['modifier-result-1', 'modifier-result-2'],
        activationCount: 2,
        roundSummaryType: 'auto_result',
      }),
    ])
    expect(payload.modifierResults.map((modifier) => modifier.modifierResultId)).toEqual([
      'modifier-result-1',
      'modifier-result-2',
    ])
    expect(payload.modifierResults[0]).not.toHaveProperty('scoreDelta')
    expect(payload.modifierResults[0]).not.toHaveProperty('killDelta')
  })
})

function createModifier(
  overrides: Partial<GameRoundDetails['modifierResults'][number]> = {},
): GameRoundDetails['modifierResults'][number] {
  return {
    modifierResultId: 'modifier-result-1',
    modifierId: 'modifier-1',
    modifierName: 'Momentum',
    modifierCategory: 'round',
    modifierMechanicType: 'rule_only',
    modifierDescription: 'Manual score adjustment.',
    modifierScoringType: 'non_scoring',
    modifierEffect: null,
    outcomeStatus: 'pending',
    scoreDelta: 30,
    killDelta: 1,
    multiplierApplied: null,
    resolutionDataJson: null,
    resolvedByUserId: null,
    resolvedAtUtc: null,
    ...overrides,
  }
}

function createAutoResultEffect(): NonNullable<
  GameRoundDetails['modifierResults'][number]['modifierEffect']
> {
  return {
    mechanicType: 'restriction_with_reward',
    traits: ['requires_manual_resolution'],
    durationSeconds: null,
    ruleText: null,
    scoreImpact: {
      pointsDelta: null,
      perKillBonus: 5,
      failurePenaltyPoints: 25,
      multiplierDelta: null,
      killDelta: null,
      scoreFormula: null,
    },
    conditions: [{ type: 'at_least_one_kill', source: 'manual_input' }],
    resolutionInputs: ['kills'],
    killEffect: null,
    multiplierEffect: null,
    mentorEffect: null,
  }
}

function createScoreDetails(): GameRoundDetails['scoreDetails'] {
  return {
    scoreUnit: 100,
    killsScore: 0,
    bountyScore: 0,
    modifierKillDelta: 0,
    modifierKillScore: 0,
    modifierScoreDelta: 0,
    emptyCardPenaltyApplied: false,
    emptyCardPenaltyScore: 0,
    penaltyTotal: 0,
    bonusDelta: 0,
    totalKillCount: 0,
    finalScore: 0,
  }
}
