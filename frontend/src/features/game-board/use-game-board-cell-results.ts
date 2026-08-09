import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { gameHistoryGameDetailsQueryOptions } from '../game-history/api/game-history-queries.ts'
import {
  buildGameBoardCellPlayResultMap,
  type GameBoardCellPlayResult,
} from './model/game-board-cell-results.ts'

export function useGameBoardCellResults(gameId: string | null, cells: readonly GameBoardCell[]) {
  const openCellIds = useMemo(
    () => new Set(cells.filter((cell) => cell.state === 'open').map((cell) => cell.id)),
    [cells],
  )
  const gameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(gameId ?? ''),
    enabled: gameId !== null && openCellIds.size > 0,
  })

  const playResultsByCellId = useMemo(() => {
    if (!gameDetailsQuery.data) {
      return new Map<string, GameBoardCellPlayResult>()
    }

    return buildGameBoardCellPlayResultMap(gameDetailsQuery.data.mainGame.rounds, openCellIds)
  }, [gameDetailsQuery.data, openCellIds])

  return {
    playResultsByCellId,
    isLoading: gameDetailsQuery.isLoading,
    isError: gameDetailsQuery.isError,
  }
}
