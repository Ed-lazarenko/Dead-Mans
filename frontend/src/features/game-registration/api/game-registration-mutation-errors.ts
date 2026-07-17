import type { ParseKeys, TFunction } from 'i18next'
import { API_ERROR_CODES } from '../../../shared/api/errors/api-error-codes.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import type { ErrorResponse } from '../../../shared/api/contracts/index.ts'

const REGISTRATION_ERROR_I18N_KEYS: Partial<Record<NonNullable<ErrorResponse['code']>, ParseKeys>> =
  {
    [API_ERROR_CODES.gameRegistrationNotOpen]: 'gameRegistration.errors.notOpen',
    [API_ERROR_CODES.gameRegistrationNoSlots]: 'gameRegistration.errors.noSlots',
    [API_ERROR_CODES.gameRegistrationAlreadyOnTeam]: 'gameRegistration.errors.alreadyOnTeam',
    [API_ERROR_CODES.gameRegistrationTeamNotFound]: 'gameRegistration.errors.teamNotFound',
    [API_ERROR_CODES.gameRegistrationTeamNotJoinable]: 'gameRegistration.errors.teamNotJoinable',
    [API_ERROR_CODES.gameRegistrationNotTeamMember]: 'gameRegistration.errors.notTeamMember',
    [API_ERROR_CODES.gameRegistrationInvitationInvalid]:
      'gameRegistration.errors.invitationInvalid',
    [API_ERROR_CODES.gameRegistrationSlotNotFound]: 'gameRegistration.errors.slotNotFound',
    [API_ERROR_CODES.gameRegistrationSlotNotAvailable]: 'gameRegistration.errors.slotNotAvailable',
    [API_ERROR_CODES.gameRegistrationUserNotFound]: 'gameRegistration.errors.userNotFound',
    [API_ERROR_CODES.gameRegistrationPendingInvitation]:
      'gameRegistration.errors.pendingInvitation',
    [API_ERROR_CODES.gameRegistrationTeamActiveInGame]: 'gameRegistration.errors.teamActiveInGame',
    [API_ERROR_CODES.gameRegistrationOperationFailed]: 'gameRegistration.errors.operationFailed',
    [API_ERROR_CODES.gameLifecycleNoConfirmedTeams]: 'gameRegistration.errors.noConfirmedTeams',
    [API_ERROR_CODES.gameLifecycleUnconfirmedTeams]: 'gameRegistration.errors.unconfirmedTeams',
    [API_ERROR_CODES.gameLifecyclePendingInvitations]:
      'gameRegistration.errors.pendingStartInvitations',
    [API_ERROR_CODES.gameLifecyclePendingDisbandRequests]:
      'gameRegistration.errors.pendingDisbandRequests',
    [API_ERROR_CODES.gameLifecycleInvalidConfirmedTeamRoster]:
      'gameRegistration.errors.invalidConfirmedTeamRoster',
    [API_ERROR_CODES.gameLifecycleActiveAlreadyExists]:
      'gameRegistration.errors.activeGameAlreadyExists',
    [API_ERROR_CODES.gameLifecycleGameNotReady]: 'gameRegistration.errors.gameNotReady',
  }

function readApiErrorPayload(error: unknown): ErrorResponse | undefined {
  if (!(error instanceof ApiError) || !error.details || typeof error.details !== 'object') {
    return undefined
  }

  const body = error.details as Partial<ErrorResponse>
  return typeof body.error === 'string' ? (body as ErrorResponse) : undefined
}

export function getGameRegistrationMutationErrorMessage(
  error: unknown,
  t: TFunction<'translation'>,
): string {
  const payload = readApiErrorPayload(error)
  if (payload?.code) {
    const key = REGISTRATION_ERROR_I18N_KEYS[payload.code]
    if (key) {
      return t(key)
    }
  }

  if (error instanceof ApiError) {
    if (error.status === 401) {
      return t('gameRegistration.errors.unauthorized')
    }

    if (error.status === 403) {
      return t('gameRegistration.errors.forbidden')
    }
  }

  return t('gameRegistration.errors.generic')
}
