import { describe, expect, it } from 'vitest'
import i18n from '../../../i18n.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import { resolveCatalogErrorMessage } from './catalog-error.ts'

describe('resolveCatalogErrorMessage', () => {
  it('uses the localized message for a known API error code', () => {
    const error = new ApiError('HTTP 404', {
      status: 404,
      details: {
        code: 'game_question.category_not_found',
        error: 'Requested question category was not found.',
      },
    })

    expect(resolveCatalogErrorMessage(error, i18n.getFixedT('ru'))).toBe(
      'Категория вопросов не найдена. Возможно, она была удалена.',
    )
  })

  it('never exposes an untranslated backend message for an unknown code', () => {
    const error = new ApiError('HTTP 500', {
      status: 500,
      details: {
        code: 'unknown.code',
        error: 'Internal backend message',
      },
    })

    expect(resolveCatalogErrorMessage(error, i18n.getFixedT('pl'))).toBe(
      'Nie udało się wykonać operacji. Spróbuj ponownie.',
    )
  })

  it('explains an active-game content lock', () => {
    const error = new ApiError('HTTP 409', {
      status: 409,
      details: { code: 'content_locked_by_active_game' },
    })

    expect(resolveCatalogErrorMessage(error, i18n.getFixedT('ru'))).toBe(
      'Модификатор заблокирован, потому что он включён в активную игру.',
    )
  })
})
