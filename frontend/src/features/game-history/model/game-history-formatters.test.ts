import { describe, expect, it } from 'vitest'
import i18n from '../../../i18n.ts'
import { formatShortCardLabel } from './game-history-formatters.ts'

describe('game history formatters', () => {
  it('labels the snapshotted card value independently from the card title', () => {
    const t = i18n.getFixedT('ru')

    expect(formatShortCardLabel({ cellTitle: 'Карточка 100', cellCost: 375 }, t)).toBe(
      'Карточка 100 · Стоимость 375 очк.',
    )
  })
})
