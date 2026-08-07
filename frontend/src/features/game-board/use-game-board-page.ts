import { useQueries } from '@tanstack/react-query'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import {
  currentGameBoardQueryOptions,
  currentGameTeamQueueQueryOptions,
} from './api/game-board-queries.ts'

export function useGameBoardPage() {
  const [snapshotQuery, activeRoundQuery, teamQueueQuery] = useQueries({
    queries: [
      currentGameBoardQueryOptions,
      activeGameRoundQueryOptions,
      currentGameTeamQueueQueryOptions,
    ],
  })

  return {
    data: snapshotQuery.data,
    activeRound: activeRoundQuery.data ?? null,
    teamQueue: teamQueueQuery.data ?? [],
    isTeamQueueLoading: teamQueueQuery.isLoading,
    isTeamQueueError: teamQueueQuery.isError,
    isLoading: snapshotQuery.isLoading || activeRoundQuery.isLoading,
    isError: snapshotQuery.isError || activeRoundQuery.isError,
  }
}
