import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameBoardPage } from './GameBoardPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useGameBoardPage: vi.fn(),
  useGameBoardLaunchPanel: vi.fn(),
  useActiveGameTeam: vi.fn(),
  useManualQuizAward: vi.fn(),
  useManualQuizAwardPlayers: vi.fn(),
  useGameTeamPlayedState: vi.fn(),
  useStartGameRound: vi.fn(),
  useCardPlayResult: vi.fn(),
  useGameBoardCellResults: vi.fn(),
}))

vi.mock('./use-game-board-page.ts', () => ({
  useGameBoardPage: pageMocks.useGameBoardPage,
}))

vi.mock('./use-game-board-launch-panel.ts', () => ({
  useGameBoardLaunchPanel: pageMocks.useGameBoardLaunchPanel,
}))

vi.mock('./use-active-game-team.ts', () => ({
  useActiveGameTeam: pageMocks.useActiveGameTeam,
}))

vi.mock('./use-manual-quiz-award.ts', () => ({
  useManualQuizAward: pageMocks.useManualQuizAward,
}))

vi.mock('./use-manual-quiz-award-players.ts', () => ({
  useManualQuizAwardPlayers: pageMocks.useManualQuizAwardPlayers,
}))

vi.mock('./use-game-team-played-state.ts', () => ({
  useGameTeamPlayedState: pageMocks.useGameTeamPlayedState,
}))

vi.mock('./use-start-game-round.ts', () => ({
  useStartGameRound: pageMocks.useStartGameRound,
}))

vi.mock('./use-card-play-result.ts', () => ({
  useCardPlayResult: pageMocks.useCardPlayResult,
}))

vi.mock('./use-game-board-cell-results.ts', () => ({
  useGameBoardCellResults: pageMocks.useGameBoardCellResults,
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
  activeTeamId: null,
}

function createPageQuery(overrides: Record<string, unknown> = {}) {
  return {
    isLoading: false,
    isError: false,
    data: readySnapshot,
    activeRound: null,
    teamQueue: [],
    teamQueueSummary: {
      totalTeams: 0,
      playedTeams: 0,
      remainingTeams: 0,
    },
    isTeamQueueLoading: false,
    isTeamQueueError: false,
    ...overrides,
  }
}

function createLaunchPanelState(overrides: Record<string, unknown> = {}) {
  return {
    canManageGame: false,
    canStartGame: false,
    shouldRender: false,
    snapshot: undefined,
    isLoadingLaunchState: false,
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
    launchSummary: {
      canStartGame: true,
      confirmedTeamsCount: 1,
      formingTeamsCount: 0,
      pendingInvitationsCount: 0,
      disbandRequestsCount: 0,
      invalidConfirmedRostersCount: 0,
    },
    teamSlots: [
      {
        teamSlotId: 'slot-1',
        teamSlotIndex: 1,
        teamSlotType: 'public',
        reservedLabel: null,
        isAvailableForNewTeam: false,
        teamId: 'team-1',
        teamStatus: 'confirmed',
      },
    ],
    teams: [
      {
        teamId: 'team-1',
        teamSlotIndex: 1,
        teamSlotType: 'public',
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

function openManagementPanel() {
  fireEvent.click(screen.getByRole('button', { name: 'Управление игрой' }))
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
  pageMocks.useActiveGameTeam.mockReturnValue({
    isSelectingActiveTeam: false,
    selectActiveTeam: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  pageMocks.useManualQuizAward.mockReturnValue({
    isAwardingManualQuizPoints: false,
    awardManualQuizPoints: vi.fn(),
    toastMessage: null,
    toastSeverity: 'success',
    dismissToast: vi.fn(),
  })
  pageMocks.useManualQuizAwardPlayers.mockReturnValue({
    players: [],
    isLoading: false,
    isError: false,
  })
  pageMocks.useGameTeamPlayedState.mockReturnValue({
    isUpdatingPlayedState: false,
    updatingTeamId: null,
    setTeamPlayedState: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  pageMocks.useStartGameRound.mockReturnValue({
    isChangingRoundStage: false,
    startRound: vi.fn(),
    reviewRound: vi.fn(),
    completeRound: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  pageMocks.useCardPlayResult.mockReturnValue({
    round: null,
    isLoading: false,
    isError: false,
  })
  pageMocks.useGameBoardCellResults.mockReturnValue({
    playResultsByCellId: new Map(),
    isLoading: false,
    isError: false,
  })
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
    expect(screen.getByRole('button', { name: 'Открыть очередь команд' })).toBeInTheDocument()
    expect(screen.queryByRole('complementary', { name: 'Очередь команд' })).not.toBeInTheDocument()
    expect(screen.getByTestId('game-board-grid')).toBeInTheDocument()
    expect(screen.queryByText(/модификатор/i)).not.toBeInTheDocument()
    expect(screen.queryByText('Активна')).not.toBeInTheDocument()
    expect(
      screen.getByText(
        'Текущий шаг: выберите активную команду перед открытием следующей карточки.',
      ),
    ).toBeInTheDocument()
  })

  it('renders team queue and highlights the active round team', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        activeRound: {
          roundId: 'round-1',
          teamId: 'team-2',
          teamSlotIndex: 2,
          baseScore: 120,
          emptyCardPenaltyApplied: false,
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
          {
            teamId: 'team-2',
            teamSlotIndex: 2,
            participants: [
              {
                userId: 'user-2',
                displayName: 'Player Two',
              },
              {
                userId: 'user-3',
                displayName: 'Player Three',
              },
            ],
          },
        ],
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    const boardCard = screen.getByTestId('game-board-grid').closest('.MuiPaper-root')
    expect(boardCard).not.toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Открыть очередь команд' }))

    const queuePanel = screen.getByRole('complementary', { name: 'Очередь команд' })
    expect(queuePanel).toBeInTheDocument()
    expect(within(queuePanel).getByText('Команда #1')).toBeInTheDocument()
    expect(within(queuePanel).getByText('Команда #2')).toBeInTheDocument()
    expect(within(queuePanel).getByText('Player One')).toBeInTheDocument()
    expect(within(queuePanel).getByText('Player Two')).toBeInTheDocument()
    expect(within(queuePanel).getByText('Player Three')).toBeInTheDocument()
    expect(within(queuePanel).getByText('Играет')).toBeInTheDocument()
    expect(within(boardCard as HTMLElement).getByText('Играет')).toBeInTheDocument()
    expect(screen.getByText('Идёт раунд: команда #2, база 120')).toBeInTheDocument()
    expect(within(boardCard as HTMLElement).getByText('Команда #2')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Закрыть очередь команд' }))
    expect(screen.queryByRole('complementary', { name: 'Очередь команд' })).not.toBeInTheDocument()
  })

  it('shows the active team banner above the board', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-2',
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
          {
            teamId: 'team-2',
            teamSlotIndex: 2,
            participants: [
              {
                userId: 'user-2',
                displayName: 'Player Two',
              },
              {
                userId: 'user-3',
                displayName: 'Player Three',
              },
            ],
          },
        ],
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    const boardCard = screen.getByTestId('game-board-grid').closest('.MuiPaper-root')
    expect(boardCard).not.toBeNull()
    expect(within(boardCard as HTMLElement).getByText('Активная команда')).toBeInTheDocument()
    expect(within(boardCard as HTMLElement).getByText('Команда #2')).toBeInTheDocument()
    expect(within(boardCard as HTMLElement).getByText('Player Two')).toBeInTheDocument()
    expect(within(boardCard as HTMLElement).getByText('Player Three')).toBeInTheDocument()
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

  it('shows the live round phase above the board for regular users', () => {
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          cellId: 'cell-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          status: 'awaiting_modifiers',
          baseScore: 100,
          emptyCardPenaltyApplied: false,
        },
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    expect(
      screen.getByText(
        'Сейчас открыто окно модификаторов. Дайте игрокам активировать их, затем начните раунд.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('Активировать модификаторы')).toBeInTheDocument()
  })

  it('opens the management panel from the game board when available', () => {
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
        canManageGame: true,
        canStartGame: true,
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
    expect(screen.getByRole('button', { name: 'Управление игрой' })).toBeInTheDocument()

    openManagementPanel()

    const managementPanel = screen.getByRole('complementary', { name: 'Управление игрой' })
    expect(managementPanel).toBeInTheDocument()
    expect(
      within(managementPanel).getByText(
        'Сначала запустите игру в секции запуска. После этого можно назначать активную команду и начинать цикл раунда.',
      ),
    ).toBeInTheDocument()
    expect(within(managementPanel).getAllByText('Запуск')[0]).toBeInTheDocument()
    expect(
      screen.getByText('Перед стартом пройдите финальные проверки регистрации.'),
    ).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Запуск игры' }))
    expect(screen.getByRole('heading', { name: 'Запуск игры' })).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Запустить игру' }))
    expect(screen.getByText('Запустить игру?')).toBeInTheDocument()

    const launchButtons = screen.getAllByRole('button', { name: 'Запустить игру' })
    fireEvent.click(launchButtons[launchButtons.length - 1])

    expect(startGame).toHaveBeenCalled()
  })

  it('shows blocked launch state inside the management panel action', () => {
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
        canManageGame: true,
        canStartGame: true,
        shouldRender: true,
        snapshot: createAdminRegistrationSnapshot({
          launchSummary: {
            canStartGame: false,
            confirmedTeamsCount: 0,
            formingTeamsCount: 0,
            pendingInvitationsCount: 0,
            disbandRequestsCount: 0,
            invalidConfirmedRostersCount: 0,
          },
          teamSlots: [],
          teams: [],
        }),
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    openManagementPanel()

    expect(screen.getByRole('complementary', { name: 'Управление игрой' })).toBeInTheDocument()
    expect(screen.getByText('Блокеров: 1')).toBeInTheDocument()
  })

  it('shows management status without launch action for moderators', () => {
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
        canManageGame: true,
        canStartGame: false,
      }),
    )

    renderWithAppProviders(
      <MemoryRouter>
        <GameBoardPage />
      </MemoryRouter>,
    )

    openManagementPanel()

    expect(screen.getByRole('complementary', { name: 'Управление игрой' })).toBeInTheDocument()
    expect(screen.getByText('Запустить игру может только администратор.')).toBeInTheDocument()
    expect(screen.getByText('Сейчас нет запущенного раунда.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Запуск игры' })).not.toBeInTheDocument()
  })

  it('opens the round summary dialog and submits manual results', async () => {
    const completeRound = vi.fn().mockResolvedValue(undefined)
    const selectActiveTeam = vi.fn().mockResolvedValue(undefined)
    pageMocks.useActiveGameTeam.mockReturnValue({
      isSelectingActiveTeam: false,
      selectActiveTeam,
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          cellId: 'cell-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          status: 'reviewing_results',
          baseScore: 100,
          emptyCardPenaltyApplied: false,
          killsCount: 0,
          bountyCount: 0,
          participants: [
            {
              userId: 'user-1',
              displayName: 'Player One',
            },
          ],
          modifierResults: [
            {
              modifierResultId: 'modifier-result-1',
              modifierId: 'modifier-1',
              modifierName: 'Меткий глаз',
              outcomeStatus: 'pending',
              scoreDelta: 20,
              killDelta: 1,
              multiplierApplied: null,
              resolutionDataJson: null,
            },
          ],
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )
    pageMocks.useStartGameRound.mockReturnValue({
      isChangingRoundStage: false,
      startRound: vi.fn(),
      reviewRound: vi.fn(),
      completeRound,
      toastMessage: null,
      dismissToast: vi.fn(),
    })

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    fireEvent.click(screen.getByRole('button', { name: 'Заполнить итоги раунда' }))

    expect(screen.getByRole('heading', { name: 'Итоги раунда' })).toBeInTheDocument()
    expect(screen.getByText('Меткий глаз')).toBeInTheDocument()
    expect(screen.getByText('Игроки: Player One')).toBeInTheDocument()

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Убитые враги' }), {
      target: { value: '3' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Вынесенные награды' }), {
      target: { value: '2' },
    })
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Изменение очков' }), {
      target: { value: '50' },
    })

    fireEvent.click(screen.getByRole('button', { name: 'Завершить раунд' }))

    await waitFor(() =>
      expect(completeRound).toHaveBeenCalledWith({
        roundId: 'round-1',
        killsCount: 3,
        bountyCount: 2,
        modifierResults: [
          {
            modifierResultId: 'modifier-result-1',
            outcomeStatus: 'completed',
            countValue: null,
            isConditionMet: null,
            manualScoreDelta: 50,
            manualKillDelta: 1,
            resolutionDataJson: null,
          },
        ],
      }),
    )
    await waitFor(() => expect(selectActiveTeam).toHaveBeenCalledWith('team-1'))
  })

  it('marks the team as played when the operator finishes it in the round summary', async () => {
    const completeRound = vi.fn().mockResolvedValue(undefined)
    const setTeamPlayedState = vi.fn().mockResolvedValue(undefined)
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          cellId: 'cell-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          status: 'reviewing_results',
          baseScore: 100,
          emptyCardPenaltyApplied: false,
          killsCount: 0,
          bountyCount: 0,
          participants: [
            {
              userId: 'user-1',
              displayName: 'Player One',
            },
          ],
          modifierResults: [],
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )
    pageMocks.useStartGameRound.mockReturnValue({
      isChangingRoundStage: false,
      startRound: vi.fn(),
      reviewRound: vi.fn(),
      completeRound,
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameTeamPlayedState.mockReturnValue({
      isUpdatingPlayedState: false,
      updatingTeamId: null,
      setTeamPlayedState,
      toastMessage: null,
      dismissToast: vi.fn(),
    })

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    fireEvent.click(screen.getByRole('button', { name: 'Заполнить итоги раунда' }))
    fireEvent.click(screen.getByRole('button', { name: /Команда закончила игру/i }))
    fireEvent.click(screen.getByRole('button', { name: 'Завершить раунд' }))

    await waitFor(() =>
      expect(setTeamPlayedState).toHaveBeenCalledWith({
        teamId: 'team-1',
        isPlayed: true,
      }),
    )
  })

  it('lets staff select the active team from the management panel during an active game', () => {
    const selectActiveTeam = vi.fn()
    pageMocks.useActiveGameTeam.mockReturnValue({
      isSelectingActiveTeam: false,
      selectActiveTeam,
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: null,
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
        canStartGame: false,
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    const managementPanel = screen.getByRole('complementary', { name: 'Управление игрой' })

    expect(
      screen.getByText('Выберите активную команду, прежде чем открывать карточки.'),
    ).toBeInTheDocument()
    fireEvent.click(
      within(managementPanel).getByText('Команда #1').closest('button') as HTMLElement,
    )

    expect(selectActiveTeam).toHaveBeenCalledWith('team-1')
  })

  it('blocks team selection while a played-state update is pending', () => {
    const selectActiveTeam = vi.fn()
    pageMocks.useActiveGameTeam.mockReturnValue({
      isSelectingActiveTeam: false,
      selectActiveTeam,
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameTeamPlayedState.mockReturnValue({
      isUpdatingPlayedState: true,
      updatingTeamId: 'team-1',
      setTeamPlayedState: vi.fn(),
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: null,
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    const managementPanel = screen.getByRole('complementary', { name: 'Управление игрой' })
    const teamButton = within(managementPanel).getByText('Команда #1').closest('button')

    expect(teamButton).toBeDisabled()
    fireEvent.click(teamButton as HTMLElement)
    expect(selectActiveTeam).not.toHaveBeenCalled()
  })

  it('locks active team selection while a round is in progress', () => {
    const selectActiveTeam = vi.fn()
    pageMocks.useActiveGameTeam.mockReturnValue({
      isSelectingActiveTeam: false,
      selectActiveTeam,
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          baseScore: 100,
          emptyCardPenaltyApplied: false,
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
          {
            teamId: 'team-2',
            teamSlotIndex: 2,
            participants: [
              {
                userId: 'user-2',
                displayName: 'Player Two',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    const managementPanel = screen.getByRole('complementary', { name: 'Управление игрой' })

    expect(
      screen.getByText('Завершите текущий раунд, прежде чем менять активную команду.'),
    ).toBeInTheDocument()

    const nextTeamButton = within(managementPanel).getByText('Команда #2').closest('button')
    expect(nextTeamButton).toBeDisabled()
    fireEvent.click(nextTeamButton as HTMLElement)

    expect(selectActiveTeam).not.toHaveBeenCalled()
  })

  it('lets staff manually award quiz points from the management panel', () => {
    const awardManualQuizPoints = vi.fn()
    pageMocks.useManualQuizAward.mockReturnValue({
      isAwardingManualQuizPoints: false,
      awardManualQuizPoints,
      toastMessage: null,
      toastSeverity: 'success',
      dismissToast: vi.fn(),
    })
    pageMocks.useManualQuizAwardPlayers.mockReturnValue({
      players: [
        {
          userId: 'user-1',
          login: 'player_one',
          displayName: 'Player One',
        },
      ],
      isLoading: false,
      isError: false,
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        teamQueue: [
          {
            teamId: 'team-1',
            teamSlotIndex: 1,
            participants: [
              {
                userId: 'user-1',
                displayName: 'Player One',
              },
            ],
          },
        ],
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    fireEvent.click(screen.getByRole('button', { name: /Ручное начисление очков викторины/i }))

    fireEvent.change(screen.getByRole('combobox', { name: 'Игрок' }), {
      target: { value: 'player' },
    })
    fireEvent.click(screen.getByRole('option', { name: /Player One · player_one/i }))
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Очки' }), {
      target: { value: '7' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Начислить очки' }))

    expect(awardManualQuizPoints).toHaveBeenCalledWith({
      awardedToUserId: 'user-1',
      points: 7,
    })
  })

  it('does not show the management panel when it is unavailable for the current user', () => {
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

    expect(screen.queryByRole('button', { name: 'Управление игрой' })).not.toBeInTheDocument()
  })

  it('starts the opened round while it is waiting for modifiers', () => {
    const startRound = vi.fn()
    pageMocks.useStartGameRound.mockReturnValue({
      isChangingRoundStage: false,
      startRound,
      reviewRound: vi.fn(),
      completeRound: vi.fn(),
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          cellId: 'cell-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          status: 'awaiting_modifiers',
          baseScore: 100,
          emptyCardPenaltyApplied: false,
        },
      }),
    )
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    expect(
      screen.getByText(
        'Шаг 3: дайте зрителям прожать модификаторы для этой команды. Когда всё готово, запускайте раунд.',
      ),
    ).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Начать раунд' }))

    expect(startRound).toHaveBeenCalledWith({
      cellId: 'cell-1',
      teamId: 'team-1',
    })
  })

  it('moves the active round to review stage', () => {
    const reviewRound = vi.fn()
    pageMocks.useStartGameRound.mockReturnValue({
      isChangingRoundStage: false,
      startRound: vi.fn(),
      reviewRound,
      completeRound: vi.fn(),
      toastMessage: null,
      dismissToast: vi.fn(),
    })
    pageMocks.useGameBoardLaunchPanel.mockReturnValue(
      createLaunchPanelState({
        canManageGame: true,
      }),
    )
    pageMocks.useGameBoardPage.mockReturnValue(
      createPageQuery({
        data: {
          ...readySnapshot,
          status: 'active',
          activeTeamId: 'team-1',
        },
        activeRound: {
          roundId: 'round-1',
          cellId: 'cell-1',
          teamId: 'team-1',
          teamSlotIndex: 1,
          status: 'in_progress',
          baseScore: 100,
          emptyCardPenaltyApplied: false,
        },
      }),
    )

    renderWithAppProviders(<GameBoardPage />)

    openManagementPanel()

    fireEvent.click(screen.getByRole('button', { name: 'Подвести итоги' }))
    expect(reviewRound).toHaveBeenCalledWith('round-1')
  })
})
