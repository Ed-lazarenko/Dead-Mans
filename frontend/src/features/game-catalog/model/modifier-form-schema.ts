import { z } from 'zod'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'

export const modifierKinds = ['rule', 'scoring'] as const
export const modifierPhases = ['preparation', 'round', 'result'] as const
const modifierPerformers = ['activeTeam', 'mentor'] as const
export const modifierMeasurementDomains = ['kills', 'event'] as const
export const modifierKillMeasurementModes = ['all', 'qualifying'] as const
export const modifierEventMeasurementModes = ['condition', 'count', 'perActivation'] as const
export const modifierEventMaximumKinds = ['none', 'activations'] as const
export const modifierPayoutKinds = [
  'fixedPoints',
  'cardPercent',
  'bonusKills',
  'killValueIncrease',
] as const
export const modifierPayoutDefaultValues = {
  fixedPoints: '10',
  cardPercent: '75',
  bonusKills: '1',
  killValueIncrease: '5',
} satisfies Record<(typeof modifierPayoutKinds)[number], string>

export const suggestedModifierTags = [
  'combat',
  'mentor',
  'movement',
  'equipment',
  'communication',
  'revival',
  'environment',
  'restriction',
  'weapon',
  'bonus',
  'penalty',
  'timer',
] as const

function graphemeLength(value: string) {
  return [...new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(value)].length
}

export function normalizeModifierTags(values: readonly string[]) {
  const normalized: string[] = []
  const keys = new Set<string>()
  for (const value of values) {
    const display = value.normalize('NFKC').trim().replace(/\s+/gu, ' ')
    const key = display.toLowerCase()
    if (display !== '' && !keys.has(key)) {
      keys.add(key)
      normalized.push(display)
    }
  }
  return normalized
}

interface ModifierFormSchemaMessages {
  required: string
  number: string
  limit: string
  tags: string
}
const numericText = /^-?\d+([.,]\d+)?$/

export function createModifierFormSchema(messages: ModifierFormSchemaMessages) {
  return z
    .object({
      kind: z.enum(modifierKinds),
      name: z.string().trim().min(1, messages.required).max(128, messages.required),
      description: z.string().trim().min(1, messages.required).max(2000, messages.required),
      iconEmoji: z.string().max(16),
      tags: z.array(z.string()),
      activationCost: z.string().regex(/^\d+$/, messages.number),
      activationLimitCount: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      phase: z.enum(modifierPhases),
      performer: z.enum(modifierPerformers),
      rule: z.string().trim().min(1, messages.required).max(2000, messages.required),
      requiresHostMonitoring: z.boolean(),
      durationEnabled: z.boolean(),
      durationSeconds: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      conflictingModifierIds: z.array(z.string()),
      activationCommand: z.string().max(128),
      measurementDomain: z.enum(modifierMeasurementDomains).nullable(),
      killMeasurementMode: z.enum(modifierKillMeasurementModes),
      eventMeasurementMode: z.enum(modifierEventMeasurementModes),
      eventInputLabel: z.string().trim().max(128, messages.required),
      eventMaximumKind: z.enum(modifierEventMaximumKinds),
      eventsPerActivation: z.string().regex(/^[1-9]\d*$/, messages.limit),
      payoutKind: z.enum(modifierPayoutKinds).nullable(),
      payoutValue: z.string().regex(numericText, messages.number),
      zeroCountPenaltyPoints: z.string().regex(/^\d+$/, messages.number),
    })
    .superRefine((values, context) => {
      const tags = normalizeModifierTags(values.tags)
      if (tags.length > 5 || tags.some((tag) => graphemeLength(tag) > 32)) {
        context.addIssue({ code: 'custom', path: ['tags'], message: messages.tags })
      }
      if (values.kind === 'rule' && values.durationEnabled && values.durationSeconds === '') {
        context.addIssue({ code: 'custom', path: ['durationSeconds'], message: messages.required })
      }
      if (values.kind !== 'scoring') return
      if (values.measurementDomain === null) {
        context.addIssue({
          code: 'custom',
          path: ['measurementDomain'],
          message: messages.required,
        })
      }
      if (values.payoutKind === null) {
        context.addIssue({ code: 'custom', path: ['payoutKind'], message: messages.required })
        return
      }
      const needsInputLabel =
        (values.measurementDomain === 'kills' && values.killMeasurementMode === 'qualifying') ||
        (values.measurementDomain === 'event' && values.eventMeasurementMode !== 'perActivation')
      if (needsInputLabel && values.eventInputLabel.trim() === '') {
        context.addIssue({ code: 'custom', path: ['eventInputLabel'], message: messages.required })
      }
      const payout = Number.parseFloat(values.payoutValue.replace(',', '.'))
      if (!Number.isFinite(payout) || payout === 0) {
        context.addIssue({ code: 'custom', path: ['payoutValue'], message: messages.number })
      }
      if (values.payoutKind === 'fixedPoints' && !Number.isInteger(payout)) {
        context.addIssue({ code: 'custom', path: ['payoutValue'], message: messages.limit })
      }
      if (
        (values.payoutKind === 'bonusKills' || values.payoutKind === 'killValueIncrease') &&
        (!Number.isInteger(payout) || payout < 1)
      ) {
        context.addIssue({ code: 'custom', path: ['payoutValue'], message: messages.limit })
      }
    })
}

export type ModifierFormValues = z.infer<ReturnType<typeof createModifierFormSchema>>
type Resolution = GameModifierDefinition['behaviorV2']['resolution']
type Formula = NonNullable<GameModifierDefinition['behaviorV2']['formulaReference']>

function inferMeasurement(resolution: Resolution | undefined, formula: Formula | undefined) {
  const defaults = {
    measurementDomain: null as 'kills' | 'event' | null,
    killMeasurementMode: 'all' as const,
    eventMeasurementMode: 'count' as const,
    eventInputLabel: '',
    eventMaximumKind: 'none' as const,
    eventsPerActivation: '1',
  }
  if (!resolution || resolution.type === 'ruleStatus') return defaults
  if (resolution.type === 'nonNegativeCount' && formula?.code === 'window_kill_bonus_points') {
    return {
      ...defaults,
      measurementDomain: 'kills' as const,
      killMeasurementMode: 'qualifying' as const,
      eventInputLabel: resolution.inputLabel ?? '',
    }
  }
  if (resolution.type === 'automaticRoundMetric')
    return { ...defaults, measurementDomain: 'kills' as const }
  if (resolution.type === 'boolean')
    return {
      ...defaults,
      measurementDomain: 'event' as const,
      eventMeasurementMode: 'condition' as const,
      eventInputLabel: resolution.inputLabel ?? '',
    }
  if (resolution.type === 'perActivation')
    return {
      ...defaults,
      measurementDomain: 'event' as const,
      eventMeasurementMode: 'perActivation' as const,
    }
  const qualifyingKills = resolution.maximumKind === 'resolvedKills'
  return {
    ...defaults,
    measurementDomain: qualifyingKills ? ('kills' as const) : ('event' as const),
    killMeasurementMode: qualifyingKills ? ('qualifying' as const) : ('all' as const),
    eventInputLabel: resolution.inputLabel ?? '',
    eventMaximumKind:
      resolution.maximumKind === 'activations' ? ('activations' as const) : ('none' as const),
    eventsPerActivation: String(resolution.maximumPerActivation ?? 1),
  }
}

function inferPayout(formula: Formula | undefined) {
  const parameters = formula?.parameters
  const fallback = {
    payoutKind: null as (typeof modifierPayoutKinds)[number] | null,
    payoutValue: modifierPayoutDefaultValues.bonusKills,
    zeroCountPenaltyPoints: '0',
  }
  if (!parameters) return fallback
  if (parameters.type === 'fixedPointsPerUnit')
    return {
      payoutKind: 'fixedPoints' as const,
      payoutValue: String(parameters.pointsPerUnit),
      zeroCountPenaltyPoints: '0',
    }
  if (parameters.type === 'cardPercentPerUnit')
    return {
      payoutKind: 'cardPercent' as const,
      payoutValue: String(parameters.rate * 100),
      zeroCountPenaltyPoints: '0',
    }
  if (parameters.type === 'bonusKillsPerUnit')
    return {
      payoutKind: 'bonusKills' as const,
      payoutValue: String(parameters.bonusKillsPerUnit),
      zeroCountPenaltyPoints: '0',
    }
  if (parameters.type === 'killValueIncreasePerUnit')
    return {
      payoutKind: 'killValueIncrease' as const,
      payoutValue: String(parameters.incrementPointsPerUnit),
      zeroCountPenaltyPoints: String(parameters.zeroCountPenaltyPoints),
    }
  if (parameters.type === 'growingKillValue')
    return {
      payoutKind: 'killValueIncrease' as const,
      payoutValue: String(parameters.incrementPointsPerKill),
      zeroCountPenaltyPoints: String(parameters.zeroKillPenaltyPoints),
    }
  if (parameters.type === 'windowKillBonusPoints')
    return {
      payoutKind: 'cardPercent' as const,
      payoutValue: String(parameters.bonusRate * 100),
      zeroCountPenaltyPoints: '0',
    }
  if (parameters.type === 'bonusKillOnCondition')
    return {
      payoutKind: 'bonusKills' as const,
      payoutValue: String(parameters.successBonusKills),
      zeroCountPenaltyPoints: '0',
    }
  return {
    payoutKind: 'bonusKills' as const,
    payoutValue: String(parameters.bonusKillsPerUnit),
    zeroCountPenaltyPoints: '0',
  }
}

export function createDefaultModifierFormValues(
  initial?: GameModifierDefinition,
): ModifierFormValues {
  const behavior = initial?.behaviorV2
  return {
    kind: behavior?.kind ?? 'rule',
    name: initial?.name ?? '',
    description: initial?.description ?? '',
    iconEmoji: initial?.iconEmoji ?? '',
    tags: initial?.normalizedTags ?? [],
    activationCost: String(initial?.activationCost ?? 0),
    activationLimitCount:
      initial?.activationLimit.count == null ? '' : String(initial.activationLimit.count),
    phase: behavior?.phase ?? 'round',
    performer: behavior?.performer ?? 'activeTeam',
    rule: behavior?.rule ?? '',
    requiresHostMonitoring: behavior?.requiresHostMonitoring ?? false,
    durationEnabled: behavior?.durationSecondsPerActivation != null,
    durationSeconds:
      behavior?.durationSecondsPerActivation == null
        ? ''
        : String(behavior.durationSecondsPerActivation),
    conflictingModifierIds: initial?.conflictingModifierIds ?? [],
    activationCommand: initial?.activationCommand ?? '',
    ...inferMeasurement(behavior?.resolution, behavior?.formulaReference ?? undefined),
    ...inferPayout(behavior?.formulaReference ?? undefined),
  }
}

function optionalPositiveInteger(value: string) {
  return value.trim() === '' ? null : Number.parseInt(value, 10)
}
function parseNumber(value: string) {
  return Number.parseFloat(value.replace(',', '.'))
}

function buildBehavior(values: ModifierFormValues) {
  if (values.kind === 'rule')
    return {
      schemaVersion: 2 as const,
      kind: 'rule' as const,
      phase: values.phase,
      performer: values.performer,
      requiresHostMonitoring: values.requiresHostMonitoring,
      rule: values.rule.trim(),
      stackingPolicy: 'aggregateParameters' as const,
      resolution: { type: 'ruleStatus' as const },
      reward: 'none' as const,
      formulaReference: null,
      ...(!values.durationEnabled || optionalPositiveInteger(values.durationSeconds) === null
        ? {}
        : { durationSecondsPerActivation: optionalPositiveInteger(values.durationSeconds) }),
    }
  if (values.measurementDomain === null || values.payoutKind === null)
    throw new Error('Scoring modifier measurement and payout are required')

  const resolution =
    values.measurementDomain === 'kills'
      ? values.killMeasurementMode === 'all'
        ? { type: 'automaticRoundMetric' as const, metric: 'killsCount' as const }
        : {
            type: 'nonNegativeCount' as const,
            inputLabel: values.eventInputLabel.trim(),
            maximumKind: 'resolvedKills' as const,
            maximumPerActivation: null,
          }
      : values.eventMeasurementMode === 'condition'
        ? { type: 'boolean' as const, inputLabel: values.eventInputLabel.trim() }
        : values.eventMeasurementMode === 'perActivation'
          ? { type: 'perActivation' as const }
          : {
              type: 'nonNegativeCount' as const,
              inputLabel: values.eventInputLabel.trim(),
              maximumKind: values.eventMaximumKind,
              maximumPerActivation:
                values.eventMaximumKind === 'activations'
                  ? Number.parseInt(values.eventsPerActivation, 10)
                  : null,
            }
  const amount = parseNumber(values.payoutValue)
  const formulaReference =
    values.payoutKind === 'fixedPoints'
      ? {
          code: 'fixed_points_per_unit' as const,
          version: 1 as const,
          parameters: { type: 'fixedPointsPerUnit' as const, pointsPerUnit: amount },
        }
      : values.payoutKind === 'cardPercent'
        ? {
            code: 'card_percent_per_unit' as const,
            version: 1 as const,
            parameters: { type: 'cardPercentPerUnit' as const, rate: amount / 100 },
          }
        : values.payoutKind === 'bonusKills'
          ? {
              code: 'bonus_kills_per_unit' as const,
              version: 1 as const,
              parameters: { type: 'bonusKillsPerUnit' as const, bonusKillsPerUnit: amount },
            }
          : {
              code: 'kill_value_increase_per_unit' as const,
              version: 1 as const,
              parameters: {
                type: 'killValueIncreasePerUnit' as const,
                incrementPointsPerUnit: amount,
                zeroCountPenaltyPoints: Number.parseInt(values.zeroCountPenaltyPoints, 10),
              },
            }
  return {
    schemaVersion: 2 as const,
    kind: 'scoring' as const,
    phase: values.phase,
    performer: values.performer,
    requiresHostMonitoring: values.requiresHostMonitoring,
    rule: values.rule.trim(),
    stackingPolicy: 'independentInstances' as const,
    resolution,
    reward: values.payoutKind === 'bonusKills' ? ('bonusKills' as const) : ('points' as const),
    formulaReference,
  }
}

export function toModifierRequest(values: ModifierFormValues): CreateGameModifierRequest {
  return {
    name: values.name.trim(),
    description: values.description.trim(),
    category: values.phase,
    activationCost: Number.parseInt(values.activationCost, 10),
    activationLimit: { count: optionalPositiveInteger(values.activationLimitCount) },
    conflictingModifierIds: values.conflictingModifierIds,
    iconEmoji: values.iconEmoji.trim() || null,
    activationCommand: values.activationCommand.trim() || null,
    normalizedTags: normalizeModifierTags(values.tags),
    behaviorV2: buildBehavior(values),
  }
}
