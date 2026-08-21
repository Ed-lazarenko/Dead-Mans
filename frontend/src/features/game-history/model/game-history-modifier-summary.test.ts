import { describe, expect, it } from 'vitest'
import type { components } from '../../../shared/api/contracts/generated'
import { buildGameHistoryModifierSummary } from './game-history-modifier-summary.ts'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

describe('game history modifier summary', () => {
  it('groups frozen modifier revisions separately and excludes cancelled rounds', () => {
    const completed = createRound('completed', [
      createModifier({ definitionRevision: 1, scoreDelta: 10, killDelta: 1 }),
      createModifier({
        modifierResultId: 'result-2',
        activationId: 'activation-2',
        definitionRevision: 2,
        scoreDelta: 20,
      }),
    ])
    const cancelled = createRound('cancelled', [
      createModifier({
        modifierResultId: 'cancelled-result',
        activationId: 'cancelled-activation',
        definitionRevision: 1,
        scoreDelta: 999,
      }),
    ])

    expect(buildGameHistoryModifierSummary([completed, cancelled])).toEqual([
      expect.objectContaining({
        definitionRevision: 1,
        activationCount: 1,
        pointsDelta: 10,
        bonusKillsDelta: 1,
      }),
      expect.objectContaining({
        definitionRevision: 2,
        activationCount: 1,
        pointsDelta: 20,
        bonusKillsDelta: 0,
      }),
    ])
  })
})

function createRound(
  status: GameHistoryRound['status'],
  modifiers: GameHistoryRound['modifiers'],
): GameHistoryRound {
  return {
    roundId: `round-${status}`,
    teamId: 'team-1',
    teamSlotIndex: 1,
    status,
    roundVersion: 1,
    startedAtUtc: '2026-08-20T10:00:00Z',
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
    cellId: 'cell-1',
    cellRowIndex: 0,
    cellColIndex: 0,
    cellType: 'question',
    cellCost: 100,
    purchasesRefunded: status === 'cancelled',
    cellMedia: [],
    participants: [],
    modifiers,
  }
}

function createModifier(
  overrides: Partial<GameHistoryRound['modifiers'][number]> = {},
): GameHistoryRound['modifiers'][number] {
  return {
    modifierResultId: 'result-1',
    modifierId: 'modifier-1',
    modifierName: 'Frozen modifier',
    modifierDescription: 'Frozen description',
    modifierCategory: 'round',
    outcomeStatus: 'completed',
    scoreDelta: 0,
    killDelta: 0,
    activationId: 'activation-1',
    ...overrides,
  }
}
