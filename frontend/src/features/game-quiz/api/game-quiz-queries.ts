import { queryOptions } from '@tanstack/react-query'
import { fetchGameQuestionHistory } from './game-quiz-api.ts'

const gameQuizQueryKeys = {
  all: ['gameQuiz'] as const,
  questionHistory: (gameId: string) =>
    [...gameQuizQueryKeys.all, 'questionHistory', gameId] as const,
}

export const gameQuestionHistoryQueryOptions = (gameId: string) =>
  queryOptions({
    queryKey: gameQuizQueryKeys.questionHistory(gameId),
    queryFn: () => fetchGameQuestionHistory(gameId),
  })
