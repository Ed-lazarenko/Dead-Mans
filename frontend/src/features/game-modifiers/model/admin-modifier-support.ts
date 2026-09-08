import type { ErrorResponse, GameModifierActivation } from '../../../shared/api/contracts/index.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../../shared/api/errors/api-error-codes.ts'

export interface CancelModifierOption {
  modifierId: string
  modifierName: string
}

export function buildCancelModifierOptions(
  activeModifiers: GameModifierActivation[],
): CancelModifierOption[] {
  const seenModifierIds = new Set<string>()
  const options: CancelModifierOption[] = []

  for (const activation of activeModifiers) {
    if (seenModifierIds.has(activation.modifierId)) {
      continue
    }

    seenModifierIds.add(activation.modifierId)
    options.push({
      modifierId: activation.modifierId,
      modifierName: activation.modifierName,
    })
  }

  return options
}

export function resolveAdminActivateErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierOrderingClosed:
      return 'gameModifiers.blockedReasons.ordering_closed'
    case API_ERROR_CODES.gameModifierActiveTeamMember:
      return 'gameModifiers.blockedReasons.active_team_member'
    case API_ERROR_CODES.gameModifierLimitReached:
      return 'gameModifiers.blockedReasons.limit_reached'
    case API_ERROR_CODES.gameModifierConflictActive:
      return 'gameModifiers.blockedReasons.conflict_active'
    case API_ERROR_CODES.gameModifierInsufficientQuizPoints:
      return 'gameModifiers.blockedReasons.insufficient_points'
    case API_ERROR_CODES.gameModifierPlayerNotFound:
      return 'gameModifiers.adminPanel.playerNotFound'
    default:
      return 'gameModifiers.activateFailed'
  }
}

export function resolveAdminCancelErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierActivationNotFound:
      return 'gameModifiers.adminPanel.activationNotFound'
    case API_ERROR_CODES.gameModifierActivationCancelInvalidState:
      return 'gameModifiers.adminPanel.alreadyAppliedInRound'
    case API_ERROR_CODES.gameRoundStaleVersion:
      return 'gameModifiers.adminPanel.staleRound'
    case API_ERROR_CODES.gameModifierActivationCancelReasonRequired:
      return 'gameModifiers.adminPanel.reasonRequired'
    default:
      return 'gameModifiers.activateFailed'
  }
}
