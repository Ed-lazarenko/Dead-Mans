import { describe, expect, it } from 'vitest'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { buildModifierSearchText, matchesModifierSearch } from './modifier-search.ts'

function createModifier(overrides: Partial<GameModifierDefinition> = {}): GameModifierDefinition {
  return {
    id: 'modifier-1',
    name: 'Патрон',
    description: 'Если враг убит первой пулей, команда получает бонус.',
    scoringType: 'conditional_bonus',
    category: 'result',
    requiresHostControl: true,
    mechanicType: 'kill_counter',
    activationCost: 4,
    defaultLimitPerGame: 1,
    activationLimit: { count: 1 },
    effect: {
      mechanicType: 'kill_counter',
      traits: ['requires_manual_resolution'],
      durationSeconds: null,
      ruleText: null,
      scoreImpact: {
        pointsDelta: null,
        perKillBonus: null,
        failurePenaltyPoints: null,
        multiplierDelta: null,
        killDelta: 1,
      },
      conditions: [{ type: 'first_kill_first_bullet', source: 'manual_input' }],
      resolutionInputs: ['kills'],
      killEffect: {
        killDeltaMode: 'conditional_bonus_kill',
        killDeltaValue: 1,
        condition: 'first_kill_first_bullet',
        excludedWeapons: ['дробовик'],
      },
      multiplierEffect: null,
      mentorEffect: null,
    },
    conflictingModifierIds: [],
    iconEmoji: '🔫',
    activationCommand: '!активировать патрон',
    ...overrides,
  }
}

describe('modifier-search', () => {
  it('includes derived round summary fields and domain metadata in the search text', () => {
    const text = buildModifierSearchText(createModifier())

    expect(text).toContain('toggle_bonus')
    expect(text).toContain('conditional_bonus_kill')
    expect(text).toContain('first_kill_first_bullet')
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
