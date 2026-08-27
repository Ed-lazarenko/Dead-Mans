import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { deriveModifierRoundSummaryMeta } from './modifier-round-summary.ts'

function definition(behaviorV2: GameModifierDefinition['behaviorV2']): GameModifierDefinition {
  return {
    id: 'modifier',
    category: behaviorV2.phase,
    name: 'Modifier',
    description: 'Rule',
    activationCost: 1,
    activationLimit: { count: 1 },
    conflictingModifierIds: [],
    iconEmoji: null,
    activationCommand: null,
    isLockedByActiveGame: false,
    revision: 1,
    normalizedTags: [],
    behaviorV2,
  }
}

const base = {
  schemaVersion: 2 as const,
  phase: 'result' as const,
  performer: 'activeTeam' as const,
  requiresHostMonitoring: true,
  rule: 'Rule',
  stackingPolicy: 'independentInstances' as const,
}

describe('deriveModifierRoundSummaryMeta', () => {
  it('marks rules as passive', () => {
    expect(
      deriveModifierRoundSummaryMeta(
        definition({
          ...base,
          kind: 'rule',
          phase: 'round',
          stackingPolicy: 'aggregateParameters',
          resolution: { type: 'ruleStatus' },
          reward: 'none',
          formulaReference: null,
        }),
      ).type,
    ).toBe('passive')
  })

  it('maps the growing-kill formula to automatic result metadata', () => {
    const meta = deriveModifierRoundSummaryMeta(
      definition({
        ...base,
        kind: 'scoring',
        resolution: { type: 'automaticRoundMetric', metric: 'killsCount' },
        reward: 'points',
        formulaReference: {
          code: 'growing_kill_value',
          version: 1,
          parameters: {
            type: 'growingKillValue',
            incrementPointsPerKill: 5,
            zeroKillPenaltyPoints: 25,
          },
        },
      }),
    )
    expect(meta).toEqual({ type: 'automatic', includeInRoundSummary: true })
  })

  it('classifies manual input by resolution instead of a hard-coded formula', () => {
    const bonus = deriveModifierRoundSummaryMeta(
      definition({
        ...base,
        kind: 'scoring',
        performer: 'mentor',
        resolution: { type: 'nonNegativeCount' },
        reward: 'bonusKills',
        formulaReference: {
          code: 'bonus_kills_by_count',
          version: 1,
          parameters: { type: 'bonusKillsByCount', bonusKillsPerUnit: 1 },
        },
      }),
    )
    const window = deriveModifierRoundSummaryMeta(
      definition({
        ...base,
        kind: 'scoring',
        resolution: { type: 'nonNegativeCount' },
        reward: 'points',
        formulaReference: {
          code: 'window_kill_bonus_points',
          version: 1,
          parameters: { type: 'windowKillBonusPoints', bonusRate: 0.75 },
        },
      }),
    )
    expect(bonus).toEqual({ type: 'manual_count', includeInRoundSummary: true })
    expect(window).toEqual({ type: 'manual_count', includeInRoundSummary: true })
  })

  it('classifies a generic boolean formula as a condition', () => {
    const meta = deriveModifierRoundSummaryMeta(
      definition({
        ...base,
        kind: 'scoring',
        resolution: { type: 'boolean', inputLabel: 'Objective completed' },
        reward: 'points',
        formulaReference: {
          code: 'fixed_points_per_unit',
          version: 1,
          parameters: { type: 'fixedPointsPerUnit', pointsPerUnit: 25 },
        },
      }),
    )

    expect(meta).toEqual({ type: 'condition', includeInRoundSummary: true })
  })
})
