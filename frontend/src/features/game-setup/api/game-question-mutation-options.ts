import { mutationOptions, type QueryClient } from '@tanstack/react-query'
import type {
  CreateGameQuestionRequest,
  UpdateGameQuestionRequest,
} from '../../../shared/api/contracts/index.ts'
import { createGameQuestion, deleteGameQuestion, updateGameQuestion } from './game-questions-api.ts'
import { gameQuestionQueryKeys } from './game-question-queries.ts'

function invalidateGameQuestions(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: gameQuestionQueryKeys.all })
}

export function createGameQuestionMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: (request: CreateGameQuestionRequest) => createGameQuestion(request),
    onSuccess: () => invalidateGameQuestions(queryClient),
  })
}

export function updateGameQuestionMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: ({
      questionId,
      request,
    }: {
      questionId: string
      request: UpdateGameQuestionRequest
    }) => updateGameQuestion(questionId, request),
    onSuccess: () => invalidateGameQuestions(queryClient),
  })
}

export function deleteGameQuestionMutationOptions(queryClient: QueryClient) {
  return mutationOptions({
    mutationFn: (questionId: string) => deleteGameQuestion(questionId),
    onSuccess: () => invalidateGameQuestions(queryClient),
  })
}
