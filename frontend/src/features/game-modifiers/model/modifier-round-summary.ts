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
] as const
export type ModifierRoundSummaryType = (typeof modifierRoundSummaryTypes)[number]
type ModifierRoundSummaryCountInput = 'bonusKills' | 'mentorKills' | 'killsDuringWindow'

interface ModifierRoundSummaryMeta {
  type: ModifierRoundSummaryType
  includeInRoundSummary: boolean
  countInput: ModifierRoundSummaryCountInput | null
  autoResultFormula: ModifierScoreFormulaMode | null
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
  const behavior = modifier.behaviorV2
  const formula = behavior.formulaReference
  const parameters = formula?.parameters
  const base = {
    includeInRoundSummary: behavior.kind === 'scoring',
    autoResultSuccessExpression: null,
    autoResultFailureExpression: null,
    conditionType: null,
    flatPointsDelta: null,
  } as const

  if (formula?.code === 'growing_kill_value' && parameters?.type === 'growingKillValue') {
    return {
      ...base,
      type: 'auto_result',
      countInput: null,
      autoResultFormula: 'stacking_per_kill_bonus',
      perKillBonus: parameters.incrementPointsPerKill,
      failurePenaltyPoints: parameters.zeroKillPenaltyPoints,
      killDeltaValue: null,
      multiplierDelta: null,
    }
  }
  if (formula?.code === 'bonus_kill_on_condition' && parameters?.type === 'bonusKillOnCondition') {
    return {
      ...base,
      type: 'toggle_bonus',
      countInput: null,
      autoResultFormula: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      killDeltaValue: parameters.successBonusKills,
      multiplierDelta: null,
    }
  }
  if (formula?.code === 'bonus_kills_by_count' && parameters?.type === 'bonusKillsByCount') {
    return {
      ...base,
      type: 'counted_bonus',
      countInput: behavior.performer === 'mentor' ? 'mentorKills' : 'bonusKills',
      autoResultFormula: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      killDeltaValue: parameters.bonusKillsPerUnit,
      multiplierDelta: null,
    }
  }
  if (
    formula?.code === 'window_kill_bonus_points' &&
    parameters?.type === 'windowKillBonusPoints'
  ) {
    return {
      ...base,
      type: 'kill_multiplier',
      countInput: 'killsDuringWindow',
      autoResultFormula: null,
      perKillBonus: null,
      failurePenaltyPoints: null,
      killDeltaValue: null,
      multiplierDelta: parameters.bonusRate,
    }
  }
  return {
    ...base,
    type: 'passive',
    includeInRoundSummary: false,
    countInput: null,
    autoResultFormula: null,
    perKillBonus: null,
    failurePenaltyPoints: null,
    killDeltaValue: null,
    multiplierDelta: null,
  }
}
