import { describe, expect, it } from 'vitest'
import i18n from '../../i18n.ts'
import {
  formatPlayedCardModifierOutcomeStatus,
  getPlayedCardModifierOutcomeColor,
} from './played-card-formatters.ts'

describe('formatPlayedCardModifierOutcomeStatus', () => {
  it('uses a localized fallback instead of exposing an unknown protocol value', () => {
    expect(
      formatPlayedCardModifierOutcomeStatus(i18n.getFixedT('uk'), 'future_server_status'),
    ).toBe('Невідомий статус')
  })

  it('formats V2 scoring and rule outcomes as first-class statuses', () => {
    const t = i18n.getFixedT('ru')

    expect(formatPlayedCardModifierOutcomeStatus(t, 'succeeded')).toBe('Условие выполнено')
    expect(formatPlayedCardModifierOutcomeStatus(t, 'not_succeeded')).toBe('Условие не выполнено')
    expect(formatPlayedCardModifierOutcomeStatus(t, 'calculated')).toBe('Рассчитан')
    expect(formatPlayedCardModifierOutcomeStatus(t, 'violated')).toBe('Правило нарушено')
    expect(getPlayedCardModifierOutcomeColor('calculated')).toBe('success')
    expect(getPlayedCardModifierOutcomeColor('violated')).toBe('warning')
  })
})
