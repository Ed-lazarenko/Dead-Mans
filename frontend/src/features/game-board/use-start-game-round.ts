import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import {
  finalizeGameRound,
  reviewGameRound,
  startGameRound,
} from '../game-rounds/api/game-rounds-api.ts'
import { gameHistoryQueryKeys } from '../game-history/api/game-history-queries.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'
import type { CompleteRoundInput } from './model/game-round-summary-form.ts'

async function invalidateRoundState(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey })
  await queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
  await queryClient.invalidateQueries({ queryKey: gameHistoryQueryKeys.all })
}

export function useStartGameRound() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const startMutation = useMutation({
    mutationFn: (input: { cellId: string; teamId: string }) =>
      startGameRound({ cellId: input.cellId, teamId: input.teamId }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelStartSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelStartFailed'))
    },
  })

  const reviewMutation = useMutation({
    mutationFn: (roundId: string) => reviewGameRound(roundId),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelReviewSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelReviewFailed'))
    },
  })

  const completeMutation = useMutation({
    mutationFn: (input: CompleteRoundInput) =>
      finalizeGameRound(input.roundId, {
        status: 'completed',
        finalScore: input.finalScore,
        killsCount: input.killsCount,
        bountyCount: input.bountyCount,
        notes: null,
        modifierResults: input.modifierResults,
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelCompleteSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelCompleteFailed'))
    },
  })

  const isMutating =
    startMutation.isPending || reviewMutation.isPending || completeMutation.isPending

  return {
    isChangingRoundStage: isMutating,
    startRound: startMutation.mutate,
    reviewRound: reviewMutation.mutate,
    completeRound: completeMutation.mutateAsync,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
