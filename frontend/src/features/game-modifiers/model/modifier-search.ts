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
    modifier.behaviorV2.kind,
    modifier.behaviorV2.performer,
    modifier.behaviorV2.rule,
    modifier.behaviorV2.formulaReference?.code ?? '',
    modifier.behaviorV2.formulaReference?.parameters.type ?? '',
    modifier.category,
    modifier.iconEmoji ?? '',
    modifier.activationCommand ?? '',
    roundSummaryMeta.type,
    roundSummaryMeta.countInput ?? '',
    ...modifier.normalizedTags,
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
