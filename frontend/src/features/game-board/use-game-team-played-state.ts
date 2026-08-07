import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { gameRegistrationQueryKeys } from '../game-registration/api/game-registration-queries.ts'
import {
  currentGameBoardQueryOptions,
  currentGameTeamQueueQueryOptions,
} from './api/game-board-queries.ts'
import { setGameTeamPlayedState } from './api/game-board-data-access.ts'

function getPlayedStateErrorMessage(
  error: unknown,
  messages: {
    fallback: string
    roundInProgress: string
    noActiveGame: string
    notFound: string
    notConfirmed: string
  },
) {
  if (
    error instanceof ApiError &&
    error.status === 409 &&
    error.details &&
    typeof error.details === 'object' &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardTeamPlayedStateRoundInProgress
  ) {
    return messages.roundInProgress
  }

  if (
    error instanceof ApiError &&
    error.status === 404 &&
    error.details &&
    typeof error.details === 'object' &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardTeamPlayedStateNoActiveGame
  ) {
    return messages.noActiveGame
  }

  if (
    error instanceof ApiError &&
    error.status === 404 &&
    error.details &&
    typeof error.details === 'object' &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardTeamPlayedStateNotFound
  ) {
    return messages.notFound
  }

  if (
    error instanceof ApiError &&
    error.status === 409 &&
    error.details &&
    typeof error.details === 'object' &&
    'code' in error.details &&
    error.details.code === API_ERROR_CODES.gameBoardTeamPlayedStateNotConfirmed
  ) {
    return messages.notConfirmed
  }

  return messages.fallback
}

export function useGameTeamPlayedState() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (input: { teamId: string; isPlayed: boolean }) =>
      setGameTeamPlayedState(input.teamId, input.isPlayed),
    onSuccess: async (_, variables) => {
      setToastMessage(
        variables.isPlayed
          ? t('gameBoard.teamPlayedMarkSuccess')
          : t('gameBoard.teamPlayedResetSuccess'),
      )

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: currentGameBoardQueryOptions.queryKey,
        }),
        queryClient.invalidateQueries({
          queryKey: currentGameTeamQueueQueryOptions.queryKey,
        }),
        queryClient.invalidateQueries({
          queryKey: gameRegistrationQueryKeys.all,
        }),
      ])
    },
    onError: (error) => {
      setToastMessage(
        getPlayedStateErrorMessage(error, {
          fallback: t('gameBoard.teamPlayedUpdateFailed'),
          roundInProgress: t('gameBoard.teamPlayedRoundInProgress'),
          noActiveGame: t('gameBoard.teamPlayedNoActiveGame'),
          notFound: t('gameBoard.teamPlayedNotFound'),
          notConfirmed: t('gameBoard.teamPlayedNotConfirmed'),
        }),
      )
    },
  })

  return {
    isUpdatingPlayedState: mutation.isPending,
    updatingTeamId: mutation.variables?.teamId ?? null,
    setTeamPlayedState: mutation.mutateAsync,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
