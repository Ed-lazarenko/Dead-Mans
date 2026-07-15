import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameBoardPage } from './GameBoardPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useGameBoardPage: vi.fn(),
}))

vi.mock('./use-game-board-page.ts', () => ({
  useGameBoardPage: pageMocks.useGameBoardPage,
}))

const readySnapshot = {
  gameId: 'game-1',
  title: 'Тестовая игра',
  description: 'Описание игры',
  status: 'active' as const,
  version: 1,
  rows: 1,
  cols: 1,
  rowLabels: ['A'],
  colLabels: ['1'],
  cells: [],
  enabledModifierIds: [],
  activeModifiers: [],
}

function createPageQuery(overrides: Record<string, unknown> = {}) {
  return {
    isLoading: false,
    isError: false,
    data: readySnapshot,
    activeRun: null,
    ...overrides,
  }
}

vi.mock('./use-open-game-board-cell.ts', () => ({
  useOpenGameBoardCell: () => ({
    pendingCell: null,
    toastMessage: null,
    canOpenCells: false,
    isSubmitting: false,
    requestOpenCell: vi.fn(),
    confirmOpenCell: vi.fn(),
    dismissPendingCell: vi.fn(),
    dismissToast: vi.fn(),
  }),
}))

vi.mock('./ui/GameBoardGrid.tsx', () => ({
  GameBoardGrid: () => <div data-testid="game-board-grid" />,
}))

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  pageMocks.useGameBoardPage.mockReturnValue(createPageQuery())
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameBoardPage', () => {
  it('renders loading, error and empty states', () => {
    pageMocks.useGameBoardPage.mockReturnValue(createPageQuery({ isLoading: true }))
    renderWithAppProviders(<GameBoardPage />)
    expect(screen.getByText('Загрузка игрового поля...')).toBeInTheDocument()

    cleanup()
    pageMocks.useGameBoardPage.mockReturnValue(createPageQuery({ isError: true }))
    renderWithAppProviders(<GameBoardPage />)
    expect(screen.getByText('Не удалось загрузить игровое поле.')).toBeInTheDocument()

    cleanup()
    pageMocks.useGameBoardPage.mockReturnValue(createPageQuery({ data: null }))
    renderWithAppProviders(<GameBoardPage />)
    expect(screen.getByText('Игровое поле сейчас недоступно.')).toBeInTheDocument()
  })

  it('renders only the game board surface and its status', () => {
    renderWithAppProviders(<GameBoardPage />)

    expect(screen.getByRole('heading', { name: 'Тестовая игра' })).toBeInTheDocument()
    expect(screen.getByText('Активна')).toBeInTheDocument()
    expect(screen.getByTestId('game-board-grid')).toBeInTheDocument()
    expect(screen.queryByText(/модификатор/i)).not.toBeInTheDocument()
  })

  it('renders active run chip when card run is in progress', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        activeRun: {
          cardRunId: 'run-1',
          teamSlotIndex: 2,
          baseScore: 120,
        },
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    expect(screen.getByText('Идёт раунд: команда #2, база 120')).toBeInTheDocument()
  })
})
