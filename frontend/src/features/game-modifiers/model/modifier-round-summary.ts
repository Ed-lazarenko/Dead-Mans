import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
  UpdateGameModifierRequest,
} from '../../../shared/api/contracts/index.ts'

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
}

export function deriveModifierRoundSummaryMeta(
  modifier: ModifierDefinitionLike,
): ModifierRoundSummaryMeta {
  const behavior = modifier.behaviorV2
  const formula = behavior.formulaReference
  const parameters = formula?.parameters
  const base = {
    includeInRoundSummary: behavior.kind === 'scoring',
  } as const

  if (formula?.code === 'growing_kill_value' && parameters?.type === 'growingKillValue') {
    return {
      ...base,
      type: 'auto_result',
      countInput: null,
    }
  }
  if (formula?.code === 'bonus_kill_on_condition' && parameters?.type === 'bonusKillOnCondition') {
    return {
      ...base,
      type: 'toggle_bonus',
      countInput: null,
    }
  }
  if (formula?.code === 'bonus_kills_by_count' && parameters?.type === 'bonusKillsByCount') {
    return {
      ...base,
      type: 'counted_bonus',
      countInput: behavior.performer === 'mentor' ? 'mentorKills' : 'bonusKills',
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
    }
  }
  return {
    ...base,
    type: 'passive',
    includeInRoundSummary: false,
    countInput: null,
  }
}
