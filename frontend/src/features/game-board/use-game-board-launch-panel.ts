import { useQuery, useQueryClient } from '@tanstack/react-query'
import { hasPanelCapability } from '../../shared/auth/panel-capabilities.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import {
  gameRegistrationAdminSnapshotQueryOptions,
  useGameRegistrationToast,
  useStartGameFromRegistrationMutation,
} from '../game-registration/index.ts'
import { currentGameBoardQueryOptions } from './api/game-board-queries.ts'

export function useGameBoardLaunchPanel(gameStatus: string) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const { toastMessage, onMutationError, dismissToast } = useGameRegistrationToast()
  const canManageGame = hasPanelCapability('manageGame', user?.roles)
  const canStartGame = hasPanelCapability('startGame', user?.roles)
  const canFinishGame = hasPanelCapability('finishGame', user?.roles)
  const shouldLoadLaunchState = canStartGame && gameStatus === 'ready'
  const adminSnapshotQuery = useQuery({
    ...gameRegistrationAdminSnapshotQueryOptions,
    enabled: shouldLoadLaunchState,
  })
  const startGameMutation = useStartGameFromRegistrationMutation(onMutationError)

  return {
    canManageGame,
    canStartGame,
    canFinishGame,
    shouldRender: shouldLoadLaunchState && adminSnapshotQuery.data != null,
    snapshot: adminSnapshotQuery.data,
    isLoadingLaunchState: shouldLoadLaunchState && adminSnapshotQuery.isLoading,
    isStartingGame: startGameMutation.isPending,
    startGame: () =>
      startGameMutation.mutate(undefined, {
        onSuccess: () =>
          queryClient.invalidateQueries({
            queryKey: currentGameBoardQueryOptions.queryKey,
          }),
      }),
    toastMessage,
    dismissToast,
  }
}
