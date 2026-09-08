import type { TFunction } from 'i18next'
import type { components } from '../../../shared/api/contracts/generated'

type GameHistoryGameSummary = components['schemas']['GameHistoryGameSummaryDto']
type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function formatSignedNumber(value: number) {
  return value > 0 ? `+${value}` : `${value}`
}

export function formatGameTimeLabel(
  game: Pick<GameHistoryGameSummary, 'startedAtUtc' | 'finishedAtUtc' | 'createdAtUtc'>,
  t: TFunction,
  locale?: string,
) {
  if (game.finishedAtUtc) {
    return t('gameHistory.gameTimeFinished', {
      date: formatDateTime(game.finishedAtUtc, locale),
    })
  }

  if (game.startedAtUtc) {
    return t('gameHistory.gameTimeStarted', {
      date: formatDateTime(game.startedAtUtc, locale),
    })
  }

  return t('gameHistory.gameTimeCreated', {
    date: formatDateTime(game.createdAtUtc, locale),
  })
}

export function formatDateTime(value: string, locale?: string) {
  return new Date(value).toLocaleString(locale)
}

export function formatOptionalDateTime(
  value: string | null | undefined,
  t: TFunction,
  locale?: string,
) {
  return value ? formatDateTime(value, locale) : t('gameHistory.notAvailable')
}

export function normalizeStatus(status: string) {
  return status.toLowerCase()
}

export function normalizeRoundStatus(status: string) {
  return status.toLowerCase().replace(/\s+/g, '_')
}

export function isCountedRound(round: GameHistoryRound) {
  return normalizeRoundStatus(round.status) === 'completed'
}

export function getGameStatusColor(status: string): 'default' | 'success' | 'warning' | 'info' {
  switch (normalizeStatus(status)) {
    case 'finished':
      return 'success'
    case 'active':
      return 'warning'
    case 'ready':
      return 'info'
    default:
      return 'default'
  }
}

export function getRoundStatusColor(status: string): 'default' | 'success' | 'warning' | 'error' {
  switch (normalizeRoundStatus(status)) {
    case 'completed':
      return 'success'
    case 'cancelled':
    case 'failed':
      return 'error'
    case 'review':
      return 'warning'
    default:
      return 'default'
  }
}
