import { describe, expect, it } from 'vitest'
import { groupCardPlayResultModifiers } from './card-play-result-modifiers.ts'

describe('groupCardPlayResultModifiers', () => {
  it('groups repeated modifier activations by modifier id', () => {
    expect(
      groupCardPlayResultModifiers([
        createModifier({
          modifierResultId: 'result-1',
          modifierId: 'thirst',
          modifierName: 'Жажда',
          scoreDelta: 15,
          killDelta: 1,
          resolutionDataJson:
            '{"effect":"success","killsCount":3,"bountyCount":1,"activationCount":2,"perKillBonus":5,"failurePenaltyPoints":25,"autoResultFormula":"custom_expression","autoResultSuccessExpression":"killsCount * perKillBonus * activationCount","autoResultFailureExpression":null}',
        }),
        createModifier({
          modifierResultId: 'result-2',
          modifierId: 'thirst',
          modifierName: 'Жажда',
          scoreDelta: 15,
        }),
        createModifier({
          modifierResultId: 'result-3',
          modifierId: 'other',
          modifierName: 'Меткий глаз',
        }),
      ]),
    ).toEqual([
      {
        modifierId: 'thirst',
        modifierName: 'Жажда',
        count: 2,
        scoreDeltas: [15, 15],
        killDeltas: [1, 0],
        outcomeStatuses: [{ status: 'completed', count: 2 }],
        multiplierAppliedValues: [],
        calculation: {
          source: null,
          effect: 'success',
          conditionType: null,
          conditionMet: null,
          input: null,
          countValue: null,
          killDeltaValue: null,
          multiplierDelta: null,
          killsCount: 3,
          bountyCount: 1,
          activationCount: 2,
          perKillBonus: 5,
          failurePenaltyPoints: 25,
          formulaMode: 'custom_expression',
          successExpression: 'killsCount * perKillBonus * activationCount',
          failureExpression: null,
        },
      },
      {
        modifierId: 'other',
        modifierName: 'Меткий глаз',
        count: 1,
        scoreDeltas: [0],
        killDeltas: [0],
        outcomeStatuses: [{ status: 'completed', count: 1 }],
        multiplierAppliedValues: [],
        calculation: null,
      },
    ])
  })

  it('keeps same-name modifiers separate when their ids differ', () => {
    expect(
      groupCardPlayResultModifiers([
        createModifier({
          modifierResultId: 'result-1',
          modifierId: 'thirst-1',
          modifierName: 'Жажда',
        }),
        createModifier({
          modifierResultId: 'result-2',
          modifierId: 'thirst-2',
          modifierName: 'Жажда',
        }),
      ]),
    ).toEqual([
      {
        modifierId: 'thirst-1',
        modifierName: 'Жажда',
        count: 1,
        scoreDeltas: [0],
        killDeltas: [0],
        outcomeStatuses: [{ status: 'completed', count: 1 }],
        multiplierAppliedValues: [],
        calculation: null,
      },
      {
        modifierId: 'thirst-2',
        modifierName: 'Жажда',
        count: 1,
        scoreDeltas: [0],
        killDeltas: [0],
        outcomeStatuses: [{ status: 'completed', count: 1 }],
        multiplierAppliedValues: [],
        calculation: null,
      },
    ])
  })

  it('collects distinct multiplier values and outcome counts', () => {
    expect(
      groupCardPlayResultModifiers([
        createModifier({
          modifierResultId: 'result-1',
          modifierId: 'multiplier',
          modifierName: 'Множитель',
          outcomeStatus: 'completed',
          multiplierApplied: 1.5,
        }),
        createModifier({
          modifierResultId: 'result-2',
          modifierId: 'multiplier',
          modifierName: 'Множитель',
          outcomeStatus: 'failed',
          multiplierApplied: 1.5,
        }),
      ])[0],
    ).toMatchObject({
      modifierId: 'multiplier',
      count: 2,
      outcomeStatuses: [
        { status: 'completed', count: 1 },
        { status: 'failed', count: 1 },
      ],
      multiplierAppliedValues: [1.5],
    })
  })

  it('uses the grouped count when every duplicated activation stores its own calculation context', () => {
    expect(
      groupCardPlayResultModifiers([
        createModifier({
          modifierResultId: 'result-1',
          modifierId: 'thirst',
          modifierName: 'Жажда',
          scoreDelta: -25,
          outcomeStatus: 'failed',
          resolutionDataJson:
            '{"effect":"failure","killsCount":0,"bountyCount":0,"activationCount":1,"perKillBonus":5,"failurePenaltyPoints":25,"autoResultFormula":"flat_per_kill"}',
        }),
        createModifier({
          modifierResultId: 'result-2',
          modifierId: 'thirst',
          modifierName: 'Жажда',
          scoreDelta: -25,
          outcomeStatus: 'failed',
          resolutionDataJson:
            '{"effect":"failure","killsCount":0,"bountyCount":0,"activationCount":1,"perKillBonus":5,"failurePenaltyPoints":25,"autoResultFormula":"flat_per_kill"}',
        }),
      ])[0],
    ).toMatchObject({
      count: 2,
      scoreDeltas: [-25, -25],
      outcomeStatuses: [{ status: 'failed', count: 2 }],
      calculation: {
        source: null,
        effect: 'failure',
        conditionType: null,
        conditionMet: null,
        input: null,
        countValue: null,
        killDeltaValue: null,
        multiplierDelta: null,
        activationCount: 1,
        failurePenaltyPoints: 25,
      },
    })
  })

  it('parses manual calculation details from stored resolution data', () => {
    expect(
      groupCardPlayResultModifiers([
        createModifier({
          modifierResultId: 'result-1',
          modifierId: 'mentor',
          modifierName: 'Наставник',
          killDelta: 2,
          resolutionDataJson:
            '{"source":"manual_count","input":"mentorKills","countValue":2,"killDeltaValue":1}',
        }),
      ])[0],
    ).toMatchObject({
      modifierId: 'mentor',
      killDeltas: [2],
      calculation: {
        source: 'manual_count',
        input: 'mentorKills',
        countValue: 2,
        killDeltaValue: 1,
      },
    })
  })
})

function createModifier(
  overrides: {
    modifierResultId: string
    modifierId: string
    modifierName: string
  } & Partial<ReturnType<typeof createDefaultModifier>>,
) {
  return {
    ...createDefaultModifier(),
    ...overrides,
  }
}

function createDefaultModifier() {
  return {
    modifierResultId: 'result-1',
    modifierId: 'modifier-1',
    modifierName: 'Modifier',
    modifierDescription: '',
    modifierCategory: 'round',
    modifierMechanicType: 'passive',
    outcomeStatus: 'completed',
    scoreDelta: 0,
    killDelta: 0,
    multiplierApplied: null,
    resolutionDataJson: null,
    resolvedByUserId: null,
    resolvedAtUtc: null,
  }
}
