import type { TFunction } from 'i18next'
import type { Theme } from '@mui/material/styles'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'

type GameHistoryCardLabelInput = {
  cellTitle?: string | null
  cellCost: number
}

export function formatCardLabel(round: GameHistoryCardLabelInput, t: TFunction) {
  return round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
}

export function formatShortCardLabel(round: GameHistoryCardLabelInput, t: TFunction) {
  const title = round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${t('gameHistory.cardCostLabel', { cost: round.cellCost })}`
}

export function formatHistoryTeamName(
  t: TFunction,
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(teamName, t('common.teamWithSlot', { slot: teamSlotIndex }))
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
