import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../shared/api/query-keys.ts'
import { fetchCurrentGameBoardSnapshot } from './api/game-board-data-access.ts'

export function useGameBoardPage() {
  return useQuery({
    queryKey: queryKeys.gameBoard.currentSnapshot(),
    queryFn: fetchCurrentGameBoardSnapshot,
  })
}
