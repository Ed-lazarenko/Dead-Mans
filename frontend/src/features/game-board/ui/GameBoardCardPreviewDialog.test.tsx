import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import type { GameBoardCell } from '../../../shared/api/contracts/index.ts'
import type { GameBoardCardPlayResultRound } from '../use-card-play-result.ts'
import { GameBoardCardPreviewDialog } from './GameBoardCardPreviewDialog.tsx'

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(() => {
  cleanup()
})

describe('GameBoardCardPreviewDialog', () => {
  it('uses the shared played-card result layout from the leaderboard', () => {
    renderWithAppProviders(
      <GameBoardCardPreviewDialog
        cell={createCell()}
        playResult={{
          round: createRound({
            finalScore: -150,
            emptyCardPenaltyApplied: true,
            scoreDetails: createScoreDetails({
              finalScore: -150,
              emptyCardPenaltyApplied: true,
              emptyCardPenaltyScore: -100,
              penaltyTotal: 150,
              bonusDelta: -150,
            }),
            modifiers: [
              createModifier({
                modifierName: 'Токсик',
                modifierDescription: 'Провальная карточка снимает очки.',
                scoreDelta: -50,
              }),
              createModifier({
                modifierResultId: 'modifier-result-2',
                modifierName: 'Токсик',
                modifierDescription: 'Провальная карточка снимает очки.',
                scoreDelta: -50,
              }),
            ],
          }),
          isLoading: false,
          isError: false,
        }}
        onClose={vi.fn()}
      />,
    )

    expect(screen.getByText('Стоимость карточки')).toBeInTheDocument()
    expect(screen.getByText('100 очк.')).toBeInTheDocument()
    expect(screen.getByText('Итоговый штраф')).toBeInTheDocument()
    expect(screen.getByText('150 очк.')).toBeInTheDocument()
    expect(screen.getByText('Токсик x2')).toBeInTheDocument()
    expect(screen.getByText('Провальная карточка снимает очки.')).toBeInTheDocument()
    expect(screen.getByText('Провален x2')).toBeInTheDocument()
    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.getByText('Player Two')).toBeInTheDocument()
    expect(screen.queryByText('Player One, Player Two')).not.toBeInTheDocument()
  })
})

function createCell(): GameBoardCell {
  return {
    id: 'cell-1',
    row: 0,
    col: 2,
    cellType: 'question',
    title: 'Токсик 100',
    description: null,
    cost: 100,
    state: 'open',
    media: [],
  }
}

function createRound(
  overrides: Partial<GameBoardCardPlayResultRound> = {},
): GameBoardCardPlayResultRound {
  return {
    roundId: 'round-1',
    teamId: 'team-1',
    teamName: 'Toxic Team',
    teamSlotIndex: 3,
    status: 'completed',
    startedAtUtc: '2026-07-23T09:00:00Z',
    finishedAtUtc: '2026-07-23T09:10:00Z',
    baseScore: 100,
    finalScore: 100,
    emptyCardPenaltyApplied: false,
    scoreDetails: createScoreDetails(),
    killsCount: 0,
    bountyCount: 0,
    cellId: 'cell-1',
    cellRowIndex: 0,
    cellColIndex: 2,
    cellType: 'question',
    cellTitle: 'Токсик 100',
    cellDescription: null,
    cellCost: 100,
    notes: null,
    cellMedia: [],
    participants: [
      {
        userId: 'user-1',
        displayName: 'Player One',
        createdAtUtc: '2026-07-23T09:00:00Z',
      },
      {
        userId: 'user-2',
        displayName: 'Player Two',
        createdAtUtc: '2026-07-23T09:00:00Z',
      },
    ],
    modifiers: [],
    ...overrides,
  }
}

function createScoreDetails(
  overrides: Partial<GameBoardCardPlayResultRound['scoreDetails']> = {},
): GameBoardCardPlayResultRound['scoreDetails'] {
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
    finalScore: 100,
    ...overrides,
  }
}

function createModifier(
  overrides: Partial<GameBoardCardPlayResultRound['modifiers'][number]> = {},
): GameBoardCardPlayResultRound['modifiers'][number] {
  return {
    modifierResultId: 'modifier-result-1',
    modifierId: 'modifier-1',
    modifierName: 'Modifier',
    modifierDescription: '',
    modifierCategory: 'result',
    modifierMechanicType: 'restriction_with_reward',
    outcomeStatus: 'failed',
    scoreDelta: 0,
    killDelta: 0,
    multiplierApplied: null,
    resolutionDataJson: null,
    resolvedByUserId: null,
    resolvedAtUtc: null,
    ...overrides,
  }
}
