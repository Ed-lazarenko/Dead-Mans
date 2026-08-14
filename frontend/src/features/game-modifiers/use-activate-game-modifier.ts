import { useMutation, useMutationState, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { ErrorResponse, GameModifierState } from '../../shared/api/contracts/index.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { activateGameModifier } from './api/game-modifiers-api.ts'
import { gameModifierQueryKeys } from './api/game-modifier-queries.ts'

const activateGameModifierMutationKey = ['gameModifiers', 'activate'] as const

export function useActivateGameModifier() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const pendingMutationVariables = useMutationState<string | undefined>({
    filters: {
      mutationKey: activateGameModifierMutationKey,
      status: 'pending',
    },
    select: (mutation) =>
      typeof mutation.state.variables === 'string' ? mutation.state.variables : undefined,
  })

  const pendingModifierIds = pendingMutationVariables.filter(
    (value): value is string => value != null,
  )

  const mutation = useMutation({
    mutationKey: activateGameModifierMutationKey,
    mutationFn: (modifierId: string) => activateGameModifier(modifierId),
    onSuccess: () => {
      setToastMessage(t('gameModifiers.activateSuccess'))

      void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
    },
    onError: (error, modifierId) => {
      setToastMessage(t(resolveActivationErrorKey(error)))
      queryClient.setQueryData<GameModifierState | null>(gameModifierQueryKeys.state(), (current) =>
        applyActivationErrorState(current, modifierId, error),
      )
      void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
    },
  })

  return {
    activate: mutation.mutate,
    isActivating: pendingModifierIds.length > 0,
    pendingModifierId: pendingModifierIds.at(-1) ?? null,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}

function applyActivationErrorState(
  current: GameModifierState | null | undefined,
  modifierId: string,
  error: unknown,
): GameModifierState | null {
  if (!current) {
    return current ?? null
  }

  const blockedReason = resolveBlockedReasonFromError(error)
  if (!blockedReason) {
    return current
  }

  if (blockedReason === 'ordering_closed') {
    return {
      ...current,
      isOrderingOpen: false,
      availableModifiers: current.availableModifiers.map((item) => ({
        ...item,
        canActivate: false,
        blockedReason: 'ordering_closed',
      })),
    }
  }

  if (blockedReason === 'active_team_member') {
    return {
      ...current,
      availableModifiers: current.availableModifiers.map((item) => ({
        ...item,
        canActivate: false,
        blockedReason: 'active_team_member',
      })),
    }
  }

  return {
    ...current,
    availableModifiers: current.availableModifiers.map((item) =>
      item.modifier.id === modifierId
        ? {
            ...item,
            canActivate: false,
            blockedReason,
          }
        : item,
    ),
  }
}

function resolveBlockedReasonFromError(
  error: unknown,
): GameModifierState['availableModifiers'][number]['blockedReason'] | null {
  if (!(error instanceof ApiError)) {
    return null
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierOrderingClosed:
      return 'ordering_closed'
    case API_ERROR_CODES.gameModifierActiveTeamMember:
      return 'active_team_member'
    case API_ERROR_CODES.gameModifierLimitReached:
      return 'limit_reached'
    case API_ERROR_CODES.gameModifierConflictActive:
      return 'conflict_active'
    case API_ERROR_CODES.gameModifierInsufficientQuizPoints:
      return 'insufficient_points'
    default:
      return null
  }
}

function resolveActivationErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierNotEnabled:
      return 'gameModifiers.notEnabled'
    case API_ERROR_CODES.gameModifierGameNotActive:
      return 'gameModifiers.noGame'
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
    default:
      return 'gameModifiers.activateFailed'
  }
}
