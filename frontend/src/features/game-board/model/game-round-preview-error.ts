import { ApiError } from '../../../shared/api/errors/ApiError.ts'

export function getGameRoundPreviewErrorCode(error: unknown) {
  if (!(error instanceof ApiError) || !error.details || typeof error.details !== 'object') {
    return null
  }

  const code = Reflect.get(error.details, 'code')
  return typeof code === 'string' ? code : null
}
