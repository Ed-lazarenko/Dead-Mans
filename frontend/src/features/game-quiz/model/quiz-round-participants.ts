import type { components } from '../../../shared/api/contracts/generated'

type QuestionRound = components['schemas']['GameHistoryQuizRoundItemDto']

export type QuizRoundParticipantDetails = {
  answeredByDisplayName: string | null
  answeredForDisplayName: string | null
}

export function getQuizRoundParticipantDetails(round: QuestionRound): QuizRoundParticipantDetails {
  const answeredByDisplayName = round.answeredByDisplayName ?? null
  const answeredForDisplayName =
    round.answeredForDisplayName != null &&
    round.answeredForUserId != null &&
    round.answeredForUserId !== round.answeredByUserId
      ? round.answeredForDisplayName
      : null

  return {
    answeredByDisplayName,
    answeredForDisplayName,
  }
}
