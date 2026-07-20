import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { activeGameCardRunQueryOptions } from '../game-card-runs/api/game-card-runs-queries.ts'
import {
  finalizeGameCardRun,
  reviewGameCardRun,
  startGameCardRun,
} from '../game-card-runs/api/game-card-runs-api.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'

async function invalidateRoundState(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: activeGameCardRunQueryOptions.queryKey })
  await queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
}

export function useStartGameCardRun() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const startMutation = useMutation({
    mutationFn: (input: { cellId: string; teamId: string }) =>
      startGameCardRun({ cellId: input.cellId, teamId: input.teamId }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.runPanelStartSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.runPanelStartFailed'))
    },
  })

  const reviewMutation = useMutation({
    mutationFn: (cardRunId: string) => reviewGameCardRun(cardRunId),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.runPanelReviewSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.runPanelReviewFailed'))
    },
  })

  const completeMutation = useMutation({
    mutationFn: (cardRunId: string) =>
      finalizeGameCardRun(cardRunId, {
        status: 'completed',
        finalScore: null,
        notes: null,
        modifierResults: [],
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.runPanelCompleteSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.runPanelCompleteFailed'))
    },
  })

  const isMutating =
    startMutation.isPending || reviewMutation.isPending || completeMutation.isPending

  return {
    isChangingRoundStage: isMutating,
    startRound: startMutation.mutate,
    reviewRound: reviewMutation.mutate,
    completeRound: completeMutation.mutate,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
