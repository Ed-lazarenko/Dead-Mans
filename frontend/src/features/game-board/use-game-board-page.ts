import { useQueries } from '@tanstack/react-query'
import { activeGameCardRunQueryOptions } from '../game-card-runs/api/game-card-runs-queries.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'

export function useGameBoardPage() {
  const [snapshotQuery, activeRunQuery] = useQueries({
    queries: [currentGameBoardQueryOptions, activeGameCardRunQueryOptions],
  })

  return {
    data: snapshotQuery.data,
    activeRun: activeRunQuery.data ?? null,
    isLoading: snapshotQuery.isLoading || activeRunQuery.isLoading,
    isError: snapshotQuery.isError || activeRunQuery.isError,
  }
}
