import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { TFunction } from 'i18next'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { hasPanelCapability } from '../../shared/auth/panel-capabilities.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { openGameBoardCell } from './api/game-board-data-access.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'

function getOpenCellErrorMessage(error: unknown, t: TFunction<'translation'>) {
  if (error instanceof ApiError) {
    if (
      error.status === 409 &&
      typeof error.details === 'object' &&
      error.details !== null &&
      'code' in error.details &&
      error.details.code === API_ERROR_CODES.gameBoardActiveTeamRequired
    ) {
      return t('gameBoard.openActiveTeamRequired')
    }

    if (error.status === 403) {
      return t('gameBoard.openForbidden')
    }

    if (error.status === 404) {
      return t('gameBoard.openNotFound')
    }
  }

  return t('gameBoard.openFailed')
}

interface UseOpenGameBoardCellOptions {
  activeTeamId?: string | null
  gameStatus?: string | null
}

export function useOpenGameBoardCell({
  activeTeamId,
  gameStatus,
}: UseOpenGameBoardCellOptions = {}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [pendingCell, setPendingCell] = useState<GameBoardCell | null>(null)
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const canOpenCells = useMemo(
    () => hasPanelCapability('openGameBoardCell', user?.roles),
    [user?.roles],
  )
  const hasSelectedActiveTeam = gameStatus !== 'active' || activeTeamId != null

  const openCellMutation = useMutation({
    mutationFn: (cellId: string) => openGameBoardCell(cellId),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.openSuccess'))
      await queryClient.invalidateQueries({
        queryKey: currentGameBoardQueryOptions.queryKey,
      })
    },
    onError: (error) => {
      setToastMessage(getOpenCellErrorMessage(error, t))
    },
    onSettled: () => {
      setPendingCell(null)
    },
  })

  const requestOpenCell = (cell: GameBoardCell) => {
    if (!canOpenCells || openCellMutation.isPending) {
      return
    }

    if (!hasSelectedActiveTeam) {
      setToastMessage(t('gameBoard.openActiveTeamRequired'))
      return
    }

    setPendingCell(cell)
  }

  const confirmOpenCell = () => {
    if (!pendingCell) {
      return
    }

    openCellMutation.mutate(pendingCell.id)
  }

  return {
    pendingCell,
    toastMessage,
    canOpenCells: canOpenCells && hasSelectedActiveTeam && !openCellMutation.isPending,
    isSubmitting: openCellMutation.isPending,
    requestOpenCell,
    confirmOpenCell,
    dismissPendingCell: () => setPendingCell(null),
    dismissToast: () => setToastMessage(null),
  }
}
