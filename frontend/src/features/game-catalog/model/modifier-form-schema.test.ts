import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import {
  createDefaultModifierFormValues,
  createModifierFormSchema,
  normalizeModifierTags,
  toModifierRequest,
} from './modifier-form-schema.ts'

const messages = { required: 'required', number: 'number', limit: 'limit', tags: 'tags' }
const scoring = () => ({
  ...createDefaultModifierFormValues(),
  kind: 'scoring' as const,
  name: 'Modifier',
  description: 'Description',
  rule: 'Apply the effect.',
  measurementDomain: 'event' as const,
  eventMeasurementMode: 'count' as const,
  eventInputLabel: 'Successful actions',
  payoutKind: 'fixedPoints' as const,
  payoutValue: '10',
})

describe('modifier wizard model', () => {
  it('normalizes tags with NFKC, collapsed whitespace and first-value casing', () => {
    expect(normalizeModifierTags(['  Бой   вблизи ', 'БОЙ ВБЛИЗИ', 'ＡＢＣ', 'ABC'])).toEqual([
      'Бой вблизи',
      'ABC',
    ])
  })

  it('requires a source, payout and a concrete manual-input label', () => {
    const schema = createModifierFormSchema(messages)
    expect(schema.safeParse({ ...scoring(), eventInputLabel: '' }).success).toBe(false)
    expect(schema.safeParse(scoring()).success).toBe(true)
  })

  it('creates arbitrary fixed points per manually counted event', () => {
    const behavior = toModifierRequest({ ...scoring(), payoutValue: '15' }).behaviorV2
    expect(behavior).toMatchObject({
      resolution: {
        type: 'nonNegativeCount',
        inputLabel: 'Successful actions',
        maximumKind: 'none',
      },
      reward: 'points',
      formulaReference: {
        code: 'fixed_points_per_unit',
        parameters: { type: 'fixedPointsPerUnit', pointsPerUnit: 15 },
      },
    })
  })

  it('creates a fixed-point penalty without a special-case formula', () => {
    const values = { ...scoring(), payoutValue: '-10' }
    const parsed = createModifierFormSchema(messages).safeParse(values)

    expect(parsed.success).toBe(true)
    expect(toModifierRequest(parsed.data!).behaviorV2.formulaReference).toEqual({
      code: 'fixed_points_per_unit',
      version: 1,
      parameters: { type: 'fixedPointsPerUnit', pointsPerUnit: -10 },
    })
  })

  it('creates Hard75 without coupling the source to its payout', () => {
    const behavior = toModifierRequest({
      ...scoring(),
      measurementDomain: 'kills',
      killMeasurementMode: 'qualifying',
      eventInputLabel: 'Kills before healing',
      payoutKind: 'cardPercent',
      payoutValue: '75',
    }).behaviorV2
    expect(behavior).toMatchObject({
      resolution: { type: 'nonNegativeCount', maximumKind: 'resolvedKills' },
      formulaReference: { code: 'card_percent_per_unit', parameters: { rate: 0.75 } },
    })
  })

  it('creates Thirst with automatic kills and growing kill value', () => {
    const behavior = toModifierRequest({
      ...scoring(),
      measurementDomain: 'kills',
      killMeasurementMode: 'all',
      payoutKind: 'killValueIncrease',
      payoutValue: '5',
      zeroCountPenaltyPoints: '25',
    }).behaviorV2
    expect(behavior).toMatchObject({
      resolution: { type: 'automaticRoundMetric', metric: 'killsCount' },
      formulaReference: {
        code: 'kill_value_increase_per_unit',
        parameters: { incrementPointsPerUnit: 5, zeroCountPenaltyPoints: 25 },
      },
    })
  })

  it('creates Lucky Shot as one aggregate count capped by activations', () => {
    const behavior = toModifierRequest({
      ...scoring(),
      eventInputLabel: 'Successful host kills',
      eventMaximumKind: 'activations',
      eventsPerActivation: '1',
      payoutKind: 'bonusKills',
      payoutValue: '1',
    }).behaviorV2
    expect(behavior).toMatchObject({
      resolution: { type: 'nonNegativeCount', maximumKind: 'activations', maximumPerActivation: 1 },
      reward: 'bonusKills',
      formulaReference: { code: 'bonus_kills_per_unit' },
    })
  })

  it('migrates a legacy formula to the generic representation while editing', () => {
    const initial = {
      id: crypto.randomUUID(),
      category: 'result',
      name: 'Shot',
      description: 'Bonus kill.',
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
    expect(request.behaviorV2).toMatchObject({
      resolution: { type: 'boolean' },
      formulaReference: { code: 'bonus_kills_per_unit', parameters: { bonusKillsPerUnit: 1 } },
    })
  })

  it('preserves the qualifying-kill meaning of a legacy percentage formula while editing', () => {
    const initial = {
      id: crypto.randomUUID(),
      category: 'result',
      name: 'Legacy percentage',
      description: 'Percentage for matching kills.',
      activationCost: 1,
      activationLimit: { count: null },
      conflictingModifierIds: [],
      iconEmoji: null,
      activationCommand: null,
      isLockedByActiveGame: false,
      revision: 1,
      normalizedTags: [],
      behaviorV2: {
        schemaVersion: 2,
        kind: 'scoring',
        phase: 'result',
        performer: 'activeTeam',
        requiresHostMonitoring: true,
        rule: 'Count matching kills.',
        stackingPolicy: 'independentInstances',
        resolution: { type: 'nonNegativeCount', inputLabel: 'Matching kills' },
        reward: 'points',
        formulaReference: {
          code: 'window_kill_bonus_points',
          version: 1,
          parameters: { type: 'windowKillBonusPoints', bonusRate: 0.75 },
        },
      },
    } satisfies GameModifierDefinition

    const request = toModifierRequest(createDefaultModifierFormValues(initial))

    expect(request.behaviorV2).toMatchObject({
      resolution: {
        type: 'nonNegativeCount',
        inputLabel: 'Matching kills',
        maximumKind: 'resolvedKills',
      },
      formulaReference: {
        code: 'card_percent_per_unit',
        parameters: { type: 'cardPercentPerUnit', rate: 0.75 },
      },
    })
  })
})
