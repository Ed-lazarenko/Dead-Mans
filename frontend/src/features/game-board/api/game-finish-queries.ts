import { queryOptions } from '@tanstack/react-query'
import { fetchGameFinishPreview } from './game-finish-api.ts'

export const gameFinishQueryKeys = {
  all: ['gameFinish'] as const,
  preview: (gameId: string) => [...gameFinishQueryKeys.all, gameId, 'preview'] as const,
}

export const gameFinishPreviewQueryOptions = (gameId: string) =>
  queryOptions({
    queryKey: gameFinishQueryKeys.preview(gameId),
    queryFn: () => fetchGameFinishPreview(gameId),
    staleTime: 0,
    retry: false,
  })
