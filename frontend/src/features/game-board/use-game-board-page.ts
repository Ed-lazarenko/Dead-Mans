import { useQueries } from '@tanstack/react-query'
import { activeGameCardRunQueryOptions } from '../game-card-runs/api/game-card-runs-queries.ts'
import {
  currentGameBoardQueryOptions,
  currentGameTeamQueueQueryOptions,
} from './api/game-board-queries.ts'

export function useGameBoardPage() {
  const [snapshotQuery, activeRunQuery, teamQueueQuery] = useQueries({
    queries: [
      currentGameBoardQueryOptions,
      activeGameCardRunQueryOptions,
      currentGameTeamQueueQueryOptions,
    ],
  })

  return {
    data: snapshotQuery.data,
    activeRun: activeRunQuery.data ?? null,
    teamQueue: teamQueueQuery.data ?? [],
    isTeamQueueLoading: teamQueueQuery.isLoading,
    isTeamQueueError: teamQueueQuery.isError,
    isLoading: snapshotQuery.isLoading || activeRunQuery.isLoading,
    isError: snapshotQuery.isError || activeRunQuery.isError,
  }
}
