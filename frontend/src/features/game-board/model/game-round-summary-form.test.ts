import { describe, expect, it } from 'vitest'
import type { components } from '../../../shared/api/contracts/generated'
import {
  buildCompleteRoundInput,
  buildGameRoundSummaryDefaultValues,
  gameRoundSummaryFormSchema,
} from './game-round-summary-form.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type ModifierResult = GameRoundDetails['modifierResults'][number]

describe('game-round-summary-form', () => {
  it('builds one exact rule resolution unit with every group member', () => {
    const round = createRound({
      modifierResults: [
        createModifier({
          modifierResultId: 'rule-result-1',
          activationId: 'rule-activation-1',
          resolutionGroupId: 'rule-group-1',
          resolutionKind: 'ruleStatus',
        }),
        createModifier({
          modifierResultId: 'rule-result-2',
          activationId: 'rule-activation-2',
          resolutionGroupId: 'rule-group-1',
          resolutionKind: 'ruleStatus',
        }),
      ],
    })
    const defaults = buildGameRoundSummaryDefaultValues(round)
    defaults.notes = '  Confirmed by the host.  '

    expect(defaults.ruleGroups).toEqual([
      expect.objectContaining({
        resolutionGroupId: 'rule-group-1',
        memberResultIds: ['rule-result-1', 'rule-result-2'],
        memberActivationIds: ['rule-activation-1', 'rule-activation-2'],
        outcomeStatus: null,
      }),
    ])

    defaults.ruleGroups[0].outcomeStatus = 'violated'
    defaults.ruleGroups[0].violationComment = '  crossed the restricted area  '
    const payload = buildCompleteRoundInput(round, defaults)

    expect(payload.ruleGroups).toEqual([
      {
        resolutionGroupId: 'rule-group-1',
        memberResultIds: ['rule-result-1', 'rule-result-2'],
        outcomeStatus: 'violated',
        violationComment: 'crossed the restricted area',
      },
    ])
    expect(payload.expectedRoundVersion).toBe(7)
    expect(payload.notes).toBe('Confirmed by the host.')
  })

  it('keeps every Shot activation independent', () => {
    const round = createRound({
      modifierResults: [
        createModifier({
          modifierResultId: 'shot-result-1',
          activationId: 'shot-activation-1',
          modifierId: 'shot',
          modifierName: 'Shot',
          resolutionKind: 'boolean',
        }),
        createModifier({
          modifierResultId: 'shot-result-2',
          activationId: 'shot-activation-2',
          modifierId: 'shot',
          modifierName: 'Shot',
          resolutionKind: 'boolean',
        }),
      ],
    })
    const defaults = buildGameRoundSummaryDefaultValues(round)

    expect(
      defaults.scoringInstances.map(({ activationIndex, activationCount }) => ({
        activationIndex,
        activationCount,
      })),
    ).toEqual([
      { activationIndex: 1, activationCount: 2 },
      { activationIndex: 2, activationCount: 2 },
    ])

    defaults.scoringInstances[0].isConditionMet = true
    defaults.scoringInstances[1].isConditionMet = false
    expect(buildCompleteRoundInput(round, defaults).modifierResults).toEqual([
      expect.objectContaining({
        modifierResultId: 'shot-result-1',
        isConditionMet: true,
      }),
      expect.objectContaining({
        modifierResultId: 'shot-result-2',
        isConditionMet: false,
      }),
    ])
  })

  it('never sends automatic V2 activations as manual resolution input', () => {
    const round = createRound({
      modifierResults: [
        createModifier({
          modifierResultId: 'automatic-result-1',
          activationId: 'automatic-activation-1',
          resolutionKind: 'automaticRoundMetric',
        }),
      ],
    })
    const defaults = buildGameRoundSummaryDefaultValues(round)

    expect(defaults.automaticInstances).toHaveLength(1)
    expect(buildCompleteRoundInput(round, defaults).modifierResults).toEqual([])
  })

  it('requires every rule, boolean, and count input before preview', () => {
    const round = createRound({
      modifierResults: [
        createModifier({ resolutionKind: 'boolean' }),
        createModifier({
          modifierResultId: 'count-result',
          activationId: 'count-activation',
          resolutionKind: 'nonNegativeCount',
        }),
        createModifier({
          modifierResultId: 'rule-result',
          activationId: 'rule-activation',
          resolutionKind: 'ruleStatus',
          resolutionGroupId: 'rule-group',
        }),
      ],
    })
    const defaults = buildGameRoundSummaryDefaultValues(round)
    expect(gameRoundSummaryFormSchema.safeParse(defaults).success).toBe(false)

    defaults.scoringInstances[0].isConditionMet = false
    defaults.scoringInstances[1].countValue = 0
    defaults.ruleGroups[0].outcomeStatus = 'violated'
    expect(gameRoundSummaryFormSchema.safeParse(defaults).success).toBe(false)

    defaults.ruleGroups[0].violationComment = 'Observed violation'
    expect(gameRoundSummaryFormSchema.safeParse(defaults).success).toBe(true)
  })
})

function createRound(overrides: Partial<GameRoundDetails> = {}): GameRoundDetails {
  return {
    roundId: 'round-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamName: null,
    teamSlotIndex: 1,
    status: 'reviewing_results',
    roundVersion: 7,
    startedAtUtc: '2026-07-23T10:00:00Z',
    baseScore: 100,
    finalScore: null,
    emptyCardPenaltyApplied: false,
    scoreDetails: createScoreDetails(),
    killsCount: 2,
    bountyCount: 1,
    participants: [],
    modifierResults: [],
    ...overrides,
  }
}

function createModifier(overrides: Partial<ModifierResult> = {}): ModifierResult {
  return {
    modifierResultId: 'modifier-result-1',
    modifierId: 'modifier-1',
    modifierName: 'Momentum',
    modifierCategory: 'round',
    modifierDescription: 'Modifier effect.',
    outcomeStatus: 'pending',
    scoreDelta: 0,
    killDelta: 0,
    activationId: 'activation-1',
    definitionRevision: 1,
    resolutionGroupId: null,
    resolutionKind: 'boolean',
    ...overrides,
  }
}

function createScoreDetails(): GameRoundDetails['scoreDetails'] {
  return {
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
  }
}
