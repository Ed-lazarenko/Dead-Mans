import { z } from 'zod'
import type { components } from '../../../shared/api/contracts/generated'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type GameRoundModifierResult = GameRoundDetails['modifierResults'][number]
type FinalizeGameRoundRequest = components['schemas']['FinalizeGameRoundRequestDto']

const aggregateSafeCountFormulaCodes = new Set([
  'fixed_points_per_unit',
  'bonus_kills_per_unit',
  'bonus_kills_by_count',
])

export const gameRoundRuleOutcomeStatuses = ['completed', 'violated', 'notTriggered'] as const
export const gameRoundPostRoundActions = ['continue', 'finish'] as const
const ruleGroupSummarySchema = z
  .object({
    resolutionGroupId: z.string().min(1),
    modifierId: z.string().min(1),
    modifierName: z.string().min(1),
    modifierDescription: z.string().nullable(),
    memberResultIds: z.array(z.string().min(1)).min(1),
    memberActivationIds: z.array(z.string().min(1)).min(1),
    outcomeStatus: z.enum(gameRoundRuleOutcomeStatuses).nullable(),
    violationComment: z.string(),
  })
  .superRefine((value, context) => {
    if (value.outcomeStatus === null) {
      context.addIssue({ code: 'custom', path: ['outcomeStatus'], message: '' })
    }
    if (value.outcomeStatus === 'violated' && value.violationComment.trim().length === 0) {
      context.addIssue({ code: 'custom', path: ['violationComment'], message: '' })
    }
  })

const scoringInstanceSummarySchema = z
  .object({
    modifierResultId: z.string().min(1),
    activationId: z.string().min(1),
    memberResultIds: z.array(z.string().min(1)).min(1),
    memberActivationIds: z.array(z.string().min(1)).min(1),
    modifierId: z.string().min(1),
    modifierName: z.string().min(1),
    modifierDescription: z.string().nullable(),
    activationIndex: z.coerce.number().int().min(1),
    activationCount: z.coerce.number().int().min(1),
    resolutionKind: z.enum(['boolean', 'nonNegativeCount']),
    isConditionMet: z.boolean().nullable(),
    countValue: z.coerce.number().int().min(0).nullable(),
    inputLabel: z.string().nullable(),
    maximumKind: z.enum(['none', 'resolvedKills', 'activations']).nullable(),
    maximumPerActivation: z.coerce.number().int().min(1).nullable(),
  })
  .superRefine((value, context) => {
    if (value.resolutionKind === 'boolean' && value.isConditionMet === null) {
      context.addIssue({ code: 'custom', path: ['isConditionMet'], message: '' })
    }
    if (value.resolutionKind === 'nonNegativeCount' && value.countValue === null) {
      context.addIssue({ code: 'custom', path: ['countValue'], message: '' })
    }
    if (
      value.resolutionKind === 'nonNegativeCount' &&
      value.countValue !== null &&
      value.maximumKind === 'activations' &&
      value.maximumPerActivation !== null &&
      value.countValue > value.memberResultIds.length * value.maximumPerActivation
    ) {
      context.addIssue({ code: 'custom', path: ['countValue'], message: '' })
    }
  })

const automaticInstanceSummarySchema = z.object({
  modifierResultId: z.string().min(1),
  activationId: z.string().min(1),
  modifierId: z.string().min(1),
  modifierName: z.string().min(1),
  modifierDescription: z.string().nullable(),
  activationIndex: z.coerce.number().int().min(1),
  activationCount: z.coerce.number().int().min(1),
})

export const gameRoundSummaryFormSchema = z.object({
  killsCount: z.coerce.number().int().min(0),
  bountyCount: z.coerce.number().int().min(0),
  notes: z.string().max(2000),
  ruleGroups: z.array(ruleGroupSummarySchema),
  scoringInstances: z.array(scoringInstanceSummarySchema),
  automaticInstances: z.array(automaticInstanceSummarySchema),
  postRoundAction: z.enum(gameRoundPostRoundActions),
})

export type GameRoundSummaryFormValues = z.infer<typeof gameRoundSummaryFormSchema>
export type GameRoundPostRoundAction = (typeof gameRoundPostRoundActions)[number]

export interface CompleteRoundInput {
  roundId: string
  killsCount: number
  bountyCount: number
  notes: string | null
  expectedRoundVersion: number
  modifierResults: NonNullable<FinalizeGameRoundRequest['modifierResults']>
  ruleGroups: NonNullable<FinalizeGameRoundRequest['ruleGroups']>
}

export function buildGameRoundSummaryDefaultValues(
  activeRound: GameRoundDetails,
): GameRoundSummaryFormValues {
  const v2Results = activeRound.modifierResults.filter((result) => result.resolutionKind != null)
  return {
    killsCount: activeRound.killsCount,
    bountyCount: activeRound.bountyCount,
    notes: activeRound.notes ?? '',
    postRoundAction: 'continue',
    ruleGroups: buildRuleGroupDefaults(v2Results),
    scoringInstances: buildScoringInstanceDefaults(v2Results),
    automaticInstances: buildAutomaticInstanceDefaults(v2Results),
  }
}

export function buildCompleteRoundInput(
  activeRound: GameRoundDetails,
  values: GameRoundSummaryFormValues,
): CompleteRoundInput {
  return {
    roundId: activeRound.roundId,
    killsCount: values.killsCount,
    bountyCount: values.bountyCount,
    notes: normalizeOptionalText(values.notes),
    expectedRoundVersion: activeRound.roundVersion,
    modifierResults: values.scoringInstances.flatMap((instance) => {
      if (instance.resolutionKind === 'boolean')
        return [
          {
            modifierResultId: instance.modifierResultId,
            countValue: null,
            isConditionMet: instance.isConditionMet,
          },
        ]
      const count = instance.countValue ?? 0
      const distributed = distributeCount(
        count,
        instance.memberResultIds.length,
        instance.maximumKind === 'activations' ? instance.maximumPerActivation : null,
      )
      return instance.memberResultIds.map((modifierResultId, index) => ({
        modifierResultId,
        countValue: distributed[index] ?? 0,
        isConditionMet: null,
      }))
    }),
    ruleGroups: values.ruleGroups.map((group) => ({
      resolutionGroupId: group.resolutionGroupId,
      memberResultIds: group.memberResultIds,
      outcomeStatus: group.outcomeStatus ?? 'notTriggered',
      violationComment: group.outcomeStatus === 'violated' ? group.violationComment.trim() : null,
    })),
  }
}

export function buildGameRoundPreviewRequest(input: CompleteRoundInput): FinalizeGameRoundRequest {
  return {
    status: 'completed',
    killsCount: input.killsCount,
    bountyCount: input.bountyCount,
    notes: input.notes,
    modifierResults: input.modifierResults,
    ruleGroups: input.ruleGroups,
    expectedRoundVersion: input.expectedRoundVersion,
  }
}

export function serializeGameRoundPreviewInput(input: CompleteRoundInput) {
  return JSON.stringify(buildGameRoundPreviewRequest(input))
}

function buildRuleGroupDefaults(results: GameRoundModifierResult[]) {
  const groups = new Map<string, GameRoundSummaryFormValues['ruleGroups'][number]>()
  for (const result of results) {
    if (result.resolutionKind !== 'ruleStatus' || !result.resolutionGroupId) continue
    const current = groups.get(result.resolutionGroupId)
    if (current) {
      current.memberResultIds.push(result.modifierResultId)
      current.memberActivationIds.push(result.activationId)
      continue
    }
    groups.set(result.resolutionGroupId, {
      resolutionGroupId: result.resolutionGroupId,
      modifierId: result.modifierId,
      modifierName: result.modifierName,
      modifierDescription: normalizeOptionalText(result.modifierDescription),
      memberResultIds: [result.modifierResultId],
      memberActivationIds: [result.activationId],
      outcomeStatus: normalizeRuleOutcomeStatus(result.outcomeStatus),
      violationComment: result.violationComment ?? '',
    })
  }
  return Array.from(groups.values())
}

function buildScoringInstanceDefaults(results: GameRoundModifierResult[]) {
  const manual = results.filter(isManualV2ScoringResult)
  const counts = countByModifier(manual)
  const positions = new Map<string, number>()
  const instances: GameRoundSummaryFormValues['scoringInstances'] = []
  const aggregateIndexes = new Map<string, number>()
  for (const result of manual) {
    const aggregateKey = getCountAggregationKey(result)
    const shouldAggregate = aggregateKey !== null
    if (shouldAggregate) {
      const existingIndex = aggregateIndexes.get(aggregateKey)
      if (existingIndex !== undefined) {
        const existing = instances[existingIndex]
        existing.memberResultIds.push(result.modifierResultId)
        existing.memberActivationIds.push(result.activationId)
        existing.activationCount = existing.memberResultIds.length
        if (result.outcomeStatus !== 'pending') {
          existing.countValue = (existing.countValue ?? 0) + deriveInitialCountValue(result)
        }
        continue
      }
    }
    const activationIndex = (positions.get(result.modifierId) ?? 0) + 1
    positions.set(result.modifierId, activationIndex)
    const parsed = parseResolutionData(result.resolutionDataJson)
    const isResolved = result.outcomeStatus !== 'pending'
    instances.push({
      modifierResultId: result.modifierResultId,
      activationId: result.activationId,
      memberResultIds: [result.modifierResultId],
      memberActivationIds: [result.activationId],
      modifierId: result.modifierId,
      modifierName: result.modifierName,
      modifierDescription: normalizeOptionalText(result.modifierDescription),
      activationIndex,
      activationCount: shouldAggregate ? 1 : (counts.get(result.modifierId) ?? 1),
      resolutionKind: result.resolutionKind,
      isConditionMet:
        result.resolutionKind === 'boolean' && isResolved
          ? typeof parsed?.conditionMet === 'boolean'
            ? parsed.conditionMet
            : result.outcomeStatus === 'succeeded'
          : null,
      countValue:
        result.resolutionKind === 'nonNegativeCount' && isResolved
          ? deriveInitialCountValue(result)
          : result.resolutionKind === 'nonNegativeCount'
            ? 0
            : null,
      inputLabel: result.runtimeBehavior?.resolutionInputLabel ?? null,
      maximumKind: result.runtimeBehavior?.resolutionMaximumKind ?? null,
      maximumPerActivation: result.runtimeBehavior?.resolutionMaximumPerActivation ?? null,
    })
    if (aggregateKey !== null) aggregateIndexes.set(aggregateKey, instances.length - 1)
  }
  return instances
}

function getCountAggregationKey(result: GameRoundModifierResult) {
  const runtime = result.runtimeBehavior
  if (
    result.resolutionKind !== 'nonNegativeCount' ||
    !runtime?.resolutionInputLabel ||
    runtime.resolutionMaximumKind !== 'activations' ||
    runtime.resolutionMaximumPerActivation == null ||
    !runtime.formulaCode ||
    !aggregateSafeCountFormulaCodes.has(runtime.formulaCode)
  ) {
    return null
  }

  return JSON.stringify([
    result.modifierId,
    result.definitionRevision,
    runtime.formulaCode,
    runtime.resolutionInputLabel,
    runtime.resolutionMaximumPerActivation,
  ])
}

function distributeCount(total: number, members: number, maximumPerMember: number | null) {
  if (members <= 0) return []
  if (maximumPerMember === null) return [total, ...Array.from({ length: members - 1 }, () => 0)]
  let remaining = total
  return Array.from({ length: members }, () => {
    const value = Math.min(remaining, maximumPerMember)
    remaining -= value
    return value
  })
}

function buildAutomaticInstanceDefaults(results: GameRoundModifierResult[]) {
  const automatic = results.filter(
    (result) =>
      result.resolutionKind === 'automaticRoundMetric' || result.resolutionKind === 'perActivation',
  )
  const counts = countByModifier(automatic)
  const positions = new Map<string, number>()
  return automatic.map((result) => {
    const activationIndex = (positions.get(result.modifierId) ?? 0) + 1
    positions.set(result.modifierId, activationIndex)
    return {
      modifierResultId: result.modifierResultId,
      activationId: result.activationId,
      modifierId: result.modifierId,
      modifierName: result.modifierName,
      modifierDescription: normalizeOptionalText(result.modifierDescription),
      activationIndex,
      activationCount: counts.get(result.modifierId) ?? 1,
    }
  })
}

function isManualV2ScoringResult(
  result: GameRoundModifierResult,
): result is GameRoundModifierResult & { resolutionKind: 'boolean' | 'nonNegativeCount' } {
  return result.resolutionKind === 'boolean' || result.resolutionKind === 'nonNegativeCount'
}

function countByModifier(results: GameRoundModifierResult[]) {
  const counts = new Map<string, number>()
  for (const result of results)
    counts.set(result.modifierId, (counts.get(result.modifierId) ?? 0) + 1)
  return counts
}

function deriveInitialCountValue(modifier: GameRoundModifierResult) {
  const parsed = parseResolutionData(modifier.resolutionDataJson)
  return typeof parsed?.countValue === 'number'
    ? parsed.countValue
    : typeof parsed?.count === 'number'
      ? parsed.count
      : typeof parsed?.mentorKills === 'number'
        ? parsed.mentorKills
        : typeof parsed?.killsDuringWindow === 'number'
          ? parsed.killsDuringWindow
          : 0
}

function normalizeOptionalText(value: string | null | undefined) {
  const normalized = value?.trim()
  return normalized ? normalized : null
}

function parseResolutionData(value: string | null | undefined) {
  if (!value) return null
  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : null
  } catch {
    return null
  }
}

function normalizeRuleOutcomeStatus(value: string | null | undefined) {
  if (value === 'completed' || value === 'violated') return value
  if (value === 'not_triggered') return 'notTriggered'
  return null
}
