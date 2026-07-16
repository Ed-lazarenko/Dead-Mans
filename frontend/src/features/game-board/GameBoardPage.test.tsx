import { cleanup, fireEvent, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameBoardPage } from './GameBoardPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useGameBoardPage: vi.fn(),
  useGameBoardLaunchPanel: vi.fn(),
}))

vi.mock('./use-game-board-page.ts', () => ({
  useGameBoardPage: pageMocks.useGameBoardPage,
}))

vi.mock('./use-game-board-launch-panel.ts', () => ({
  useGameBoardLaunchPanel: pageMocks.useGameBoardLaunchPanel,
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

function createLaunchPanelState(overrides: Record<string, unknown> = {}) {
  return {
    shouldRender: false,
    snapshot: undefined,
    isStartingGame: false,
    startGame: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
    ...overrides,
  }
}

function createAdminRegistrationSnapshot(overrides: Record<string, unknown> = {}) {
  return {
    gameId: 'game-1',
    gameStatus: 'ready',
    minPlayersPerTeam: 1,
    maxPlayersPerTeam: 2,
    slots: [
      {
        slotId: 'slot-1',
        slotIndex: 1,
        availability: 'public',
        reservedLabel: null,
        isAvailableForNewTeam: false,
        teamId: 'team-1',
        teamStatus: 'confirmed',
      },
    ],
    teams: [
      {
        teamId: 'team-1',
        slotIndex: 1,
        slotAvailability: 'public',
        reservedLabel: null,
        recruitmentOpen: false,
        status: 'confirmed',
        members: [
          {
            player: {
              userId: 'user-1',
              login: 'player',
              displayName: 'Player One',
            },
            joinedAtUtc: '2026-06-11T12:00:00Z',
          },
        ],
        pendingInvitations: [],
      },
    ],
    availablePlayers: [],
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
  pageMocks.useGameBoardLaunchPanel.mockReturnValue(createLaunchPanelState())
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

  it('shows a registration call-to-action above the board while the game is ready', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'ready',
        },
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    expect(screen.getByText('Сейчас идёт приём заявок')).toBeInTheDocument()
    expect(screen.getByText(/Подайте заявку, пока регистрация открыта/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Подать заявку' })).toHaveAttribute(
      'href',
      '/panel/game-application',
    )
    expect(
      screen
        .getByText('Сейчас идёт приём заявок')
        .compareDocumentPosition(screen.getByRole('heading', { name: 'Тестовая игра' })),
    ).toBe(Node.DOCUMENT_POSITION_FOLLOWING)
  })

  it('shows the launch panel action on the ready game board when available', () => {
    const startGame = vi.fn()
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'ready',
        },
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        shouldRender: true,
        snapshot: createAdminRegistrationSnapshot(),
        startGame,
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    expect(pageMocks.useGameBoardLaunchPanel).toHaveBeenCalledWith('ready')
    expect(screen.getByText('Управление запуском игры')).toBeInTheDocument()
    expect(
      screen.getByText(
        'Проверки регистрации пройдены. Откройте панель запуска, чтобы стартовать игру.',
      ),
    ).toBeInTheDocument()
    expect(
      screen
        .getByText('Управление запуском игры')
        .compareDocumentPosition(screen.getByRole('heading', { name: 'Тестовая игра' })),
    ).toBe(Node.DOCUMENT_POSITION_FOLLOWING)

    fireEvent.click(screen.getByRole('button', { name: 'Запуск игры' }))
    expect(screen.getByRole('heading', { name: 'Запуск игры' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Запустить игру' }))
    expect(screen.getByText('Запустить игру?')).toBeInTheDocument()

    const launchButtons = screen.getAllByRole('button', { name: 'Запустить игру' })
    fireEvent.click(launchButtons[launchButtons.length - 1])

    expect(startGame).toHaveBeenCalled()
  })

  it('shows blocked launch messaging in the separate admin container', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'ready',
        },
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        shouldRender: true,
        snapshot: createAdminRegistrationSnapshot({
          slots: [],
          teams: [],
        }),
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    expect(screen.getAllByText('Блокеров: 1')).toHaveLength(2)
    expect(
      screen.getByText(
        'В регистрации осталось блокеров: 1. Откройте панель запуска, чтобы проверить их.',
      ),
    ).toBeInTheDocument()
  })

  it('does not show the launch panel action when it is unavailable for the current user', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'ready',
        },
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    expect(screen.queryByRole('button', { name: 'Запуск игры' })).not.toBeInTheDocument()
  })

  it('keeps the round control panel hidden for now', () => {
    renderWithAppProviders(<GameBoardPage />)

    expect(screen.queryByText('Управление раундом')).not.toBeInTheDocument()
  })
})
