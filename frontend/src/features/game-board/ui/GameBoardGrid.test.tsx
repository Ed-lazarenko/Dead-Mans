import { cleanup, fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import type { GameBoardCellPlayResult } from '../model/game-board-cell-results.ts'
import { GameBoardGrid } from './GameBoardGrid.tsx'

const snapshot = {
  gameId: 'game-1',
  title: 'Test board',
  description: null,
  status: 'active' as const,
  version: 1,
  rows: 1,
  cols: 2,
  rowLabels: ['A'],
  colLabels: ['1', '2'],
  enabledModifierIds: [],
  activeModifiers: [],
  activeTeamId: 'team-1',
  cells: [
    {
      id: 'cell-1',
      row: 0,
      col: 0,
      title: 'Открытая карта',
      description: 'Описание',
      cost: 100,
      type: 'question',
      state: 'open',
      media: [{ url: '/media/cards/open-card.png' }],
    },
    {
      id: 'cell-2',
      row: 0,
      col: 1,
      title: 'Закрытая карта',
      description: null,
      cost: 200,
      type: 'question',
      state: 'closed',
      media: [],
    },
  ],
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(() => {
  cleanup()
})

describe('GameBoardGrid', () => {
  it('renders prominent semantic column headers', () => {
    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    expect(screen.getAllByRole('columnheader').map((header) => header.textContent)).toEqual([
      '1',
      '2',
    ])
  })

  it('renders preview media for an opened cell', () => {
    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    expect(screen.getByAltText('Открытая карта')).toHaveAttribute(
      'src',
      'http://localhost:5285/media/cards/open-card.png',
    )
    expect(screen.queryByText('Медиа: 1')).not.toBeInTheDocument()
  })

  it('marks the card used by the current round with a visible status', () => {
    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        activeCellId="cell-1"
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    expect(screen.getByRole('status')).toHaveTextContent('Текущий раунд')
  })

  it('opens the preview dialog when an opened cell is clicked', () => {
    const onCellPreviewMedia = vi.fn()

    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={onCellPreviewMedia}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Открыть карточку Открытая карта' }))

    expect(onCellPreviewMedia).toHaveBeenCalledWith(snapshot.cells[0])
  })

  it('shows the played result summary for a completed opened cell', () => {
    const playedResult: GameBoardCellPlayResult = {
      roundId: 'round-1',
      cellId: 'cell-1',
      teamName: 'Dead Mans',
      teamSlotIndex: 2,
      finalScore: 145,
      baseScore: 100,
      emptyCardPenaltyApplied: false,
      scoreDetails: createScoreDetails({
        finalScore: 145,
        bonusDelta: 45,
      }),
      killsCount: 1,
      bountyCount: 0,
      finishedAtUtc: '2026-07-23T10:00:00Z',
      status: 'completed',
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
    }

    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        playResultsByCellId={new Map([['cell-1', playedResult]])}
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    expect(screen.getByText('Dead Mans')).toBeInTheDocument()
    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.getByText('Player Two')).toBeInTheDocument()
    expect(screen.getByText('Итог 145 очк.')).toBeInTheDocument()
    expect(screen.queryByText('100 очк.')).not.toBeInTheDocument()
    expect(screen.queryByText('Медиа: 1')).not.toBeInTheDocument()
  })

  it('shows the full penalty total for an empty card with a modifier penalty', () => {
    const playedResult: GameBoardCellPlayResult = {
      roundId: 'round-1',
      cellId: 'cell-1',
      teamName: 'Toxic Team',
      teamSlotIndex: 3,
      finalScore: -150,
      baseScore: 100,
      emptyCardPenaltyApplied: true,
      scoreDetails: createScoreDetails({
        finalScore: -150,
        emptyCardPenaltyApplied: true,
        emptyCardPenaltyScore: -100,
        penaltyTotal: 150,
        bonusDelta: -150,
      }),
      killsCount: 0,
      bountyCount: 0,
      finishedAtUtc: '2026-07-23T10:00:00Z',
      status: 'completed',
      participants: [
        {
          userId: 'user-1',
          displayName: 'Player One',
          createdAtUtc: '2026-07-23T09:00:00Z',
        },
      ],
      modifiers: [
        {
          modifierResultId: 'modifier-result-1',
          modifierId: 'modifier-1',
          modifierName: 'Токсик',
          modifierDescription: '',
          modifierCategory: 'result',
          outcomeStatus: 'failed',
          scoreDelta: -50,
          killDelta: 0,
          multiplierApplied: null,
          resolutionDataJson: null,
          resolvedByUserId: null,
          resolvedAtUtc: null,
        },
      ],
    }

    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        playResultsByCellId={new Map([['cell-1', playedResult]])}
        canOpenCells={false}
        onCellRequestOpen={vi.fn()}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    expect(screen.getByText('Штраф 150 очк.')).toBeInTheDocument()
  })

  it('requests opening a closed cell when opening is allowed', () => {
    const onCellRequestOpen = vi.fn()

    renderWithAppProviders(
      <GameBoardGrid
        snapshot={snapshot}
        canOpenCells
        onCellRequestOpen={onCellRequestOpen}
        onCellPreviewMedia={vi.fn()}
      />,
    )

    fireEvent.click(
      screen.getByRole('button', {
        name: 'Открыть карточку «Закрытая карта» стоимостью 200 очк.',
      }),
    )

    expect(onCellRequestOpen).toHaveBeenCalledWith(snapshot.cells[1])
  })
})

function createScoreDetails(
  overrides: Partial<GameBoardCellPlayResult['scoreDetails']> = {},
): GameBoardCellPlayResult['scoreDetails'] {
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
