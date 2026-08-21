import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import {
  createDefaultModifierFormValues,
  createModifierFormSchema,
  getCompatibleModifierFormulaCodes,
  getCompatibleResolutionKinds,
  normalizeModifierTags,
  toModifierRequest,
} from './modifier-form-schema.ts'

const messages = {
  required: 'required',
  number: 'number',
  limit: 'limit',
  formula: 'formula',
  tags: 'tags',
}

describe('modifier wizard model', () => {
  it('exposes only formulas and resolutions compatible with the selected reward', () => {
    expect(getCompatibleResolutionKinds('points')).toEqual([
      'automaticRoundMetric',
      'nonNegativeCount',
    ])
    expect(getCompatibleModifierFormulaCodes('points', 'automaticRoundMetric')).toEqual([
      'growing_kill_value',
    ])
    expect(getCompatibleModifierFormulaCodes('bonusKills', 'boolean')).toEqual([
      'bonus_kill_on_condition',
    ])
    expect(getCompatibleModifierFormulaCodes('bonusKills', 'nonNegativeCount')).toEqual([
      'bonus_kills_by_count',
    ])
  })

  it('normalizes tags with NFKC, collapsed whitespace and first-value casing', () => {
    expect(normalizeModifierTags(['  Бой   вблизи ', 'БОЙ ВБЛИЗИ', 'ＡＢＣ', 'ABC'])).toEqual([
      'Бой вблизи',
      'ABC',
    ])
  })

  it('rejects more than five tags and accepts 32-grapheme emoji tags', () => {
    const schema = createModifierFormSchema(messages)
    const base = {
      ...createDefaultModifierFormValues(),
      name: 'Rule',
      description: 'Rule description',
      rule: 'Complete the rule.',
    }
    expect(schema.safeParse({ ...base, tags: ['a', 'b', 'c', 'd', 'e', 'f'] }).success).toBe(false)
    expect(schema.safeParse({ ...base, tags: ['👨‍👩‍👧‍👦'.repeat(32)] }).success).toBe(true)
  })

  it('clears hidden preset parameters from the typed request', () => {
    const values = {
      ...createDefaultModifierFormValues(),
      kind: 'scoring' as const,
      name: 'Count bonus',
      description: 'Count bonus description',
      rule: 'Count the completed actions.',
      reward: 'bonusKills' as const,
      resolutionKind: 'nonNegativeCount' as const,
      formulaCode: 'bonus_kills_by_count' as const,
      incrementPointsPerKill: '999',
      zeroKillPenaltyPoints: '999',
      bonusKillsPerUnit: '2',
    }

    const request = toModifierRequest(values)

    expect(request.behaviorV2?.formulaReference?.parameters).toEqual({
      type: 'bonusKillsByCount',
      bonusKillsPerUnit: 2,
    })
    expect(request.behaviorV2?.formulaReference?.parameters).not.toHaveProperty(
      'incrementPointsPerKill',
    )
  })

  it('round-trips an existing typed modifier through the edit model', () => {
    const initial = {
      id: crypto.randomUUID(),
      category: 'result',
      name: 'Shot',
      description: 'Bonus kill on success.',
      activationCost: 3,
      activationLimit: { count: 2 },
      conflictingModifierIds: [],
      iconEmoji: '🎯',
      activationCommand: '!shot',
      isLockedByActiveGame: false,
      revision: 4,
      normalizedTags: ['weapon'],
      behaviorV2: {
        schemaVersion: 2,
        kind: 'scoring',
        phase: 'result',
        performer: 'activeTeam',
        requiresHostMonitoring: true,
        rule: 'Complete the shot.',
        stackingPolicy: 'independentInstances',
        resolution: { type: 'boolean' },
        reward: 'bonusKills',
        formulaReference: {
          code: 'bonus_kill_on_condition',
          version: 1,
          parameters: { type: 'bonusKillOnCondition', successBonusKills: 1 },
        },
      },
    } satisfies GameModifierDefinition

    const request = toModifierRequest(createDefaultModifierFormValues(initial))

    expect(request.behaviorV2).toEqual(initial.behaviorV2)
    expect(request.normalizedTags).toEqual(['weapon'])
    expect(request.activationCommand).toBe('!shot')
  })
})
