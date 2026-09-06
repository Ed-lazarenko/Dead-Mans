import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { gameRegistrationQueryKeys } from '../game-registration/api/game-registration-queries.ts'
import { gameHistoryQueryKeys } from '../game-history/api/game-history-queries.ts'
import { gameModifierQueryKeys } from '../game-modifiers/api/game-modifier-queries.ts'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import { finishGame } from './api/game-finish-api.ts'
import { gameFinishQueryKeys } from './api/game-finish-queries.ts'
import { gameBoardQueryKeys } from './api/game-board-queries.ts'

export function useGameFinish() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [toastMessage, setToastMessage] = useState('')
  const mutation = useMutation({
    mutationFn: ({ gameId, ...input }: Parameters<typeof finishGame>[1] & { gameId: string }) =>
      finishGame(gameId, input),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.finishSuccess'))
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: gameBoardQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey }),
        queryClient.invalidateQueries({ queryKey: gameRegistrationQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: gameHistoryQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all }),
        queryClient.invalidateQueries({ queryKey: gameFinishQueryKeys.all }),
      ])
    },
  })

  return {
    finishGame: mutation.mutateAsync,
    isFinishing: mutation.isPending,
    error: mutation.error,
    resetError: mutation.reset,
    toastMessage,
    dismissToast: () => setToastMessage(''),
  }
}
