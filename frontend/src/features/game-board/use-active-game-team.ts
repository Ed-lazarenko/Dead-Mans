import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { TFunction } from 'i18next'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { setActiveGameTeam } from './api/game-board-data-access.ts'
import {
  currentGameBoardQueryOptions,
  currentGameTeamQueueQueryOptions,
} from './api/game-board-queries.ts'

function getActiveTeamErrorMessage(error: unknown, t: TFunction<'translation'>) {
  if (
    error instanceof ApiError &&
    error.status === 409 &&
    typeof error.details === 'object' &&
    error.details !== null &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardActiveTeamAlreadyPlayed
  ) {
    return t('gameBoard.activeTeamAlreadyPlayed')
  }

  if (
    error instanceof ApiError &&
    error.status === 409 &&
    typeof error.details === 'object' &&
    error.details !== null &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardActiveTeamRoundInProgress
  ) {
    return t('gameBoard.activeTeamRoundInProgress')
  }

  return t('gameBoard.activeTeamSaveFailed')
}

export function useActiveGameTeam() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: setActiveGameTeam,
    onSuccess: async () => {
      setToastMessage(t('gameBoard.activeTeamSaved'))
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: currentGameBoardQueryOptions.queryKey,
        }),
        queryClient.invalidateQueries({
          queryKey: currentGameTeamQueueQueryOptions.queryKey,
        }),
      ])
    },
    onError: (error) => {
      setToastMessage(getActiveTeamErrorMessage(error, t))
    },
  })

  return {
    isSelectingActiveTeam: mutation.isPending,
    selectActiveTeam: mutation.mutate,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
