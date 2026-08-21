import { z } from 'zod'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'

export const modifierKinds = ['rule', 'scoring'] as const
export const modifierPhases = ['preparation', 'round', 'result'] as const
export const modifierPerformers = ['activeTeam', 'mentor'] as const
export const modifierRewards = ['points', 'bonusKills'] as const
const modifierResolutionKinds = ['automaticRoundMetric', 'boolean', 'nonNegativeCount'] as const
const modifierFormulaCodes = [
  'growing_kill_value',
  'bonus_kill_on_condition',
  'bonus_kills_by_count',
  'window_kill_bonus_points',
] as const

type ModifierReward = (typeof modifierRewards)[number]
type ModifierResolutionKind = (typeof modifierResolutionKinds)[number]
type ModifierFormulaCode = (typeof modifierFormulaCodes)[number]

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

const formulaCompatibility: Record<
  ModifierFormulaCode,
  { reward: ModifierReward; resolutionKind: ModifierResolutionKind }
> = {
  growing_kill_value: { reward: 'points', resolutionKind: 'automaticRoundMetric' },
  bonus_kill_on_condition: { reward: 'bonusKills', resolutionKind: 'boolean' },
  bonus_kills_by_count: { reward: 'bonusKills', resolutionKind: 'nonNegativeCount' },
  window_kill_bonus_points: { reward: 'points', resolutionKind: 'nonNegativeCount' },
}

export function getCompatibleModifierFormulaCodes(
  reward: ModifierReward,
  resolutionKind: ModifierResolutionKind,
) {
  return modifierFormulaCodes.filter((code) => {
    const compatibility = formulaCompatibility[code]
    return compatibility.reward === reward && compatibility.resolutionKind === resolutionKind
  })
}

export function getCompatibleResolutionKinds(reward: ModifierReward) {
  return modifierResolutionKinds.filter((resolutionKind) =>
    modifierFormulaCodes.some(
      (code) =>
        formulaCompatibility[code].reward === reward &&
        formulaCompatibility[code].resolutionKind === resolutionKind,
    ),
  )
}

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
  formula: string
  tags: string
}

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
      durationSeconds: z.string().regex(/^([1-9]\d*)?$/, messages.limit),
      conflictingModifierIds: z.array(z.string()),
      activationCommand: z.string().max(128),
      reward: z.enum(modifierRewards),
      resolutionKind: z.enum(modifierResolutionKinds),
      formulaCode: z.enum(modifierFormulaCodes),
      incrementPointsPerKill: z.string().regex(/^\d+$/, messages.number),
      zeroKillPenaltyPoints: z.string().regex(/^\d+$/, messages.number),
      successBonusKills: z.string().regex(/^[1-9]\d*$/, messages.limit),
      bonusKillsPerUnit: z.string().regex(/^[1-9]\d*$/, messages.limit),
      bonusRate: z.string().regex(/^\d+([.,]\d+)?$/, messages.number),
    })
    .superRefine((values, context) => {
      const tags = normalizeModifierTags(values.tags)
      if (tags.length > 5 || tags.some((tag) => graphemeLength(tag) > 32)) {
        context.addIssue({ code: 'custom', path: ['tags'], message: messages.tags })
      }

      if (values.kind === 'scoring') {
        const compatible = getCompatibleModifierFormulaCodes(values.reward, values.resolutionKind)
        if (!compatible.includes(values.formulaCode)) {
          context.addIssue({ code: 'custom', path: ['formulaCode'], message: messages.formula })
        }
        if (values.formulaCode === 'window_kill_bonus_points') {
          const bonusRate = Number.parseFloat(values.bonusRate.replace(',', '.'))
          if (!Number.isFinite(bonusRate) || bonusRate <= 0) {
            context.addIssue({ code: 'custom', path: ['bonusRate'], message: messages.number })
          }
        }
      }
    })
}

export type ModifierFormValues = z.infer<ReturnType<typeof createModifierFormSchema>>

export function createDefaultModifierFormValues(
  initial?: GameModifierDefinition,
): ModifierFormValues {
  const behavior = initial?.behaviorV2
  const formula = behavior?.formulaReference
  const parameters = formula?.parameters
  const resolutionType = behavior?.resolution.type

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
    durationSeconds:
      behavior?.durationSecondsPerActivation == null
        ? ''
        : String(behavior.durationSecondsPerActivation),
    conflictingModifierIds: initial?.conflictingModifierIds ?? [],
    activationCommand: initial?.activationCommand ?? '',
    reward: behavior?.reward === 'bonusKills' ? 'bonusKills' : 'points',
    resolutionKind:
      resolutionType === 'boolean' ||
      resolutionType === 'nonNegativeCount' ||
      resolutionType === 'automaticRoundMetric'
        ? resolutionType
        : 'automaticRoundMetric',
    formulaCode: formula?.code ?? 'growing_kill_value',
    incrementPointsPerKill:
      parameters?.type === 'growingKillValue' ? String(parameters.incrementPointsPerKill) : '5',
    zeroKillPenaltyPoints:
      parameters?.type === 'growingKillValue' ? String(parameters.zeroKillPenaltyPoints) : '25',
    successBonusKills:
      parameters?.type === 'bonusKillOnCondition' ? String(parameters.successBonusKills) : '1',
    bonusKillsPerUnit:
      parameters?.type === 'bonusKillsByCount' ? String(parameters.bonusKillsPerUnit) : '1',
    bonusRate: parameters?.type === 'windowKillBonusPoints' ? String(parameters.bonusRate) : '0.75',
  }
}

function optionalPositiveInteger(value: string) {
  return value.trim() === '' ? null : Number.parseInt(value, 10)
}

function buildBehavior(values: ModifierFormValues) {
  if (values.kind === 'rule') {
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
      ...(optionalPositiveInteger(values.durationSeconds) === null
        ? {}
        : { durationSecondsPerActivation: optionalPositiveInteger(values.durationSeconds) }),
    }
  }

  const resolution =
    values.resolutionKind === 'automaticRoundMetric'
      ? { type: 'automaticRoundMetric' as const, metric: 'killsCount' as const }
      : { type: values.resolutionKind }
  const parameters =
    values.formulaCode === 'growing_kill_value'
      ? {
          type: 'growingKillValue' as const,
          incrementPointsPerKill: Number.parseInt(values.incrementPointsPerKill, 10),
          zeroKillPenaltyPoints: Number.parseInt(values.zeroKillPenaltyPoints, 10),
        }
      : values.formulaCode === 'bonus_kill_on_condition'
        ? {
            type: 'bonusKillOnCondition' as const,
            successBonusKills: Number.parseInt(values.successBonusKills, 10),
          }
        : values.formulaCode === 'bonus_kills_by_count'
          ? {
              type: 'bonusKillsByCount' as const,
              bonusKillsPerUnit: Number.parseInt(values.bonusKillsPerUnit, 10),
            }
          : {
              type: 'windowKillBonusPoints' as const,
              bonusRate: Number.parseFloat(values.bonusRate.replace(',', '.')),
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
    reward: values.reward,
    formulaReference: { code: values.formulaCode, version: 1 as const, parameters },
  }
}

export function toModifierRequest(values: ModifierFormValues): CreateGameModifierRequest {
  const behavior = buildBehavior(values)
  const limit = optionalPositiveInteger(values.activationLimitCount)

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    category: values.phase,
    activationCost: Number.parseInt(values.activationCost, 10),
    activationLimit: { count: limit },
    conflictingModifierIds: values.conflictingModifierIds,
    iconEmoji: values.iconEmoji.trim() || null,
    activationCommand: values.activationCommand.trim() || null,
    normalizedTags: normalizeModifierTags(values.tags),
    behaviorV2: behavior,
  }
}
