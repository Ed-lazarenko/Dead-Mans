import { cleanup, fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
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

    fireEvent.click(screen.getByRole('button', { name: 'Открыть медиа карточки Открытая карта' }))

    expect(onCellPreviewMedia).toHaveBeenCalledWith(snapshot.cells[0])
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
        name: 'Вы уверены, что хотите открыть эту карточку (ряд 0, колонка 1, стоимость 200)?',
      }),
    )

    expect(onCellRequestOpen).toHaveBeenCalledWith(snapshot.cells[1])
  })
})
