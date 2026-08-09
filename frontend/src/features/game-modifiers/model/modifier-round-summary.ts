import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
  UpdateGameModifierRequest,
} from '../../../shared/api/contracts/index.ts'
import type { ModifierScoreFormulaMode } from './modifier-score-formula.ts'

type ModifierDefinitionLike =
  | GameModifierDefinition
  | CreateGameModifierRequest
  | UpdateGameModifierRequest

export const modifierRoundSummaryTypes = [
  'passive',
  'auto_result',
  'toggle_bonus',
  'counted_bonus',
  'kill_multiplier',
  'manual_points',
] as const

export type ModifierRoundSummaryType = (typeof modifierRoundSummaryTypes)[number]
export type ModifierRoundSummaryCountInput = 'bonusKills' | 'mentorKills' | 'killsDuringWindow'
type ModifierAutoResultFormula = ModifierScoreFormulaMode

const STACKING_PER_KILL_BONUS_TRAIT = 'stacking_per_kill_bonus'
const ZHAZHDA_MODIFIER_ID = '10000000-0000-0000-0000-000000000002'

interface ModifierRoundSummaryMeta {
  type: ModifierRoundSummaryType
  includeInRoundSummary: boolean
  countInput: ModifierRoundSummaryCountInput | null
  autoResultFormula: ModifierAutoResultFormula | null
  autoResultSuccessExpression: string | null
  autoResultFailureExpression: string | null
  conditionType: string | null
  perKillBonus: number | null
  failurePenaltyPoints: number | null
  flatPointsDelta: number | null
  killDeltaValue: number | null
  multiplierDelta: number | null
}

export function deriveModifierRoundSummaryMeta(
  modifier: ModifierDefinitionLike,
): ModifierRoundSummaryMeta {
  const mechanicType = normalizeCode(modifier.mechanicType)
  const scoringType = normalizeCode(modifier.scoringType)
  const effect = modifier.effect
  const scoreImpact = effect?.scoreImpact
  const killEffect = effect?.killEffect
  const multiplierEffect = effect?.multiplierEffect
  const traits = new Set((effect?.traits ?? []).map(normalizeCode))
  const resolutionInputs = new Set((effect?.resolutionInputs ?? []).map(normalizeCode))
  const conditionType =
    normalizeCode(killEffect?.condition) || normalizeCode(effect?.conditions?.[0]?.type) || null
  const killDeltaValue =
    typeof killEffect?.killDeltaValue === 'number'
      ? killEffect.killDeltaValue
      : typeof scoreImpact?.killDelta === 'number'
        ? scoreImpact.killDelta
        : null
  const multiplierDelta =
    typeof multiplierEffect?.delta === 'number'
      ? multiplierEffect.delta
      : typeof scoreImpact?.multiplierDelta === 'number'
        ? scoreImpact.multiplierDelta
        : null
  const perKillBonus =
    typeof scoreImpact?.perKillBonus === 'number' ? scoreImpact.perKillBonus : null
  const failurePenaltyPoints =
    typeof scoreImpact?.failurePenaltyPoints === 'number' ? scoreImpact.failurePenaltyPoints : null
  const autoResultFormula = resolveAutoResultFormula(modifier, traits)
  const flatPointsDelta =
    typeof scoreImpact?.pointsDelta === 'number' ? scoreImpact.pointsDelta : null
  const killDeltaMode = normalizeCode(killEffect?.killDeltaMode)

  if (mechanicType === 'multiplier' && multiplierDelta !== null) {
    return {
      type: 'kill_multiplier',
      includeInRoundSummary: true,
      countInput: 'killsDuringWindow',
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue,
      multiplierDelta,
    }
  }

  if (
    mechanicType === 'restriction_with_reward' &&
    (perKillBonus !== null || failurePenaltyPoints !== null)
  ) {
    return {
      type: 'auto_result',
      includeInRoundSummary: true,
      countInput: null,
      autoResultFormula: autoResultFormula.mode,
      autoResultSuccessExpression: autoResultFormula.successExpression,
      autoResultFailureExpression: autoResultFormula.failureExpression,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue,
      multiplierDelta,
    }
  }

  if (resolutionInputs.has('mentorkills') || killDeltaMode === 'mentor_kills_as_team_kills') {
    return {
      type: 'counted_bonus',
      includeInRoundSummary: true,
      countInput: 'mentorKills',
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue: killDeltaValue ?? 1,
      multiplierDelta,
    }
  }

  if (mechanicType === 'kill_counter' && killDeltaValue !== null) {
    const hasBinaryCondition = conditionType !== null || killDeltaMode === 'conditional_bonus_kill'

    return {
      type: hasBinaryCondition ? 'toggle_bonus' : 'counted_bonus',
      includeInRoundSummary: true,
      countInput: hasBinaryCondition ? null : 'bonusKills',
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue,
      multiplierDelta,
    }
  }

  if (flatPointsDelta !== null && flatPointsDelta !== 0) {
    return {
      type: 'manual_points',
      includeInRoundSummary: true,
      countInput: null,
      autoResultFormula: null,
      autoResultSuccessExpression: null,
      autoResultFailureExpression: null,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue,
      multiplierDelta,
    }
  }

  if (scoringType !== 'non_scoring' && (perKillBonus !== null || multiplierDelta !== null)) {
    return {
      type: 'auto_result',
      includeInRoundSummary: true,
      countInput: null,
      autoResultFormula: autoResultFormula.mode,
      autoResultSuccessExpression: autoResultFormula.successExpression,
      autoResultFailureExpression: autoResultFormula.failureExpression,
      conditionType,
      perKillBonus,
      failurePenaltyPoints,
      flatPointsDelta,
      killDeltaValue,
      multiplierDelta,
    }
  }

  return {
    type: 'passive',
    includeInRoundSummary: false,
    countInput: null,
    autoResultFormula: null,
    autoResultSuccessExpression: null,
    autoResultFailureExpression: null,
    conditionType,
    perKillBonus,
    failurePenaltyPoints,
    flatPointsDelta,
    killDeltaValue,
    multiplierDelta,
  }
}

function normalizeCode(value: string | null | undefined) {
  return value?.trim().toLowerCase() ?? ''
}

function resolveAutoResultFormula(
  modifier: ModifierDefinitionLike,
  traits: ReadonlySet<string>,
): {
  mode: ModifierAutoResultFormula
  successExpression: string | null
  failureExpression: string | null
} {
  const configuredFormula = modifier.effect?.scoreImpact?.scoreFormula
  const configuredMode = normalizeCode(configuredFormula?.mode) as ModifierAutoResultFormula
  if (
    configuredMode === 'flat_per_kill' ||
    configuredMode === 'stacking_per_kill_bonus' ||
    configuredMode === 'custom_expression'
  ) {
    return {
      mode: configuredMode,
      successExpression: configuredFormula?.successExpression?.trim() || null,
      failureExpression: configuredFormula?.failureExpression?.trim() || null,
    }
  }

  if (traits.has(STACKING_PER_KILL_BONUS_TRAIT)) {
    return {
      mode: 'stacking_per_kill_bonus',
      successExpression: null,
      failureExpression: null,
    }
  }

  if ('id' in modifier && modifier.id === ZHAZHDA_MODIFIER_ID) {
    return {
      mode: 'stacking_per_kill_bonus',
      successExpression: null,
      failureExpression: null,
    }
  }

  return {
    mode: 'flat_per_kill',
    successExpression: null,
    failureExpression: null,
  }
}
