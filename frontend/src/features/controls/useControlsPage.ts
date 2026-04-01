import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import {
  getGameControlState,
  nextRound,
  pauseGame,
  resetGame,
  resumeGame,
  startGame,
} from './api/controlsDataAccess.ts'

function useControlActionMutation(action: () => Promise<unknown>) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: action,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.controls.all })
    },
  })
}

export function useControlsPage() {
  const query = useQuery({
    queryKey: queryKeys.controls.state(),
    queryFn: getGameControlState,
  })

  const startMutation = useControlActionMutation(startGame)
  const pauseMutation = useControlActionMutation(pauseGame)
  const resumeMutation = useControlActionMutation(resumeGame)
  const nextRoundMutation = useControlActionMutation(nextRound)
  const resetMutation = useControlActionMutation(resetGame)

  const isBusy =
    startMutation.isPending ||
    pauseMutation.isPending ||
    resumeMutation.isPending ||
    nextRoundMutation.isPending ||
    resetMutation.isPending

  const actionAvailability = {
    canStart: !isBusy && query.data?.phase !== 'running',
    canPause: !isBusy && query.data?.phase === 'running',
    canResume: !isBusy && query.data?.phase === 'paused',
    canNextRound: !isBusy && query.data?.phase !== 'idle',
    canReset: !isBusy,
  }

  return {
    ...query,
    startMutation,
    pauseMutation,
    resumeMutation,
    nextRoundMutation,
    resetMutation,
    isBusy,
    actionAvailability,
  }
}


