import type { components } from '../../../shared/api/contracts/generated'

type QuizRound = components['schemas']['GameHistoryQuizRoundItemDto']

type QuizRoundParticipantDetails = {
  answeredByDisplayName: string | null
  answeredForDisplayName: string | null
}

export function getQuizRoundParticipantDetails(round: QuizRound): QuizRoundParticipantDetails {
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
