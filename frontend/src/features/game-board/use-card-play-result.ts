import { useQuery } from '@tanstack/react-query'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import type { components } from '../../shared/api/contracts/generated'
import { gameHistoryGameDetailsQueryOptions } from '../game-history/api/game-history-queries.ts'

export type GameBoardCardPlayResultRound = components['schemas']['GameHistoryRoundItemDto']

export function useCardPlayResult(gameId: string | null, cell: GameBoardCell | null) {
  const isEnabled = gameId !== null && cell !== null && cell.state === 'open'
  const gameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(gameId ?? ''),
    enabled: isEnabled,
  })
  const round =
    isEnabled && gameDetailsQuery.data
      ? findLatestCardPlayResultRound(gameDetailsQuery.data.mainGame.rounds, cell.id)
      : null

  return {
    round,
    isLoading: isEnabled && gameDetailsQuery.isLoading,
    isError: isEnabled && gameDetailsQuery.isError,
  }
}

export function findLatestCardPlayResultRound(
  rounds: readonly GameBoardCardPlayResultRound[],
  cellId: string,
) {
  let latestRound: GameBoardCardPlayResultRound | null = null
  let latestTime = Number.NEGATIVE_INFINITY

  for (const round of rounds) {
    if (round.cellId !== cellId || !isCompletedCardPlayResultRound(round)) {
      continue
    }

    const roundTime = getCardPlayResultRoundTime(round)
    if (latestRound === null || roundTime > latestTime) {
      latestRound = round
      latestTime = roundTime
    }
  }

  return latestRound
}

function isCompletedCardPlayResultRound(round: GameBoardCardPlayResultRound) {
  return (
    round.finishedAtUtc !== null &&
    round.finishedAtUtc !== undefined &&
    (round.status === 'completed' || round.status === 'cancelled')
  )
}

function getCardPlayResultRoundTime(round: GameBoardCardPlayResultRound) {
  const timestamp = round.finishedAtUtc
  const time = Date.parse(timestamp)
  return Number.isNaN(time) ? 0 : time
}
