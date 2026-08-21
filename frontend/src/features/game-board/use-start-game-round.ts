import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import {
  beginGameRoundGameplay,
  finalizeGameRound,
  prepareGameRound,
  rebuildGameRound,
  reviewGameRound,
  technicalCancelGameRound,
} from '../game-rounds/api/game-rounds-api.ts'
import { gameHistoryQueryKeys } from '../game-history/api/game-history-queries.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'
import type { CompleteRoundInput } from './model/game-round-summary-form.ts'

async function invalidateRoundState(queryClient: ReturnType<typeof useQueryClient>) {
  await queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey })
  await queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
  await queryClient.invalidateQueries({ queryKey: gameHistoryQueryKeys.all })
}

export interface TechnicalCancelRoundInput {
  roundId: string
  expectedRoundVersion: number
  reasonCode:
    | 'external_game_failure'
    | 'stream_or_infrastructure_failure'
    | 'application_error'
    | 'operator_error'
    | 'other'
  publicSummary: string | null
  internalDetail: string
}

export function useStartGameRound() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const startMutation = useMutation({
    mutationFn: (input: { roundId: string; expectedRoundVersion: number }) =>
      prepareGameRound(input.roundId, { expectedRoundVersion: input.expectedRoundVersion }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelStartSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelStartFailed'))
    },
  })

  const beginGameplayMutation = useMutation({
    mutationFn: (input: { roundId: string; expectedRoundVersion: number }) =>
      beginGameRoundGameplay(input.roundId, {
        expectedRoundVersion: input.expectedRoundVersion,
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelBeginGameplaySuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelBeginGameplayFailed'))
    },
  })

  const reviewMutation = useMutation({
    mutationFn: (input: { roundId: string; expectedRoundVersion: number }) =>
      reviewGameRound(input.roundId, { expectedRoundVersion: input.expectedRoundVersion }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelReviewSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelReviewFailed'))
    },
  })

  const rebuildMutation = useMutation({
    mutationFn: (input: { roundId: string; expectedRoundVersion: number }) =>
      rebuildGameRound(input.roundId, { expectedRoundVersion: input.expectedRoundVersion }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelRebuildSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelRebuildFailed'))
    },
  })

  const technicalCancelMutation = useMutation({
    mutationFn: (input: TechnicalCancelRoundInput) =>
      technicalCancelGameRound(input.roundId, {
        expectedRoundVersion: input.expectedRoundVersion,
        reasonCode: input.reasonCode,
        publicSummary: input.publicSummary,
        internalDetail: input.internalDetail,
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.roundPanelTechnicalCancelSuccess'))
      await invalidateRoundState(queryClient)
    },
    onError: () => {
      setToastMessage(t('gameBoard.roundPanelTechnicalCancelFailed'))
    },
  })

  const completeMutation = useMutation({
    mutationFn: (input: CompleteRoundInput) =>
      finalizeGameRound(input.roundId, {
        status: 'completed',
        killsCount: input.killsCount,
        bountyCount: input.bountyCount,
        notes: input.notes,
        modifierResults: input.modifierResults,
        ruleGroups: input.ruleGroups,
        expectedRoundVersion: input.expectedRoundVersion,
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
    startMutation.isPending ||
    beginGameplayMutation.isPending ||
    reviewMutation.isPending ||
    rebuildMutation.isPending ||
    technicalCancelMutation.isPending ||
    completeMutation.isPending

  return {
    isChangingRoundStage: isMutating,
    startRound: startMutation.mutate,
    beginGameplay: beginGameplayMutation.mutate,
    reviewRound: reviewMutation.mutate,
    rebuildRound: rebuildMutation.mutate,
    technicalCancelRound: technicalCancelMutation.mutate,
    completeRound: completeMutation.mutateAsync,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
