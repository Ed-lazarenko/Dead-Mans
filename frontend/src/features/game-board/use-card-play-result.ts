import { useQuery } from '@tanstack/react-query'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { gameHistoryGameDetailsQueryOptions } from '../game-history/api/game-history-queries.ts'
import {
  findLatestGameBoardCellPlayResult,
  type GameBoardCellPlayResultRound,
} from './model/game-board-cell-results.ts'

export type GameBoardCardPlayResultRound = GameBoardCellPlayResultRound

export function useCardPlayResult(gameId: string | null, cell: GameBoardCell | null) {
  const isEnabled = gameId !== null && cell !== null && cell.state === 'open'
  const gameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(gameId ?? ''),
    enabled: isEnabled,
  })
  const round =
    isEnabled && gameDetailsQuery.data
      ? findLatestGameBoardCellPlayResult(gameDetailsQuery.data.mainGame.rounds, cell.id)
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
  return findLatestGameBoardCellPlayResult(rounds, cellId)
}
