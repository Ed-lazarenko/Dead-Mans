import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { buildModifierSearchText, matchesModifierSearch } from './modifier-search.ts'

function createModifier(overrides: Partial<GameModifierDefinition> = {}): GameModifierDefinition {
  return {
    id: 'modifier-1',
    name: 'Патрон',
    description: 'Если враг убит первой пулей, команда получает бонус.',
    category: 'result',
    activationCost: 4,
    activationLimit: { count: 1 },
    conflictingModifierIds: [],
    iconEmoji: '🔫',
    activationCommand: '!активировать патрон',
    isLockedByActiveGame: false,
    revision: 1,
    normalizedTags: ['weapon', 'first bullet'],
    behaviorV2: {
      schemaVersion: 2,
      kind: 'scoring',
      phase: 'result',
      performer: 'activeTeam',
      requiresHostMonitoring: true,
      rule: 'First bullet bonus',
      stackingPolicy: 'independentInstances',
      resolution: { type: 'boolean' },
      reward: 'bonusKills',
      formulaReference: {
        code: 'bonus_kill_on_condition',
        version: 1,
        parameters: { type: 'bonusKillOnCondition', successBonusKills: 1 },
      },
    },
    ...overrides,
  }
}

describe('modifier-search', () => {
  it('includes derived round summary fields and domain metadata in the search text', () => {
    const text = buildModifierSearchText(createModifier())

    expect(text).toContain('toggle_bonus')
    expect(text).toContain('bonus_kill_on_condition')
    expect(text).toContain('first bullet')
    expect(text).toContain('!активировать патрон')
  })

  it('matches translated or UI-provided extra terms', () => {
    const modifier = createModifier()

    expect(matchesModifierSearch(modifier, 'бинарный бонус', ['Бинарный бонус по условию'])).toBe(
      true,
    )
    expect(matchesModifierSearch(modifier, 'mentor')).toBe(false)
  })
})
