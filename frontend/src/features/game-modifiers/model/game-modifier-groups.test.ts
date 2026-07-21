import { describe, expect, it } from 'vitest'
import type {
  GameModifierActivation,
  GameModifierAvailability,
} from '../../../shared/api/contracts/index.ts'
import { groupActiveGameModifiers, groupAvailableGameModifiers } from './game-modifier-groups.ts'

function createActivation(overrides: Partial<GameModifierActivation> = {}): GameModifierActivation {
  return {
    activationId: 'activation-1',
    modifierId: 'modifier-1',
    modifierName: 'Chirik',
    activatedByUserId: 'user-1',
    activatedByDisplayName: 'Player One',
    activationCost: 15,
    activatedAtUtc: '2026-07-21T18:00:00Z',
    ...overrides,
  }
}

function createAvailability(
  overrides: Partial<GameModifierAvailability> = {},
): GameModifierAvailability {
  return {
    modifier: {
      id: 'modifier-1',
      scoringType: 'non_scoring',
      category: 'round',
      requiresHostControl: false,
      mechanicType: 'rule_only',
      name: 'Chirik',
      description: 'Compact modifier',
      activationCost: 15,
      defaultLimitPerGame: 2,
      activationLimit: { count: 2 },
      effect: {
        mechanicType: 'rule_only',
        traits: [],
        durationSeconds: null,
        ruleText: null,
        scoreImpact: null,
        conditions: [],
        resolutionInputs: [],
        killEffect: null,
        multiplierEffect: null,
        mentorEffect: null,
      },
      conflictingModifierIds: [],
      iconEmoji: '🔥',
      activationCommand: null,
    },
    isActive: false,
    canActivate: true,
    blockedReason: null,
    activationsCount: 0,
    limit: 2,
    ...overrides,
  }
}

describe('game modifier groups', () => {
  it('groups identical active modifiers and keeps the latest activation first', () => {
    const grouped = groupActiveGameModifiers([
      createActivation({
        activationId: 'activation-2',
        activatedAtUtc: '2026-07-21T18:01:00Z',
        activatedByDisplayName: 'Player Two',
      }),
      createActivation({
        activationId: 'activation-3',
        activatedAtUtc: '2026-07-21T18:03:00Z',
        activatedByDisplayName: 'Player Three',
      }),
      createActivation({
        activationId: 'activation-4',
        modifierId: 'modifier-2',
        modifierName: 'Mentorbait',
        activatedAtUtc: '2026-07-21T18:02:00Z',
      }),
    ])

    expect(grouped).toHaveLength(2)
    expect(grouped[0]).toMatchObject({
      modifierId: 'modifier-1',
      activationsCount: 2,
      totalActivationCost: 30,
      lastActivatedByDisplayName: 'Player Three',
    })
    expect(grouped[0]?.activations[0]?.activatedAtUtc).toBe('2026-07-21T18:03:00Z')
    expect(grouped[0]?.activators).toEqual([
      {
        displayName: 'Player Three',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:03:00Z',
      },
      {
        displayName: 'Player Two',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:01:00Z',
      },
    ])
    expect(grouped[1]?.modifierId).toBe('modifier-2')
  })

  it('collapses repeated activations from the same player into one activator summary entry', () => {
    const grouped = groupActiveGameModifiers([
      createActivation({
        activationId: 'activation-2',
        activatedAtUtc: '2026-07-21T18:01:00Z',
      }),
      createActivation({
        activationId: 'activation-3',
        activatedAtUtc: '2026-07-21T18:04:00Z',
      }),
      createActivation({
        activationId: 'activation-4',
        activatedByUserId: 'user-2',
        activatedByDisplayName: 'Player Two',
        activatedAtUtc: '2026-07-21T18:03:00Z',
      }),
    ])

    expect(grouped[0]?.activators).toEqual([
      {
        displayName: 'Player One',
        activationsCount: 2,
        lastActivatedAtUtc: '2026-07-21T18:04:00Z',
      },
      {
        displayName: 'Player Two',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:03:00Z',
      },
    ])
  })

  it('groups available modifiers by category and sorts activatable entries first', () => {
    const grouped = groupAvailableGameModifiers([
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-3',
          name: 'Expensive',
          activationCost: 25,
        },
        canActivate: true,
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-2',
          name: 'Blocked',
          activationCost: 5,
        },
        canActivate: false,
        blockedReason: 'insufficient_points',
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-4',
          category: 'result',
          name: 'Result buff',
          activationCost: 10,
        },
      }),
    ])

    expect(grouped).toHaveLength(2)
    expect(grouped[0]).toMatchObject({ category: 'round' })
    expect(grouped[0]?.items.map((item) => item.modifier.name)).toEqual(['Expensive', 'Blocked'])
    expect(grouped[1]).toMatchObject({ category: 'result' })
  })
})
