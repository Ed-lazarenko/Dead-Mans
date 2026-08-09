import { z } from 'zod'
import type { components } from '../../../shared/api/contracts/generated'
import {
  deriveModifierRoundSummaryMeta,
  modifierRoundSummaryTypes,
  type ModifierRoundSummaryCountInput,
} from '../../game-modifiers/model/modifier-round-summary.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type GameRoundModifierResult = GameRoundDetails['modifierResults'][number]

export const gameRoundModifierOutcomeStatuses = ['completed', 'failed', 'cancelled'] as const
export const gameRoundPostRoundActions = ['continue', 'finish'] as const
const modifierRoundSummaryCountInputs = [
  'bonusKills',
  'mentorKills',
  'killsDuringWindow',
] as const satisfies readonly ModifierRoundSummaryCountInput[]

const modifierSummarySchema = z.object({
  modifierResultIds: z.array(z.string().min(1)).min(1),
  modifierId: z.string().min(1),
  modifierName: z.string().min(1),
  modifierDescription: z.string().nullable(),
  activationCount: z.coerce.number().int().min(1),
  roundSummaryType: z.enum(modifierRoundSummaryTypes),
  outcomeStatus: z.enum(gameRoundModifierOutcomeStatuses),
  countInput: z.enum(modifierRoundSummaryCountInputs).nullable(),
  autoResultFormula: z
    .enum(['flat_per_kill', 'stacking_per_kill_bonus', 'custom_expression'])
    .nullable(),
  autoResultSuccessExpression: z.string().nullable(),
  autoResultFailureExpression: z.string().nullable(),
  countValue: z.coerce.number().int().min(0),
  conditionType: z.string().nullable(),
  isConditionMet: z.boolean(),
  manualScoreDelta: z.coerce.number().int(),
  manualKillDelta: z.coerce.number().int(),
  perKillBonus: z.coerce.number().int().nullable(),
  failurePenaltyPoints: z.coerce.number().int().nullable(),
  killDeltaValue: z.coerce.number().int().nullable(),
  multiplierDelta: z.coerce.number().nullable(),
})

export const gameRoundSummaryFormSchema = z.object({
  killsCount: z.coerce.number().int().min(0),
  bountyCount: z.coerce.number().int().min(0),
  modifiers: z.array(modifierSummarySchema),
  postRoundAction: z.enum(gameRoundPostRoundActions),
})

export type GameRoundSummaryFormValues = z.infer<typeof gameRoundSummaryFormSchema>
export type GameRoundPostRoundAction = (typeof gameRoundPostRoundActions)[number]

export interface CompleteRoundInput {
  roundId: string
  killsCount: number
  bountyCount: number
  modifierResults: Array<{
    modifierResultId: string
    outcomeStatus: string
    countValue: number | null
    isConditionMet: boolean | null
    manualScoreDelta: number | null
    manualKillDelta: number | null
    resolutionDataJson: string | null
  }>
}

export function buildGameRoundSummaryDefaultValues(
  activeRound: GameRoundDetails,
): GameRoundSummaryFormValues {
  return {
    killsCount: activeRound.killsCount,
    bountyCount: activeRound.bountyCount,
    postRoundAction: 'continue',
    modifiers: groupModifierSummaryEntries(
      activeRound.modifierResults
        .map((modifier) => {
          const meta = deriveSnapshotModifierRoundSummaryMeta(modifier)

          if (
            !meta.includeInRoundSummary &&
            modifier.scoreDelta === 0 &&
            modifier.killDelta === 0 &&
            modifier.multiplierApplied == null
          ) {
            return null
          }

          return {
            modifierResultIds: [modifier.modifierResultId],
            modifierId: modifier.modifierId,
            modifierName: modifier.modifierName,
            modifierDescription: normalizeOptionalText(modifier.modifierDescription),
            activationCount: 1,
            roundSummaryType: meta.type,
            outcomeStatus:
              normalizeOutcomeStatus(modifier.outcomeStatus) ??
              getDefaultOutcomeStatus(meta.type, modifier.scoreDelta, modifier.killDelta),
            countInput: meta.countInput,
            autoResultFormula: meta.autoResultFormula,
            autoResultSuccessExpression: meta.autoResultSuccessExpression,
            autoResultFailureExpression: meta.autoResultFailureExpression,
            countValue: deriveInitialCountValue(modifier),
            conditionType: meta.conditionType,
            isConditionMet: deriveInitialConditionMet(modifier),
            manualScoreDelta:
              meta.type === 'manual_points' ? modifier.scoreDelta : (meta.flatPointsDelta ?? 0),
            manualKillDelta: meta.type === 'manual_points' ? modifier.killDelta : 0,
            perKillBonus: meta.perKillBonus,
            failurePenaltyPoints: meta.failurePenaltyPoints,
            killDeltaValue: meta.killDeltaValue,
            multiplierDelta: meta.multiplierDelta,
          }
        })
        .filter(
          (modifier): modifier is GameRoundSummaryFormValues['modifiers'][number] =>
            modifier !== null,
        ),
    ),
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
    modifierResults: values.modifiers.flatMap(buildModifierResolutionFacts),
  }
}

function buildModifierResolutionFacts(
  modifier: GameRoundSummaryFormValues['modifiers'][number],
) {
  switch (modifier.roundSummaryType) {
    case 'auto_result': {
      return cloneResolutionFactsPerActivation(modifier, {
        outcomeStatus: 'cancelled',
        countValue: null,
        isConditionMet: null,
        manualScoreDelta: null,
        manualKillDelta: null,
        resolutionDataJson: null,
      })
    }

    case 'toggle_bonus': {
      return cloneResolutionFactsPerActivation(modifier, {
        outcomeStatus: modifier.isConditionMet ? 'completed' : 'failed',
        countValue: null,
        isConditionMet: modifier.isConditionMet,
        manualScoreDelta: null,
        manualKillDelta: null,
        resolutionDataJson: null,
      })
    }

    case 'counted_bonus': {
      const countValue = modifier.countValue

      return cloneResolutionFactsPerActivation(modifier, {
        outcomeStatus: countValue > 0 ? 'completed' : 'cancelled',
        countValue,
        isConditionMet: null,
        manualScoreDelta: null,
        manualKillDelta: null,
        resolutionDataJson: null,
      })
    }

    case 'kill_multiplier': {
      const countValue = modifier.countValue

      return cloneResolutionFactsPerActivation(modifier, {
        outcomeStatus: countValue > 0 ? 'completed' : 'cancelled',
        countValue,
        isConditionMet: null,
        manualScoreDelta: null,
        manualKillDelta: null,
        resolutionDataJson: null,
      })
    }

    case 'manual_points': {
      const outcomeStatus =
        modifier.manualScoreDelta === 0 && modifier.manualKillDelta === 0
          ? 'cancelled'
          : (normalizeOutcomeStatus(modifier.outcomeStatus) ?? 'completed')

      return modifier.modifierResultIds.map((modifierResultId) => ({
        modifierResultId,
        outcomeStatus,
        countValue: null,
        isConditionMet: null,
        manualScoreDelta: modifier.manualScoreDelta,
        manualKillDelta: modifier.manualKillDelta,
        resolutionDataJson: null,
      }))
    }

    case 'passive':
    default:
      return cloneResolutionFactsPerActivation(modifier, {
        outcomeStatus: 'cancelled',
        countValue: null,
        isConditionMet: null,
        manualScoreDelta: null,
        manualKillDelta: null,
        resolutionDataJson: null,
      })
  }
}

function cloneResolutionFactsPerActivation(
  modifier: GameRoundSummaryFormValues['modifiers'][number],
  template: Omit<CompleteRoundInput['modifierResults'][number], 'modifierResultId'>,
) {
  return modifier.modifierResultIds.map((modifierResultId) => ({
    modifierResultId,
    ...template,
  }))
}

function deriveSnapshotModifierRoundSummaryMeta(modifier: GameRoundModifierResult) {
  if (modifier.modifierEffect) {
    return deriveModifierRoundSummaryMeta({
      id: modifier.modifierId,
      name: modifier.modifierName,
      description: modifier.modifierDescription,
      scoringType: modifier.modifierScoringType,
      category: modifier.modifierCategory,
      requiresHostControl: true,
      mechanicType: modifier.modifierMechanicType,
      activationCost: 0,
      defaultLimitPerGame: null,
      activationLimit: { count: null },
      effect: modifier.modifierEffect,
      conflictingModifierIds: [],
      iconEmoji: null,
      activationCommand: null,
    })
  }

  return deriveFallbackModifierRoundSummaryMeta(modifier)
}

function deriveFallbackModifierRoundSummaryMeta(modifier: GameRoundModifierResult) {
  if (modifier.multiplierApplied != null) {
    return {
      type: 'kill_multiplier' as const,
      includeInRoundSummary: true,
      countInput: 'killsDuringWindow' as const,
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      flatPointsDelta: null,
      killDeltaValue: null,
      multiplierDelta: modifier.multiplierApplied,
    }
  }

  if (modifier.killDelta !== 0) {
    return {
      type: 'manual_points' as const,
      includeInRoundSummary: true,
      countInput: null,
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      flatPointsDelta: modifier.scoreDelta,
      killDeltaValue: modifier.killDelta,
      multiplierDelta: null,
    }
  }

  if (modifier.scoreDelta !== 0) {
    return {
      type: 'manual_points' as const,
      includeInRoundSummary: true,
      countInput: null,
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      flatPointsDelta: modifier.scoreDelta,
      killDeltaValue: null,
      multiplierDelta: null,
    }
  }

  return {
    type: 'passive' as const,
    includeInRoundSummary: false,
    countInput: null,
    autoResultFormula: null,
    autoResultSuccessExpression: null,
    autoResultFailureExpression: null,
    conditionType: null,
    perKillBonus: null,
    failurePenaltyPoints: null,
    flatPointsDelta: null,
    killDeltaValue: null,
    multiplierDelta: null,
  }
}

function deriveInitialCountValue(modifier: GameRoundModifierResult) {
  const parsed = parseResolutionData(modifier.resolutionDataJson)
  const parsedCount =
    typeof parsed?.countValue === 'number'
      ? parsed.countValue
      : typeof parsed?.mentorKills === 'number'
        ? parsed.mentorKills
        : typeof parsed?.killsDuringWindow === 'number'
          ? parsed.killsDuringWindow
          : null

  if (parsedCount !== null) {
    return parsedCount
  }

  return 0
}

function deriveInitialConditionMet(modifier: GameRoundModifierResult) {
  const parsed = parseResolutionData(modifier.resolutionDataJson)
  if (typeof parsed?.conditionMet === 'boolean') {
    return parsed.conditionMet
  }

  return modifier.killDelta > 0 || normalizeOutcomeStatus(modifier.outcomeStatus) === 'completed'
}

function normalizeOptionalText(value: string | null | undefined) {
  const normalized = value?.trim()
  return normalized ? normalized : null
}

function parseResolutionData(value: string | null) {
  if (!value) {
    return null
  }

  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : null
  } catch {
    return null
  }
}

function normalizeOutcomeStatus(value: string | null | undefined) {
  if (!value) {
    return null
  }

  const normalized = value.trim().toLowerCase()
  return gameRoundModifierOutcomeStatuses.includes(
    normalized as (typeof gameRoundModifierOutcomeStatuses)[number],
  )
    ? (normalized as (typeof gameRoundModifierOutcomeStatuses)[number])
    : null
}

function getDefaultOutcomeStatus(
  roundSummaryType: (typeof modifierRoundSummaryTypes)[number],
  scoreDelta: number,
  killDelta: number,
) {
  if (roundSummaryType === 'manual_points') {
    return scoreDelta === 0 && killDelta === 0 ? 'cancelled' : 'completed'
  }

  return 'cancelled'
}

function groupModifierSummaryEntries(entries: GameRoundSummaryFormValues['modifiers']) {
  const grouped = new Map<string, GameRoundSummaryFormValues['modifiers'][number]>()

  for (const entry of entries) {
    const current = grouped.get(entry.modifierId)
    if (!current) {
      grouped.set(entry.modifierId, entry)
      continue
    }

    grouped.set(entry.modifierId, {
      ...current,
      modifierResultIds: [...current.modifierResultIds, ...entry.modifierResultIds],
      modifierDescription: current.modifierDescription ?? entry.modifierDescription,
      activationCount: current.activationCount + entry.activationCount,
      outcomeStatus: pickMergedOutcomeStatus(current.outcomeStatus, entry.outcomeStatus),
      countValue: current.countValue !== 0 ? current.countValue : entry.countValue,
      isConditionMet: current.isConditionMet || entry.isConditionMet,
      manualScoreDelta: current.manualScoreDelta !== 0 ? current.manualScoreDelta : entry.manualScoreDelta,
      manualKillDelta: current.manualKillDelta !== 0 ? current.manualKillDelta : entry.manualKillDelta,
    })
  }

  return Array.from(grouped.values())
}

function pickMergedOutcomeStatus(
  left: GameRoundSummaryFormValues['modifiers'][number]['outcomeStatus'],
  right: GameRoundSummaryFormValues['modifiers'][number]['outcomeStatus'],
) {
  const rank = {
    completed: 3,
    failed: 2,
    cancelled: 1,
  } as const

  return rank[right] > rank[left] ? right : left
}
