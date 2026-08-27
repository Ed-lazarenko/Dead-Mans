import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { RoundScoreBreakdown } from './RoundScoreBreakdown.tsx'

type ScoreDetails = components['schemas']['GameRoundScoreDetailsDto']

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(cleanup)

describe('RoundScoreBreakdown', () => {
  it('explains every generic modifier formula used by completed-round cards', () => {
    renderWithAppProviders(
      <RoundScoreBreakdown
        score={createScore([
          modifierLine('fixed_points_per_unit', 30, [
            { code: 'sourceUnits', value: 3 },
            { code: 'pointsPerUnit', value: 10 },
          ]),
          modifierLine('card_percent_per_unit', 150, [
            { code: 'sourceUnits', value: 2 },
            { code: 'cardValue', value: 100 },
            { code: 'rate', value: 0.75 },
          ]),
          {
            ...modifierLine('bonus_kills_per_unit', 200, [
              { code: 'sourceUnits', value: 2 },
              { code: 'bonusKillsPerUnit', value: 1 },
              { code: 'bonusKills', value: 2 },
              { code: 'cardValue', value: 100 },
            ]),
            kind: 'modifierBonusKills',
          },
          modifierLine('kill_value_increase_per_unit', 20, [
            { code: 'sourceUnits', value: 2 },
            { code: 'incrementPointsPerUnit', value: 5 },
            { code: 'killsCount', value: 2 },
            { code: 'killValueIncreasePoints', value: 20 },
            { code: 'zeroSourceActivations', value: 0 },
            { code: 'zeroCountPenaltyPoints', value: 25 },
            { code: 'zeroSourcePenaltyPoints', value: 0 },
          ]),
        ])}
      />,
    )

    expect(screen.getByText('3 ед. × 10 очк. = 30.')).toBeInTheDocument()
    expect(screen.getByText(/2 ед. × стоимость карточки 100 × 75% = 150/)).toBeInTheDocument()
    expect(screen.getByText(/2 ед. × 1 = 2 бонусных убийств/)).toBeInTheDocument()
    expect(screen.getByText(/Рост: 2 ед. × 5 × 2 убийств = 20/)).toBeInTheDocument()
  })
})

function createScore(calculationLines: ScoreDetails['calculationLines']): ScoreDetails {
  return {
    scoreUnit: 100,
    killsScore: 0,
    bountyScore: 0,
    modifierKillDelta: 0,
    modifierKillScore: 0,
    modifierScoreDelta: 400,
    emptyCardPenaltyApplied: false,
    emptyCardPenaltyScore: 0,
    penaltyTotal: 0,
    bonusDelta: 400,
    totalKillCount: 0,
    finalScore: 400,
    calculationLines,
  }
}

function modifierLine(
  formulaCode: string,
  pointsDelta: number,
  operands: ScoreDetails['calculationLines'][number]['operands'],
): ScoreDetails['calculationLines'][number] {
  return {
    kind: 'modifierPoints',
    modifierId: `modifier-${formulaCode}`,
    modifierName: formulaCode,
    activationCount: 1,
    pointsDelta,
    runningTotal: pointsDelta,
    formulaCode,
    formulaVersion: 1,
    operands,
  }
}
