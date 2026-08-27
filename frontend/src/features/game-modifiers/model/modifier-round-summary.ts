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
  'automatic',
  'condition',
  'manual_count',
] as const
export type ModifierRoundSummaryType = (typeof modifierRoundSummaryTypes)[number]

interface ModifierRoundSummaryMeta {
  type: ModifierRoundSummaryType
  includeInRoundSummary: boolean
}

export function deriveModifierRoundSummaryMeta(
  modifier: ModifierDefinitionLike,
): ModifierRoundSummaryMeta {
  const behavior = modifier.behaviorV2
  if (behavior.kind !== 'scoring') return { type: 'passive', includeInRoundSummary: false }

  return behavior.resolution.type === 'boolean'
    ? { type: 'condition', includeInRoundSummary: true }
    : behavior.resolution.type === 'nonNegativeCount'
      ? { type: 'manual_count', includeInRoundSummary: true }
      : { type: 'automatic', includeInRoundSummary: true }
}
