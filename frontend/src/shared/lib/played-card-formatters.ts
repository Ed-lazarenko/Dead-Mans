import type { TFunction } from 'i18next'

export function formatPlayedCardModifierOutcomeStatus(t: TFunction, status: string) {
  const normalized = normalizePlayedCardModifierOutcomeStatus(status)
  const translationKey = `gameHistory.modifierOutcomeStatus.${normalized}`
  const translated = t(translationKey)
  return translated === translationKey ? formatEnumLabel(status) : translated
}

export function getPlayedCardModifierOutcomeColor(
  status: string,
): 'default' | 'success' | 'warning' {
  switch (normalizePlayedCardModifierOutcomeStatus(status)) {
    case 'completed':
      return 'success'
    case 'failed':
      return 'warning'
    default:
      return 'default'
  }
}

export function normalizePlayedCardModifierOutcomeStatus(status: string) {
  const normalized = status.toLowerCase().replace(/\s+/g, '_')
  return normalized === 'canceled' ? 'cancelled' : normalized
}

function formatEnumLabel(value: string) {
  return value.replace(/_/g, ' ')
}
