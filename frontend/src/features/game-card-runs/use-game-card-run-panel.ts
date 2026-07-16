import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { hasPanelCapability } from '../../shared/auth/panel-capabilities.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { currentGameBoardQueryOptions } from '../game-board/api/game-board-queries.ts'
import { finalizeGameCardRun, startGameCardRun } from './api/game-card-runs-api.ts'
import {
  activeGameCardRunQueryOptions,
  gameCardRunEligibleTeamsQueryOptions,
} from './api/game-card-runs-queries.ts'

function getRuntimeErrorMessage(error: unknown, fallback: string) {
  if (
    error instanceof ApiError &&
    (error.status === 400 || error.status === 404 || error.status === 409)
  ) {
    return fallback
  }

  return fallback
}

export function useGameCardRunPanel(openCellOptions: readonly { id: string; label: string }[]) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const canManageCardRuns = hasPanelCapability('manageGameCardRuns', user?.roles)

  const eligibleTeamsQuery = useQuery({
    ...gameCardRunEligibleTeamsQueryOptions,
    enabled: canManageCardRuns,
  })

  const [selectedCellId, setSelectedCellId] = useState(openCellOptions[0]?.id ?? '')
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [finalStatus, setFinalStatus] = useState<'completed' | 'cancelled'>('completed')
  const [finalScoreInput, setFinalScoreInput] = useState('')
  const [notes, setNotes] = useState('')
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const resolvedSelectedCellId =
    selectedCellId !== '' ? selectedCellId : (openCellOptions[0]?.id ?? '')
  const resolvedSelectedTeamId =
    selectedTeamId !== '' ? selectedTeamId : (eligibleTeamsQuery.data?.[0]?.teamId ?? '')

  const eligibleTeamOptions = useMemo(
    () =>
      (eligibleTeamsQuery.data ?? []).map((team) => ({
        value: team.teamId,
        label: `#${team.teamSlotIndex} - ${team.participants.map((participant) => participant.displayName).join(', ')}`,
      })),
    [eligibleTeamsQuery.data],
  )

  const startMutation = useMutation({
    mutationFn: () =>
      startGameCardRun({
        cellId: resolvedSelectedCellId,
        teamId: resolvedSelectedTeamId,
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.runStarted'))
      setNotes('')
      setFinalScoreInput('')
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey }),
        queryClient.invalidateQueries({ queryKey: activeGameCardRunQueryOptions.queryKey }),
        queryClient.invalidateQueries({ queryKey: gameCardRunEligibleTeamsQueryOptions.queryKey }),
      ])
    },
    onError: (error) => {
      setToastMessage(getRuntimeErrorMessage(error, t('gameBoard.runStartFailed')))
    },
  })

  const finalizeMutation = useMutation({
    mutationFn: (cardRunId: string) =>
      finalizeGameCardRun(cardRunId, {
        status: finalStatus,
        ...(finalScoreInput.trim() !== '' ? { finalScore: Number(finalScoreInput) } : {}),
        ...(notes.trim() !== '' ? { notes: notes.trim() } : {}),
      }),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.runFinalized'))
      setNotes('')
      setFinalScoreInput('')
      setFinalStatus('completed')
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey }),
        queryClient.invalidateQueries({ queryKey: activeGameCardRunQueryOptions.queryKey }),
        queryClient.invalidateQueries({ queryKey: gameCardRunEligibleTeamsQueryOptions.queryKey }),
      ])
    },
    onError: (error) => {
      setToastMessage(getRuntimeErrorMessage(error, t('gameBoard.runFinalizeFailed')))
    },
  })

  return {
    canManageCardRuns,
    eligibleTeamsQuery,
    eligibleTeamOptions,
    selectedCellId,
    selectedTeamId,
    resolvedSelectedCellId,
    resolvedSelectedTeamId,
    finalStatus,
    finalScoreInput,
    notes,
    toastMessage,
    setSelectedCellId,
    setSelectedTeamId,
    setFinalStatus,
    setFinalScoreInput,
    setNotes,
    dismissToast: () => setToastMessage(null),
    startRun: () => startMutation.mutate(),
    finalizeRun: (cardRunId: string) => finalizeMutation.mutate(cardRunId),
    isStarting: startMutation.isPending,
    isFinalizing: finalizeMutation.isPending,
  }
}
