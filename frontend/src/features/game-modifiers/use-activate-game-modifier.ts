import { useMutation, useMutationState, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { ErrorResponse, GameModifierState } from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { activateGameModifier } from './api/game-modifiers-api.ts'
import { gameModifierQueryKeys } from './api/game-modifier-queries.ts'

const activateGameModifierMutationKey = ['gameModifiers', 'activate'] as const

export function useActivateGameModifier() {
  const { t } = useTranslation()
  const { user } = useAuth()
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
    onSuccess: (_result, modifierId) => {
      setToastMessage(t('gameModifiers.activateSuccess'))

      if (user) {
        queryClient.setQueryData<GameModifierState | null>(
          gameModifierQueryKeys.state(),
          (current) => applyActivatedModifierState(current, modifierId, user.id, user.displayName),
        )
      }

      void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
    },
    onError: (error, modifierId) => {
      setToastMessage(t(resolveActivationErrorKey(error)))
      queryClient.setQueryData<GameModifierState | null>(
        gameModifierQueryKeys.state(),
        (current) => applyActivationErrorState(current, modifierId, error),
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

function applyActivatedModifierState(
  current: GameModifierState | null | undefined,
  modifierId: string,
  activatedByUserId: string,
  activatedByDisplayName: string,
): GameModifierState | null {
  if (!current) {
    return current ?? null
  }

  const availability = current.availableModifiers.find((item) => item.modifier.id === modifierId)
  if (!availability) {
    return current
  }

  const nextAvailableQuizPoints = Math.max(
    0,
    current.availableQuizPoints - availability.modifier.activationCost,
  )
  const nextSpentQuizPoints = current.spentQuizPoints + availability.modifier.activationCost
  const activatedAtUtc = new Date().toISOString()

  return {
    ...current,
    availableQuizPoints: nextAvailableQuizPoints,
    spentQuizPoints: nextSpentQuizPoints,
    activeModifiers: [
      {
        activationId: `local-${modifierId}-${Date.now()}`,
        modifierId,
        modifierName: availability.modifier.name,
        activatedByUserId,
        activatedByDisplayName,
        activationCost: availability.modifier.activationCost,
        activatedAtUtc,
      },
      ...current.activeModifiers,
    ],
    availableModifiers: current.availableModifiers.map((item) => {
      const nextActivationsCount =
        item.modifier.id === modifierId ? item.activationsCount + 1 : item.activationsCount
      const limitReached = item.limit != null && nextActivationsCount >= item.limit
      const hasConflictWithActivatedModifier =
        item.modifier.id !== modifierId && item.modifier.conflictingModifierIds.includes(modifierId)
      const blockedReason = resolveAvailabilityBlockedReason(
        current.isOrderingOpen,
        limitReached,
        hasConflictWithActivatedModifier || item.blockedReason === 'conflict_active',
        nextAvailableQuizPoints < item.modifier.activationCost,
      )

      return {
        ...item,
        isActive: item.modifier.id === modifierId ? true : item.isActive,
        activationsCount: nextActivationsCount,
        blockedReason,
        canActivate: blockedReason == null,
      }
    }),
  }
}

function resolveAvailabilityBlockedReason(
  isOrderingOpen: boolean,
  limitReached: boolean,
  hasConflict: boolean,
  insufficientPoints: boolean,
): GameModifierState['availableModifiers'][number]['blockedReason'] {
  if (!isOrderingOpen) {
    return 'ordering_closed'
  }

  if (limitReached) {
    return 'limit_reached'
  }

  if (hasConflict) {
    return 'conflict_active'
  }

  if (insufficientPoints) {
    return 'insufficient_points'
  }

  return null
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
