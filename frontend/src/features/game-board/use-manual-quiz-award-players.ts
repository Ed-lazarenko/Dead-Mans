import { useQuery } from '@tanstack/react-query'
import { manualGameQuizAwardPlayersQueryOptions } from './api/game-board-queries.ts'

export function useManualQuizAwardPlayers(isEnabled: boolean) {
  const query = useQuery({
    ...manualGameQuizAwardPlayersQueryOptions,
    enabled: isEnabled,
  })

  return {
    players: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
  }
}
