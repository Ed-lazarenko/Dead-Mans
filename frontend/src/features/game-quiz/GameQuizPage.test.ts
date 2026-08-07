import { describe, expect, it } from 'vitest'
import type { components } from '../../shared/api/contracts/generated'
import { getQuizRoundParticipantDetails } from './model/quiz-round-participants.ts'

type QuizRound = components['schemas']['GameHistoryQuizRoundItemDto']

function createRound(overrides: Partial<QuizRound> = {}): QuizRound {
  return {
    roundId: 'round-1',
    questionId: 'question-1',
    questionCode: 'quiz-001',
    questionText: 'Question text',
    categoryName: 'General',
    reward: 50,
    status: 'answered_correct',
    askedAtUtc: '2026-07-22T04:00:00Z',
    answeredAtUtc: '2026-07-22T04:01:00Z',
    answeredByDisplayName: 'Moderator',
    answeredByUserId: 'user-moderator',
    answeredForUserId: 'user-player',
    answeredForDisplayName: 'Player One',
    submittedAnswer: 'Correct answer',
    isCorrect: true,
    awardedPoints: 50,
    ...overrides,
  }
}

describe('getQuizRoundParticipantDetails', () => {
  it('keeps both the answer author and the credited player when they differ', () => {
    expect(getQuizRoundParticipantDetails(createRound())).toEqual({
      answeredByDisplayName: 'Moderator',
      answeredForDisplayName: 'Player One',
    })
  })

  it('hides the credited player line when the answer is credited to the same person', () => {
    expect(
      getQuizRoundParticipantDetails(
        createRound({
          answeredByDisplayName: 'Player One',
          answeredByUserId: 'user-player',
        }),
      ),
    ).toEqual({
      answeredByDisplayName: 'Player One',
      answeredForDisplayName: null,
    })
  })
})
