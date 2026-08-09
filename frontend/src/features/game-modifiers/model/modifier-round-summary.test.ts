import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { deriveModifierRoundSummaryMeta } from './modifier-round-summary.ts'

function createModifier(overrides: Partial<GameModifierDefinition> = {}): GameModifierDefinition {
  return {
    id: 'modifier-1',
    name: 'Modifier',
    description: 'Description',
    scoringType: 'non_scoring',
    category: 'round',
    requiresHostControl: false,
    mechanicType: 'rule_only',
    activationCost: 5,
    defaultLimitPerGame: 1,
    activationLimit: { count: 1 },
    conflictingModifierIds: [],
    iconEmoji: null,
    activationCommand: null,
    effect: {
      mechanicType: 'rule_only',
      traits: [],
      durationSeconds: null,
      ruleText: null,
      scoreImpact: null,
      conditions: [],
      resolutionInputs: [],
      killEffect: null,
      multiplierEffect: null,
      mentorEffect: null,
    },
    ...overrides,
  }
}

describe('deriveModifierRoundSummaryMeta', () => {
  it('marks pure rule modifiers as passive', () => {
    expect(deriveModifierRoundSummaryMeta(createModifier())).toMatchObject({
      type: 'passive',
      includeInRoundSummary: false,
    })
  })

  it('detects automatic result modifiers from per-kill bonus and failure penalty', () => {
    const modifier = createModifier({
      scoringType: 'conditional_bonus_penalty',
      category: 'result',
      mechanicType: 'restriction_with_reward',
      effect: {
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
        },
        conditions: [{ type: 'at_least_one_kill', source: 'manual_input' }],
        resolutionInputs: ['kills'],
        killEffect: null,
        multiplierEffect: null,
        mentorEffect: null,
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'auto_result',
      includeInRoundSummary: true,
      autoResultFormula: 'flat_per_kill',
      perKillBonus: 5,
      failurePenaltyPoints: 25,
    })
  })

  it('uses stacking kill formula from modifier traits', () => {
    const modifier = createModifier({
      scoringType: 'conditional_bonus_penalty',
      category: 'result',
      mechanicType: 'restriction_with_reward',
      effect: {
        mechanicType: 'restriction_with_reward',
        traits: ['requires_manual_resolution', 'stacking_per_kill_bonus'],
        durationSeconds: null,
        ruleText: null,
        scoreImpact: {
          pointsDelta: null,
          perKillBonus: 5,
          failurePenaltyPoints: 25,
          multiplierDelta: null,
          killDelta: null,
        },
        conditions: [{ type: 'at_least_one_kill', source: 'manual_input' }],
        resolutionInputs: ['kills'],
        killEffect: null,
        multiplierEffect: null,
        mentorEffect: null,
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'auto_result',
      autoResultFormula: 'stacking_per_kill_bonus',
    })
  })

  it('keeps custom score expressions in the derived round-summary metadata', () => {
    const modifier = createModifier({
      scoringType: 'conditional_bonus_penalty',
      category: 'result',
      mechanicType: 'restriction_with_reward',
      effect: {
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
          scoreFormula: {
            mode: 'custom_expression',
            successExpression: 'killsCount * perKillBonus * activationCount',
            failureExpression: '-failurePenaltyPoints',
          },
        },
        conditions: [{ type: 'at_least_one_kill', source: 'manual_input' }],
        resolutionInputs: ['kills'],
        killEffect: null,
        multiplierEffect: null,
        mentorEffect: null,
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'auto_result',
      autoResultFormula: 'custom_expression',
      autoResultSuccessExpression: 'killsCount * perKillBonus * activationCount',
      autoResultFailureExpression: '-failurePenaltyPoints',
    })
  })

  it('detects binary bonus-kill modifiers', () => {
    const modifier = createModifier({
      scoringType: 'conditional_bonus',
      category: 'result',
      mechanicType: 'kill_counter',
      effect: {
        mechanicType: 'kill_counter',
        traits: ['requires_manual_resolution'],
        durationSeconds: null,
        ruleText: null,
        scoreImpact: {
          pointsDelta: null,
          perKillBonus: null,
          failurePenaltyPoints: null,
          multiplierDelta: null,
          killDelta: 1,
        },
        conditions: [{ type: 'first_kill_first_bullet', source: 'manual_input' }],
        resolutionInputs: ['kills'],
        killEffect: {
          killDeltaMode: 'conditional_bonus_kill',
          killDeltaValue: 1,
          condition: 'first_kill_first_bullet',
          excludedWeapons: [],
        },
        multiplierEffect: null,
        mentorEffect: null,
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'toggle_bonus',
      includeInRoundSummary: true,
      killDeltaValue: 1,
      conditionType: 'first_kill_first_bullet',
    })
  })

  it('detects mentor kill credit modifiers as counted bonuses', () => {
    const modifier = createModifier({
      scoringType: 'conditional_bonus',
      category: 'result',
      mechanicType: 'mentor',
      effect: {
        mechanicType: 'mentor',
        traits: ['requires_manual_resolution', 'kill_counter'],
        durationSeconds: null,
        ruleText: null,
        scoreImpact: {
          pointsDelta: null,
          perKillBonus: null,
          failurePenaltyPoints: null,
          multiplierDelta: null,
          killDelta: null,
        },
        conditions: [],
        resolutionInputs: ['mentorKills'],
        killEffect: {
          killDeltaMode: 'mentor_kills_as_team_kills',
          killDeltaValue: 1,
          condition: null,
          excludedWeapons: [],
        },
        multiplierEffect: null,
        mentorEffect: {
          loadoutText: 'Mentor loadout',
          durationSeconds: null,
          canBeRevived: false,
          canBeKilled: true,
          killsCreditToTeam: true,
        },
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'counted_bonus',
      includeInRoundSummary: true,
      countInput: 'mentorKills',
      killDeltaValue: 1,
    })
  })

  it('detects kill multipliers as separate round-summary category', () => {
    const modifier = createModifier({
      scoringType: 'multiplier',
      category: 'result',
      mechanicType: 'multiplier',
      effect: {
        mechanicType: 'multiplier',
        traits: ['requires_manual_resolution'],
        durationSeconds: null,
        ruleText: null,
        scoreImpact: {
          pointsDelta: null,
          perKillBonus: null,
          failurePenaltyPoints: null,
          multiplierDelta: 0.75,
          killDelta: null,
        },
        conditions: [{ type: 'until_health_restored', source: 'manual_input' }],
        resolutionInputs: ['killsDuringWindow'],
        killEffect: null,
        multiplierEffect: {
          target: 'kills',
          delta: 0.75,
          activeWindow: 'until_condition',
          stopCondition: 'health_restored',
        },
        mentorEffect: null,
      },
    })

    expect(deriveModifierRoundSummaryMeta(modifier)).toMatchObject({
      type: 'kill_multiplier',
      includeInRoundSummary: true,
      countInput: 'killsDuringWindow',
      multiplierDelta: 0.75,
    })
  })
})
