import { useQuery } from '@tanstack/react-query'
import { manualGameQuestionAwardPlayersQueryOptions } from './api/game-board-queries.ts'

export function useManualQuizAwardPlayers(isEnabled: boolean) {
  const query = useQuery({
    ...manualGameQuestionAwardPlayersQueryOptions,
    enabled: isEnabled,
  })

  return {
    players: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
  }
}
