import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { ErrorResponse } from '../../shared/api/contracts/index.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { activateGameModifier } from './api/game-modifiers-api.ts'
import { gameModifierQueryKeys } from './api/game-modifier-queries.ts'

export function useActivateGameModifier() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (modifierId: string) => activateGameModifier(modifierId),
    onSuccess: async () => {
      setToastMessage(t('gameModifiers.activateSuccess'))
      await queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      await queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
    },
    onError: async (error) => {
      setToastMessage(t(resolveActivationErrorKey(error)))
      await queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
    },
  })

  return {
    activate: mutation.mutate,
    isActivating: mutation.isPending,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}

function resolveActivationErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
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
