import { describe, expect, it } from 'vitest'
import i18n from '../../i18n.ts'
import { formatPlayedCardModifierOutcomeStatus } from './played-card-formatters.ts'

describe('formatPlayedCardModifierOutcomeStatus', () => {
  it('uses a localized fallback instead of exposing an unknown protocol value', () => {
    expect(
      formatPlayedCardModifierOutcomeStatus(i18n.getFixedT('uk'), 'future_server_status'),
    ).toBe('Невідомий статус')
  })
})
