import type { ParseKeys, TFunction } from 'i18next'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'

function extractCode(error: unknown): string | undefined {
  if (error instanceof ApiError && error.details && typeof error.details === 'object') {
    const code = (error.details as { code?: unknown }).code
    return typeof code === 'string' ? code : undefined
  }
  return undefined
}

const codeToKey: Record<string, Extract<ParseKeys, `gameCatalog.errors.${string}`>> = {
  'game_modifier.not_found': 'gameCatalog.errors.notFound',
  'game_modifier.invalid_request': 'gameCatalog.errors.invalidRequest',
  content_locked_by_active_game: 'gameCatalog.errors.contentLocked',
  'game_question.duplicate_code': 'gameCatalog.errors.duplicateCode',
  'game_question.not_found': 'gameCatalog.errors.notFound',
  'game_question.invalid_request': 'gameCatalog.errors.invalidRequest',
  'game_question.category_not_found': 'gameCatalog.errors.categoryNotFound',
  'game_question.category_not_empty': 'gameCatalog.errors.categoryNotEmpty',
  'game_question.category_protected': 'gameCatalog.errors.categoryProtected',
}

export function resolveCatalogErrorMessage(error: unknown, t: TFunction): string {
  const code = extractCode(error)
  const key = code ? codeToKey[code] : undefined
  return t(key ?? 'gameCatalog.errors.generic')
}
