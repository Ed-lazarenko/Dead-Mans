import type { ParseKeys, TFunction } from 'i18next'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'

function extractCode(error: unknown): string | undefined {
  if (error instanceof ApiError && error.details && typeof error.details === 'object') {
    const code = (error.details as { code?: unknown }).code
    return typeof code === 'string' ? code : undefined
  }
  return undefined
}

function extractErrorText(error: unknown): string | undefined {
  if (error instanceof ApiError && error.details && typeof error.details === 'object') {
    const message = (error.details as { error?: unknown }).error
    return typeof message === 'string' ? message : undefined
  }
  return undefined
}

const codeToKey: Record<string, Extract<ParseKeys, `gameCatalog.errors.${string}`>> = {
  'game_modifier.duplicate_code': 'gameCatalog.errors.duplicateCode',
  'game_modifier.not_found': 'gameCatalog.errors.notFound',
  'game_modifier.invalid_request': 'gameCatalog.errors.invalidRequest',
  'game_question.duplicate_code': 'gameCatalog.errors.duplicateCode',
  'game_question.not_found': 'gameCatalog.errors.notFound',
  'game_question.invalid_request': 'gameCatalog.errors.invalidRequest',
  'game_question.category_not_empty': 'gameCatalog.errors.categoryNotEmpty',
}

export function resolveCatalogErrorMessage(error: unknown, t: TFunction): string {
  const code = extractCode(error)
  const key = code ? codeToKey[code] : undefined
  const errorText = extractErrorText(error)
  return errorText ?? t(key ?? 'gameCatalog.errors.generic')
}
