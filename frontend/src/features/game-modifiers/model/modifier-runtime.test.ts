import { describe, expect, it } from 'vitest'
import type { components } from '../../../shared/api/contracts/generated'
import {
  buildModifierRuntimeUnits,
  calculateModifierRuntimeClock,
  createServerClockOffset,
  formatRuntimeDuration,
} from './modifier-runtime.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

describe('modifier runtime', () => {
  it.each([
    ['Чирик', 60, 120],
    ['Проказник', 300, 600],
  ])(
    'aggregates two %s activations into one monitored runtime unit',
    (modifierName, durationSecondsPerActivation, expectedDurationSeconds) => {
      const round = createRound({
        modifierResults: [
          createResult('result-1', modifierName, durationSecondsPerActivation),
          createResult('result-2', modifierName, durationSecondsPerActivation),
        ],
      })

      expect(buildModifierRuntimeUnits(round)).toEqual([
        expect.objectContaining({
          modifierName,
          activationCount: 2,
          durationSeconds: expectedDurationSeconds,
          requiresHostMonitoring: true,
        }),
      ])
    },
  )

  it('restores countdown from server clock and clamps an expired timer to zero', () => {
    const round = createRound({
      status: 'in_progress',
      gameplayStartedAtUtc: '2026-08-20T10:00:00.000Z',
    })
    const running = calculateModifierRuntimeClock(
      round,
      120,
      Date.parse('2026-08-20T10:01:15.000Z'),
    )
    const expired = calculateModifierRuntimeClock(
      round,
      120,
      Date.parse('2026-08-20T10:03:00.000Z'),
    )

    expect(running).toEqual({ state: 'running', remainingSeconds: 45 })
    expect(expired).toEqual({ state: 'expired', remainingSeconds: 0 })
    expect(formatRuntimeDuration(expired.remainingSeconds ?? -1)).toBe('0:00')
  })

  it('freezes in review and resumes the original gameplay timeline', () => {
    const review = createRound({
      status: 'reviewing_results',
      gameplayStartedAtUtc: '2026-08-20T10:00:00.000Z',
      reviewedAtUtc: '2026-08-20T10:01:00.000Z',
    })
    expect(calculateModifierRuntimeClock(review, 120, Date.parse('2026-08-20T10:05:00Z'))).toEqual({
      state: 'stopped',
      remainingSeconds: 60,
    })

    const resumed = { ...review, status: 'in_progress' as const }
    expect(calculateModifierRuntimeClock(resumed, 120, Date.parse('2026-08-20T10:01:30Z'))).toEqual(
      {
        state: 'running',
        remainingSeconds: 30,
      },
    )
  })

  it('derives a stable server clock offset at projection receipt', () => {
    expect(
      createServerClockOffset('2026-08-20T10:00:05.000Z', Date.parse('2026-08-20T10:00:00Z')),
    ).toBe(5_000)
  })
})

function createRound(overrides: Partial<GameRoundDetails> = {}): GameRoundDetails {
  return {
    roundId: 'round-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    status: 'preparing',
    roundVersion: 1,
    startedAtUtc: '2026-08-20T09:59:00Z',
    baseScore: 100,
    emptyCardPenaltyApplied: false,
    scoreDetails: {
      scoreUnit: 100,
      killsScore: 0,
      bountyScore: 0,
      modifierKillDelta: 0,
      modifierKillScore: 0,
      modifierScoreDelta: 0,
      emptyCardPenaltyApplied: false,
      emptyCardPenaltyScore: 0,
      penaltyTotal: 0,
      bonusDelta: 0,
      totalKillCount: 0,
      finalScore: 0,
    },
    killsCount: 0,
    bountyCount: 0,
    serverNowUtc: '2026-08-20T10:00:00Z',
    participants: [],
    modifierResults: [],
    ...overrides,
  }
}

function createResult(
  modifierResultId: string,
  modifierName = 'Проказник',
  durationSecondsPerActivation = 300,
): GameRoundDetails['modifierResults'][number] {
  return {
    modifierResultId,
    modifierId: modifierName.toLocaleLowerCase('ru'),
    modifierName,
    modifierDescription: 'Use decoys for the whole window.',
    modifierCategory: 'round',
    outcomeStatus: 'pending',
    scoreDelta: 0,
    killDelta: 0,
    activationId: `activation-${modifierResultId}`,
    resolutionGroupId: 'runtime-group',
    resolutionKind: 'ruleStatus',
    runtimeBehavior: {
      phase: 'round',
      performer: 'activeTeam',
      requiresHostMonitoring: true,
      rule: 'Use decoys for the whole window.',
      stackingPolicy: 'aggregateParameters',
      durationSecondsPerActivation,
    },
  }
}
