import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { TFunction } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { gameHistoryQueryKeys } from '../game-history/api/game-history-queries.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { awardManualQuizPoints, type ManualQuizAwardInput } from './api/manual-quiz-award-api.ts'

function getManualQuizAwardErrorMessage(error: unknown, t: TFunction<'translation'>) {
  if (error instanceof ApiError) {
    if (
      error.status === 400 &&
      typeof error.details === 'object' &&
      error.details !== null &&
      'code' in error.details &&
      error.details.code === API_ERROR_CODES.gameQuizManualAwardInvalidPoints
    ) {
      return t('gameBoard.manualQuizAwardInvalidPoints')
    }

    if (
      error.status === 404 &&
      typeof error.details === 'object' &&
      error.details !== null &&
      'code' in error.details &&
      error.details.code === API_ERROR_CODES.gameQuizManualAwardPlayerNotFound
    ) {
      return t('gameBoard.manualQuizAwardPlayerNotFound')
    }
  }

  return t('gameBoard.manualQuizAwardFailed')
}

export function useManualQuizAward() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)
  const [toastSeverity, setToastSeverity] = useState<'success' | 'error'>('success')

  const mutation = useMutation({
    mutationFn: (input: ManualQuizAwardInput) => awardManualQuizPoints(input),
    onSuccess: async (award) => {
      setToastSeverity('success')
      setToastMessage(
        t('gameBoard.manualQuizAwardSaved', {
          player: award.awardedToDisplayName,
          points: award.points,
        }),
      )
      await queryClient.invalidateQueries({
        queryKey: gameHistoryQueryKeys.all,
      })
    },
    onError: (error) => {
      setToastSeverity('error')
      setToastMessage(getManualQuizAwardErrorMessage(error, t))
    },
  })

  return {
    isAwardingManualQuizPoints: mutation.isPending,
    awardManualQuizPoints: mutation.mutate,
    toastMessage,
    toastSeverity,
    dismissToast: () => setToastMessage(null),
  }
}
