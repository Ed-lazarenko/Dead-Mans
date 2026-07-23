import { describe, expect, it } from 'vitest'
import {
  evaluateModifierScoreExpression,
  evaluateModifierScoreFormulaFailure,
  evaluateModifierScoreFormulaSuccess,
} from './modifier-score-formula.ts'

const context = {
  killsCount: 5,
  bountyCount: 1,
  scoreUnit: 100,
  baseScore: 100,
  perKillBonus: 5,
  failurePenaltyPoints: 25,
  activationCount: 2,
  totalOutcomeCount: 6,
}

describe('modifier-score-formula', () => {
  it('evaluates built-in stacking formulas', () => {
    expect(
      evaluateModifierScoreFormulaSuccess(
        {
          mode: 'stacking_per_kill_bonus',
          successExpression: null,
          failureExpression: null,
        },
        context,
      ),
    ).toBe(125)
  })

  it('evaluates custom expressions with variables and helper functions', () => {
    expect(
      evaluateModifierScoreExpression(
        'max(killsCount * perKillBonus, bountyCount * scoreUnit / 2)',
        context,
      ),
    ).toBe(50)
  })

  it('evaluates custom success and failure expressions', () => {
    expect(
      evaluateModifierScoreFormulaSuccess(
        {
          mode: 'custom_expression',
          successExpression: 'killsCount * scoreUnit + activationCount * 10',
          failureExpression: '-failurePenaltyPoints',
        },
        context,
      ),
    ).toBe(520)

    expect(
      evaluateModifierScoreFormulaFailure(
        {
          mode: 'custom_expression',
          successExpression: 'killsCount * scoreUnit + activationCount * 10',
          failureExpression: '-failurePenaltyPoints',
        },
        context,
      ),
    ).toBe(-25)
  })
})
