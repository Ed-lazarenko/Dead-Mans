import { z } from 'zod'
import type { components } from '../../../shared/api/contracts/generated'
import {
  deriveModifierRoundSummaryMeta,
  modifierRoundSummaryTypes,
  type ModifierAutoResultFormula,
  type ModifierRoundSummaryCountInput,
} from '../../game-modifiers/model/modifier-round-summary.ts'
import {
  evaluateModifierScoreFormulaFailure,
  evaluateModifierScoreFormulaSuccess,
} from '../../game-modifiers/model/modifier-score-formula.ts'

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
  finalScore: number
  killsCount: number
  bountyCount: number
  modifierResults: Array<{
    modifierResultId: string
    outcomeStatus: string
    scoreDelta: number
    killDelta: number
    multiplierApplied: number | null
    resolutionDataJson: string | null
  }>
}

export interface ComputedGameRoundModifierResolution {
  modifierResultIds: string[]
  modifierName: string
  activationCount: number
  outcomeStatus: string
  scoreDelta: number
  killDelta: number
  multiplierApplied: number | null
  resolutionDataJson: string | null
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
            countValue: deriveInitialCountValue(activeRound.baseScore, modifier, meta),
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
  const expandedModifiers = values.modifiers.flatMap((modifier) =>
    buildExpandedModifierResolutions(
      activeRound.baseScore,
      values.killsCount,
      values.bountyCount,
      modifier,
    ),
  )
  const preview = buildGameRoundScorePreview(activeRound.baseScore, values)

  return {
    roundId: activeRound.roundId,
    finalScore: preview.finalScore,
    killsCount: values.killsCount,
    bountyCount: values.bountyCount,
    modifierResults: expandedModifiers.map((modifier) => ({
      modifierResultId: modifier.modifierResultId,
      outcomeStatus: modifier.outcomeStatus,
      scoreDelta: modifier.scoreDelta,
      killDelta: modifier.killDelta,
      multiplierApplied: modifier.multiplierApplied,
      resolutionDataJson: modifier.resolutionDataJson,
    })),
  }
}

export function buildGameRoundScorePreview(
  scoreUnit: number,
  values: Pick<GameRoundSummaryFormValues, 'killsCount' | 'bountyCount' | 'modifiers'>,
) {
  const computedModifiers = values.modifiers.map((modifier) =>
    buildComputedModifierResolution(scoreUnit, values.killsCount, values.bountyCount, modifier),
  )
  const modifierKillDelta = computedModifiers.reduce(
    (total, modifier) => total + modifier.killDelta,
    0,
  )
  const modifierScoreDelta = computedModifiers.reduce(
    (total, modifier) => total + modifier.scoreDelta,
    0,
  )
  const killsScore = values.killsCount * scoreUnit
  const bountyScore = values.bountyCount * scoreUnit
  const modifierKillScore = modifierKillDelta * scoreUnit
  const finalScore = killsScore + bountyScore + modifierKillScore + modifierScoreDelta

  return {
    scoreUnit,
    killsScore,
    bountyScore,
    modifierKillDelta,
    modifierKillScore,
    modifierScoreDelta,
    totalKillCount: values.killsCount + modifierKillDelta,
    finalScore,
    computedModifiers,
  }
}

export function buildComputedModifierResolution(
  scoreUnit: number,
  killsCount: number,
  bountyCount: number,
  modifier: GameRoundSummaryFormValues['modifiers'][number],
): ComputedGameRoundModifierResolution {
  const expanded = buildExpandedModifierResolutions(scoreUnit, killsCount, bountyCount, modifier)

  return {
    modifierResultIds: modifier.modifierResultIds,
    modifierName: modifier.modifierName,
    activationCount: modifier.activationCount,
    outcomeStatus: expanded[0]?.outcomeStatus ?? 'cancelled',
    scoreDelta: expanded.reduce((total, item) => total + item.scoreDelta, 0),
    killDelta: expanded.reduce((total, item) => total + item.killDelta, 0),
    multiplierApplied: expanded[0]?.multiplierApplied ?? null,
    resolutionDataJson: expanded[0]?.resolutionDataJson ?? null,
  }
}

interface ExpandedModifierResolution {
  modifierResultId: string
  outcomeStatus: string
  scoreDelta: number
  killDelta: number
  multiplierApplied: number | null
  resolutionDataJson: string | null
}

function buildExpandedModifierResolutions(
  scoreUnit: number,
  killsCount: number,
  bountyCount: number,
  modifier: GameRoundSummaryFormValues['modifiers'][number],
): ExpandedModifierResolution[] {
  switch (modifier.roundSummaryType) {
    case 'auto_result': {
      const hasCustomExpression =
        modifier.autoResultFormula === 'custom_expression' && !!modifier.autoResultSuccessExpression

      if (killsCount > 0 && ((modifier.perKillBonus ?? 0) > 0 || hasCustomExpression)) {
        const cloneAutoResult =
          modifier.autoResultFormula === 'custom_expression'
            ? cloneResolutionPerActivationWithDistributedScore
            : cloneResolutionPerActivation
        return cloneAutoResult(modifier, {
          outcomeStatus: 'completed',
          scoreDelta: computeAutoResultScoreDelta(
            scoreUnit,
            killsCount,
            bountyCount,
            modifier.perKillBonus ?? 0,
            modifier.failurePenaltyPoints ?? 0,
            modifier.activationCount,
            modifier.autoResultFormula,
            modifier.autoResultSuccessExpression,
            modifier.autoResultFailureExpression,
          ),
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: JSON.stringify({
            source: 'round_kills',
            killsCount,
            bountyCount,
            perKillBonus: modifier.perKillBonus,
            autoResultFormula: modifier.autoResultFormula,
            autoResultSuccessExpression: modifier.autoResultSuccessExpression,
            autoResultFailureExpression: modifier.autoResultFailureExpression,
          }),
        })
      }

      if (
        killsCount === 0 &&
        ((modifier.failurePenaltyPoints ?? 0) > 0 || !!modifier.autoResultFailureExpression)
      ) {
        const cloneAutoResult =
          modifier.autoResultFormula === 'custom_expression'
            ? cloneResolutionPerActivationWithDistributedScore
            : cloneResolutionPerActivation
        return cloneAutoResult(modifier, {
          outcomeStatus: 'failed',
          scoreDelta: computeAutoResultFailureScoreDelta(
            scoreUnit,
            killsCount,
            bountyCount,
            modifier.perKillBonus ?? 0,
            modifier.failurePenaltyPoints ?? 0,
            modifier.activationCount,
            modifier.autoResultFormula,
            modifier.autoResultSuccessExpression,
            modifier.autoResultFailureExpression,
          ),
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: JSON.stringify({
            source: 'round_kills',
            killsCount,
            bountyCount,
            failurePenaltyPoints: modifier.failurePenaltyPoints,
            autoResultFormula: modifier.autoResultFormula,
            autoResultSuccessExpression: modifier.autoResultSuccessExpression,
            autoResultFailureExpression: modifier.autoResultFailureExpression,
          }),
        })
      }

      return cloneResolutionPerActivation(modifier, {
        outcomeStatus: 'cancelled',
        scoreDelta: 0,
        killDelta: 0,
        multiplierApplied: null,
        resolutionDataJson: JSON.stringify({
          source: 'round_kills',
          killsCount,
          bountyCount,
          effect: 'none',
          autoResultFormula: modifier.autoResultFormula,
          autoResultSuccessExpression: modifier.autoResultSuccessExpression,
          autoResultFailureExpression: modifier.autoResultFailureExpression,
        }),
      })
    }

    case 'toggle_bonus': {
      const killDelta = modifier.isConditionMet ? (modifier.killDeltaValue ?? 0) : 0

      return cloneResolutionPerActivation(modifier, {
        outcomeStatus: modifier.isConditionMet ? 'completed' : 'failed',
        scoreDelta: modifier.isConditionMet ? modifier.manualScoreDelta : 0,
        killDelta,
        multiplierApplied: null,
        resolutionDataJson: JSON.stringify({
          source: 'manual_condition',
          conditionType: modifier.conditionType,
          conditionMet: modifier.isConditionMet,
          killDeltaValue: modifier.killDeltaValue,
        }),
      })
    }

    case 'counted_bonus': {
      const countValue = Math.max(0, modifier.countValue)
      const killDelta = countValue * (modifier.killDeltaValue ?? 1)

      return cloneResolutionPerActivation(modifier, {
        outcomeStatus: countValue > 0 ? 'completed' : 'cancelled',
        scoreDelta: modifier.manualScoreDelta,
        killDelta,
        multiplierApplied: null,
        resolutionDataJson: JSON.stringify({
          source: 'manual_count',
          input: modifier.countInput,
          countValue,
          killDeltaValue: modifier.killDeltaValue ?? 1,
        }),
      })
    }

    case 'kill_multiplier': {
      const countValue = Math.max(0, modifier.countValue)
      const multiplierApplied = modifier.multiplierDelta ?? 0
      const scoreDelta = roundModifierScore(countValue * scoreUnit * multiplierApplied)

      return cloneResolutionPerActivation(modifier, {
        outcomeStatus: countValue > 0 ? 'completed' : 'cancelled',
        scoreDelta,
        killDelta: 0,
        multiplierApplied: countValue > 0 ? multiplierApplied : null,
        resolutionDataJson: JSON.stringify({
          source: 'manual_count',
          input: modifier.countInput,
          countValue,
          multiplierDelta: multiplierApplied,
        }),
      })
    }

    case 'manual_points': {
      const outcomeStatus =
        modifier.manualScoreDelta === 0 && modifier.manualKillDelta === 0
          ? 'cancelled'
          : (normalizeOutcomeStatus(modifier.outcomeStatus) ?? 'completed')
      const scoreDeltas = distributeInteger(modifier.manualScoreDelta, modifier.activationCount)
      const killDeltas = distributeInteger(modifier.manualKillDelta, modifier.activationCount)

      return modifier.modifierResultIds.map((modifierResultId, index) => ({
        modifierResultId,
        outcomeStatus,
        scoreDelta: scoreDeltas[index] ?? 0,
        killDelta: killDeltas[index] ?? 0,
        multiplierApplied: null,
        resolutionDataJson: null,
      }))
    }

    case 'passive':
    default:
      return cloneResolutionPerActivation(modifier, {
        outcomeStatus: 'cancelled',
        scoreDelta: 0,
        killDelta: 0,
        multiplierApplied: null,
        resolutionDataJson: null,
      })
  }
}

function cloneResolutionPerActivation(
  modifier: GameRoundSummaryFormValues['modifiers'][number],
  template: Omit<ExpandedModifierResolution, 'modifierResultId'>,
) {
  return modifier.modifierResultIds.map((modifierResultId) => ({
    modifierResultId,
    ...template,
  }))
}

function cloneResolutionPerActivationWithDistributedScore(
  modifier: GameRoundSummaryFormValues['modifiers'][number],
  template: Omit<ExpandedModifierResolution, 'modifierResultId'>,
) {
  const scoreDeltas = distributeInteger(template.scoreDelta, modifier.activationCount)
  return modifier.modifierResultIds.map((modifierResultId, index) => ({
    modifierResultId,
    ...template,
    scoreDelta: scoreDeltas[index] ?? 0,
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

function deriveInitialCountValue(
  scoreUnit: number,
  modifier: GameRoundModifierResult,
  meta:
    | ReturnType<typeof deriveModifierRoundSummaryMeta>
    | ReturnType<typeof deriveFallbackModifierRoundSummaryMeta>,
) {
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
    return Math.max(0, parsedCount)
  }

  if (meta.type === 'counted_bonus') {
    const perUnit = meta.killDeltaValue ?? 1
    return perUnit > 0 ? Math.max(0, Math.round(modifier.killDelta / perUnit)) : 0
  }

  if (meta.type === 'kill_multiplier' && meta.multiplierDelta) {
    const denominator = scoreUnit * meta.multiplierDelta
    return denominator > 0 ? Math.max(0, Math.round(modifier.scoreDelta / denominator)) : 0
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

function roundModifierScore(value: number) {
  return Math.round(value)
}

function computeAutoResultScoreDelta(
  scoreUnit: number,
  killsCount: number,
  bountyCount: number,
  perKillBonus: number,
  failurePenaltyPoints: number,
  activationCount: number,
  autoResultFormula: ModifierAutoResultFormula | null,
  autoResultSuccessExpression: string | null,
  autoResultFailureExpression: string | null,
) {
  if (killsCount <= 0) {
    return 0
  }

  return roundModifierScore(
    evaluateModifierScoreFormulaSuccess(
      {
        mode: autoResultFormula ?? 'flat_per_kill',
        successExpression: autoResultSuccessExpression,
        failureExpression: autoResultFailureExpression,
      },
      {
        killsCount,
        bountyCount,
        scoreUnit,
        baseScore: scoreUnit,
        perKillBonus,
        failurePenaltyPoints,
        activationCount,
        totalOutcomeCount: killsCount + bountyCount,
      },
    ),
  )
}

function computeAutoResultFailureScoreDelta(
  scoreUnit: number,
  killsCount: number,
  bountyCount: number,
  perKillBonus: number,
  failurePenaltyPoints: number,
  activationCount: number,
  autoResultFormula: ModifierAutoResultFormula | null,
  autoResultSuccessExpression: string | null,
  autoResultFailureExpression: string | null,
) {
  const customFailureScoreDelta =
    autoResultFormula === 'custom_expression'
      ? evaluateModifierScoreFormulaFailure(
          {
            mode: autoResultFormula,
            successExpression: autoResultSuccessExpression,
            failureExpression: autoResultFailureExpression,
          },
          {
            killsCount,
            bountyCount,
            scoreUnit,
            baseScore: scoreUnit,
            perKillBonus,
            failurePenaltyPoints,
            activationCount,
            totalOutcomeCount: killsCount + bountyCount,
          },
        )
      : null

  if (customFailureScoreDelta != null) {
    return roundModifierScore(customFailureScoreDelta)
  }

  return -1 * failurePenaltyPoints
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
      countValue: Math.max(current.countValue, entry.countValue),
      isConditionMet: current.isConditionMet || entry.isConditionMet,
      manualScoreDelta: current.manualScoreDelta + entry.manualScoreDelta,
      manualKillDelta: current.manualKillDelta + entry.manualKillDelta,
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

function distributeInteger(total: number, count: number) {
  if (count <= 1) {
    return [total]
  }

  const base = Math.trunc(total / count)
  const remainder = total - base * count
  const direction = remainder >= 0 ? 1 : -1
  const values = Array.from({ length: count }, () => base)

  for (let index = 0; index < Math.abs(remainder); index += 1) {
    values[index] = (values[index] ?? 0) + direction
  }

  return values
}
