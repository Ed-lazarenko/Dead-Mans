import type { TFunction } from 'i18next'
import type { Theme } from '@mui/material/styles'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'

type GameHistoryCardLabelInput = {
  cellTitle?: string | null
  cellCost: number
  cellRowIndex: number
  cellColIndex: number
}

export function formatCardLabel(round: GameHistoryCardLabelInput, t: TFunction) {
  const title = round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${t('gameHistory.cardCostLabel', { cost: round.cellCost })} · ${t(
    'gameHistory.cardCoordinate',
    {
      row: round.cellRowIndex + 1,
      col: round.cellColIndex + 1,
    },
  )}`
}

export function formatShortCardLabel(round: GameHistoryCardLabelInput, t: TFunction) {
  const title = round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${round.cellCost}`
}

export function formatHistoryTeamName(
  t: TFunction,
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(teamName, t('gameHistory.teamLabel', { slot: teamSlotIndex }))
}

export function getRankColor(theme: Theme, rank: number) {
  if (rank === 1) {
    return theme.palette.warning.main
  }

  if (rank === 2) {
    return theme.palette.grey[500]
  }

  if (rank === 3) {
    return theme.palette.secondary.main
  }

  return theme.palette.primary.main
}
