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
  it('groups identical active modifiers and sorts groups from cheaper to more expensive', () => {
    const grouped = groupActiveGameModifiers([
      createActivation({
        activationId: 'activation-2',
        activatedByUserId: 'user-2',
        activatedAtUtc: '2026-07-21T18:01:00Z',
        activatedByDisplayName: 'Player Two',
      }),
      createActivation({
        activationId: 'activation-3',
        activatedByUserId: 'user-3',
        activatedAtUtc: '2026-07-21T18:03:00Z',
        activatedByDisplayName: 'Player Three',
      }),
      createActivation({
        activationId: 'activation-4',
        modifierId: 'modifier-2',
        modifierName: 'Mentorbait',
        activationCost: 30,
        activatedAtUtc: '2026-07-21T18:02:00Z',
      }),
    ])

    expect(grouped).toHaveLength(2)
    expect(grouped[0]).toMatchObject({
      modifierId: 'modifier-1',
      activationsCount: 2,
      activationCost: 15,
      lastActivatedByDisplayName: 'Player Three',
    })
    expect(grouped[0]?.activations[0]?.activatedAtUtc).toBe('2026-07-21T18:03:00Z')
    expect(grouped[0]?.activators).toEqual([
      {
        userId: 'user-3',
        displayName: 'Player Three',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:03:00Z',
      },
      {
        userId: 'user-2',
        displayName: 'Player Two',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:01:00Z',
      },
    ])
    expect(grouped.map((item) => item.modifierId)).toEqual(['modifier-1', 'modifier-2'])
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
        userId: 'user-1',
        displayName: 'Player One',
        activationsCount: 2,
        lastActivatedAtUtc: '2026-07-21T18:04:00Z',
      },
      {
        userId: 'user-2',
        displayName: 'Player Two',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:03:00Z',
      },
    ])
  })

  it('keeps activations from different users separate even when display names match', () => {
    const grouped = groupActiveGameModifiers([
      createActivation({
        activationId: 'activation-2',
        activatedByUserId: 'user-1',
        activatedByDisplayName: 'Same Name',
        activatedAtUtc: '2026-07-21T18:01:00Z',
      }),
      createActivation({
        activationId: 'activation-3',
        activatedByUserId: 'user-2',
        activatedByDisplayName: 'Same Name',
        activatedAtUtc: '2026-07-21T18:02:00Z',
      }),
    ])

    expect(grouped[0]?.activators).toEqual([
      {
        userId: 'user-2',
        displayName: 'Same Name',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:02:00Z',
      },
      {
        userId: 'user-1',
        displayName: 'Same Name',
        activationsCount: 1,
        lastActivatedAtUtc: '2026-07-21T18:01:00Z',
      },
    ])
  })

  it('groups available modifiers by category, sorts activatable modifiers by cost and pushes conflicts and exhausted limits down', () => {
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
          id: 'modifier-5',
          name: 'Cheap',
          activationCost: 3,
        },
        canActivate: true,
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-2',
          name: 'No points',
          activationCost: 5,
        },
        canActivate: false,
        blockedReason: 'insufficient_points',
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-6',
          name: 'Conflict',
          activationCost: 1,
        },
        canActivate: false,
        blockedReason: 'conflict_active',
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-7',
          name: 'Limit reached',
          activationCost: 2,
        },
        canActivate: false,
        blockedReason: 'limit_reached',
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
    expect(grouped[0]?.items.map((item) => item.modifier.name)).toEqual([
      'Cheap',
      'Expensive',
      'No points',
      'Conflict',
      'Limit reached',
    ])
    expect(grouped[1]).toMatchObject({ category: 'result' })
  })

  it('moves categories with only blocked modifiers below categories that still have activatable modifiers', () => {
    const grouped = groupAvailableGameModifiers([
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-8',
          category: 'preparation',
          name: 'Preparation blocked',
          activationCost: 2,
        },
        canActivate: false,
        blockedReason: 'conflict_active',
      }),
      createAvailability({
        modifier: {
          ...createAvailability().modifier,
          id: 'modifier-9',
          category: 'result',
          name: 'Result active',
          activationCost: 4,
        },
        canActivate: true,
      }),
    ])

    expect(grouped.map((item) => item.category)).toEqual(['result', 'preparation'])
  })
})
