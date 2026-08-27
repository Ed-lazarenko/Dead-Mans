import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { PlayedCardPreviewDialog } from './PlayedCardPreviewDialog.tsx'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(cleanup)

describe('PlayedCardPreviewDialog', () => {
  it('shows stacked modifier impact as one truthful total in the played-card summary', () => {
    renderWithAppProviders(
      <PlayedCardPreviewDialog card={null} round={createRound()} onClose={vi.fn()} />,
    )

    expect(screen.getByText('Универсальный бонус x2')).toBeInTheDocument()
    expect(screen.getByText('+30 очк.')).toBeInTheDocument()
    expect(screen.getByText('Убийства +1')).toBeInTheDocument()
    expect(screen.getByText('Рассчитан x2')).toBeInTheDocument()
    expect(screen.queryByText('+10 очк.')).not.toBeInTheDocument()
    expect(screen.queryByText('+20 очк.')).not.toBeInTheDocument()
  })
})

function createRound(): GameHistoryRound {
  return {
    roundId: 'round-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    status: 'completed',
    roundVersion: 1,
    startedAtUtc: '2026-08-27T12:00:00Z',
    baseScore: 100,
    finalScore: 330,
    emptyCardPenaltyApplied: false,
    scoreDetails: {
      scoreUnit: 100,
      killsScore: 200,
      bountyScore: 0,
      modifierKillDelta: 1,
      modifierKillScore: 100,
      modifierScoreDelta: 30,
      emptyCardPenaltyApplied: false,
      emptyCardPenaltyScore: 0,
      penaltyTotal: 0,
      bonusDelta: 330,
      totalKillCount: 3,
      finalScore: 330,
      calculationLines: [],
    },
    killsCount: 2,
    bountyCount: 0,
    cellId: 'cell-1',
    cellRowIndex: 0,
    cellColIndex: 0,
    cellType: 'question',
    cellTitle: 'Card',
    cellCost: 100,
    purchasesRefunded: false,
    cellMedia: [],
    participants: [],
    modifiers: [
      createModifier('result-1', 'activation-1', 10, 1),
      createModifier('result-2', 'activation-2', 20, 0),
    ],
  }
}

function createModifier(
  modifierResultId: string,
  activationId: string,
  scoreDelta: number,
  killDelta: number,
): GameHistoryRound['modifiers'][number] {
  return {
    modifierResultId,
    modifierId: 'modifier-1',
    modifierName: 'Универсальный бонус',
    modifierDescription: 'Начисляет очки за выполненные действия.',
    modifierCategory: 'result',
    outcomeStatus: 'calculated',
    scoreDelta,
    killDelta,
    activationId,
    definitionRevision: 2,
  }
}
