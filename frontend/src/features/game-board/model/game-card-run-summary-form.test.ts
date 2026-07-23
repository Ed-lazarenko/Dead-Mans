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
        modifierResultIds: ['modifier-result-1'],
        modifierId: 'modifier-1',
        modifierName: 'Momentum',
        modifierDescription: 'Manual score adjustment.',
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
    ])
  })

  it('builds score preview and finalize payload from manual results', () => {
    const run = createRun()
    const values = {
      killsCount: 2,
      bountyCount: 1,
      modifiers: [
        {
          modifierResultIds: ['modifier-result-1'],
          modifierId: 'modifier-1',
          modifierName: 'Momentum',
          activationCount: 1,
          roundSummaryType: 'manual_points' as const,
          outcomeStatus: 'completed' as const,
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

  it('stacks duplicate modifier snapshots into one form row and expands them back for finalize payload', () => {
    const run = createRun({
      modifierResults: [
        {
          modifierResultId: 'modifier-result-1',
          modifierId: 'modifier-1',
          modifierName: 'Жажда',
          modifierCategory: 'result',
          modifierMechanicType: 'restriction_with_reward',
          modifierDescription: 'Stacks on kills.',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: {
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
          },
          outcomeStatus: 'pending',
          scoreDelta: 0,
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: null,
          resolvedByUserId: null,
          resolvedAtUtc: null,
        },
        {
          modifierResultId: 'modifier-result-2',
          modifierId: 'modifier-1',
          modifierName: 'Жажда',
          modifierCategory: 'result',
          modifierMechanicType: 'restriction_with_reward',
          modifierDescription: 'Stacks on kills.',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: {
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
          },
          outcomeStatus: 'pending',
          scoreDelta: 0,
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: null,
          resolvedByUserId: null,
          resolvedAtUtc: null,
        },
      ],
    })
    const defaults = buildGameCardRunSummaryDefaultValues(run)

    expect(defaults.modifiers).toEqual([
      expect.objectContaining({
        modifierResultIds: ['modifier-result-1', 'modifier-result-2'],
        modifierId: 'modifier-1',
        modifierName: 'Жажда',
        modifierDescription: 'Stacks on kills.',
        activationCount: 2,
        roundSummaryType: 'auto_result',
      }),
    ])

    const values = {
      ...defaults,
      killsCount: 3,
      bountyCount: 1,
    }

    const preview = buildGameCardRunScorePreview(run.baseScore, values)
    const payload = buildCompleteRoundInput(run, values)

    expect(preview.modifierScoreDelta).toBe(30)
    expect(preview.finalScore).toBe(430)
    expect(payload.modifierResults).toEqual([
      expect.objectContaining({
        modifierResultId: 'modifier-result-1',
        outcomeStatus: 'completed',
        scoreDelta: 15,
        killDelta: 0,
      }),
      expect.objectContaining({
        modifierResultId: 'modifier-result-2',
        outcomeStatus: 'completed',
        scoreDelta: 15,
        killDelta: 0,
      }),
    ])
  })

  it('uses the stacking kill formula for Жажда so the kill bonus increases the value of every kill', () => {
    const run = createRun({
      baseScore: 100,
      killsCount: 0,
      bountyCount: 0,
      modifierResults: [
        {
          modifierResultId: 'modifier-result-1',
          modifierId: '10000000-0000-0000-0000-000000000002',
          modifierName: 'Жажда',
          modifierCategory: 'result',
          modifierMechanicType: 'restriction_with_reward',
          modifierDescription: 'Kills increase the value of every kill.',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: {
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
          },
          outcomeStatus: 'pending',
          scoreDelta: 0,
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: null,
          resolvedByUserId: null,
          resolvedAtUtc: null,
        },
      ],
    })
    const defaults = buildGameCardRunSummaryDefaultValues(run)
    const preview = buildGameCardRunScorePreview(run.baseScore, {
      ...defaults,
      killsCount: 5,
      bountyCount: 0,
    })

    expect(preview.killsScore).toBe(500)
    expect(preview.modifierScoreDelta).toBe(125)
    expect(preview.finalScore).toBe(625)
  })

  it('evaluates a custom modifier formula with the current round variables', () => {
    const run = createRun({
      baseScore: 120,
      killsCount: 0,
      bountyCount: 0,
      modifierResults: [
        {
          modifierResultId: 'modifier-result-1',
          modifierId: 'modifier-custom',
          modifierName: 'Custom Formula',
          modifierCategory: 'result',
          modifierMechanicType: 'restriction_with_reward',
          modifierDescription: 'Uses a custom score formula.',
          modifierScoringType: 'conditional_bonus_penalty',
          modifierEffect: {
            mechanicType: 'restriction_with_reward',
            traits: ['requires_manual_resolution'],
            durationSeconds: null,
            ruleText: null,
            scoreImpact: {
              pointsDelta: null,
              perKillBonus: 10,
              failurePenaltyPoints: 50,
              multiplierDelta: null,
              killDelta: null,
              scoreFormula: {
                mode: 'custom_expression',
                successExpression: 'killsCount * scoreUnit + bountyCount * 40',
                failureExpression: '-failurePenaltyPoints',
              },
            },
            conditions: [{ type: 'at_least_one_kill', source: 'manual_input' }],
            resolutionInputs: ['kills'],
            killEffect: null,
            multiplierEffect: null,
            mentorEffect: null,
          },
          outcomeStatus: 'pending',
          scoreDelta: 0,
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: null,
          resolvedByUserId: null,
          resolvedAtUtc: null,
        },
      ],
    })
    const defaults = buildGameCardRunSummaryDefaultValues(run)
    const successPreview = buildGameCardRunScorePreview(run.baseScore, {
      ...defaults,
      killsCount: 2,
      bountyCount: 1,
    })
    const failurePreview = buildGameCardRunScorePreview(run.baseScore, {
      ...defaults,
      killsCount: 0,
      bountyCount: 0,
    })

    expect(successPreview.modifierScoreDelta).toBe(280)
    expect(failurePreview.modifierScoreDelta).toBe(-50)
  })
})
