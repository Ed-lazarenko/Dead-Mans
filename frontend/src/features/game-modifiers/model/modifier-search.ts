import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { deriveModifierRoundSummaryMeta } from './modifier-round-summary.ts'

export function buildModifierSearchText(
  modifier: GameModifierDefinition,
  extraTerms: readonly string[] = [],
  locale?: string,
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

  return parts.join(' ').replace(/\s+/g, ' ').trim().toLocaleLowerCase(locale)
}

export function matchesModifierSearch(
  modifier: GameModifierDefinition,
  search: string,
  extraTerms: readonly string[] = [],
  locale?: string,
) {
  const normalizedSearch = search.trim().toLocaleLowerCase(locale)
  if (!normalizedSearch) {
    return true
  }

  return buildModifierSearchText(modifier, extraTerms, locale).includes(normalizedSearch)
}
