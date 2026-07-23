import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { deriveModifierRoundSummaryMeta } from './modifier-round-summary.ts'

export function buildModifierSearchText(
  modifier: GameModifierDefinition,
  extraTerms: readonly string[] = [],
) {
  const roundSummaryMeta = deriveModifierRoundSummaryMeta(modifier)
  const parts = [
    modifier.name,
    modifier.description,
    modifier.scoringType,
    modifier.mechanicType,
    modifier.category,
    modifier.iconEmoji ?? '',
    modifier.activationCommand ?? '',
    modifier.requiresHostControl ? 'host control manual host ведущий контроль' : '',
    roundSummaryMeta.type,
    roundSummaryMeta.countInput ?? '',
    roundSummaryMeta.autoResultFormula ?? '',
    roundSummaryMeta.autoResultSuccessExpression ?? '',
    roundSummaryMeta.autoResultFailureExpression ?? '',
    roundSummaryMeta.conditionType ?? '',
    modifier.effect.ruleText ?? '',
    ...(modifier.effect.traits ?? []),
    ...(modifier.effect.resolutionInputs ?? []),
    ...(modifier.effect.conditions ?? []).flatMap((condition) => [
      condition.type ?? '',
      condition.source ?? '',
    ]),
    modifier.effect.killEffect?.killDeltaMode ?? '',
    modifier.effect.killEffect?.condition ?? '',
    ...(modifier.effect.killEffect?.excludedWeapons ?? []),
    modifier.effect.multiplierEffect?.target ?? '',
    modifier.effect.multiplierEffect?.activeWindow ?? '',
    modifier.effect.multiplierEffect?.stopCondition ?? '',
    modifier.effect.mentorEffect?.loadoutText ?? '',
    ...extraTerms,
  ]

  return parts.join(' ').replace(/\s+/g, ' ').trim().toLowerCase()
}

export function matchesModifierSearch(
  modifier: GameModifierDefinition,
  search: string,
  extraTerms: readonly string[] = [],
) {
  const normalizedSearch = search.trim().toLowerCase()
  if (!normalizedSearch) {
    return true
  }

  return buildModifierSearchText(modifier, extraTerms).includes(normalizedSearch)
}
