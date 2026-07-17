import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { setActiveGameTeam } from './api/game-board-data-access.ts'
import {
  currentGameBoardQueryOptions,
  currentGameTeamQueueQueryOptions,
} from './api/game-board-queries.ts'

export function useActiveGameTeam() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: setActiveGameTeam,
    onSuccess: async () => {
      setToastMessage(t('gameBoard.activeTeamSaved'))
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: currentGameBoardQueryOptions.queryKey,
        }),
        queryClient.invalidateQueries({
          queryKey: currentGameTeamQueueQueryOptions.queryKey,
        }),
      ])
    },
    onError: () => {
      setToastMessage(t('gameBoard.activeTeamSaveFailed'))
    },
  })

  return {
    isSelectingActiveTeam: mutation.isPending,
    selectActiveTeam: mutation.mutate,
    toastMessage,
    dismissToast: () => setToastMessage(null),
  }
}
