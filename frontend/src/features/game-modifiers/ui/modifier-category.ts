import { useTranslation } from 'react-i18next'
import type { GameModifierAvailability } from '../../../shared/api/contracts/index.ts'

const CATEGORY_LABEL_KEYS = {
  preparation: 'common.modifiers.categories.preparation',
  round: 'common.modifiers.categories.round',
  result: 'common.modifiers.categories.result',
} as const

export function getCategoryLabel(
  t: ReturnType<typeof useTranslation>['t'],
  category: GameModifierAvailability['modifier']['category'],
): string {
  const translate = t as unknown as (key: string) => string
  return translate(CATEGORY_LABEL_KEYS[category])
}
