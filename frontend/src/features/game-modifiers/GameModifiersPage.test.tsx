import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { useQuery } from '@tanstack/react-query'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { AuthContext, type AuthContextValue } from '../../shared/auth/auth-context.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import { GameModifiersPage } from './GameModifiersPage.tsx'
import { gameModifierStateQueryOptions } from './api/game-modifier-queries.ts'

const modifierMocks = vi.hoisted(() => ({
  useActivateGameModifier: vi.fn(),
}))

vi.mock('@tanstack/react-query', async () => {
  const actual =
    await vi.importActual<typeof import('@tanstack/react-query')>('@tanstack/react-query')

  return {
    ...actual,
    useQuery: vi.fn(),
  }
})

vi.mock('./use-activate-game-modifier.ts', () => ({
  useActivateGameModifier: modifierMocks.useActivateGameModifier,
}))

vi.mock('./AdminModifierPanel.tsx', () => ({
  AdminModifierPanel: () => null,
}))

const mockedUseQuery = vi.mocked(useQuery)

const authContextValue: AuthContextValue = {
  user: {
    id: '11111111-1111-4111-8111-111111111111',
    displayName: 'Player One',
    roles: ['viewer'],
  },
  authStatus: 'authenticated',
  isAuthenticated: true,
  startTwitchLogin: vi.fn(),
  logout: vi.fn().mockResolvedValue(undefined),
  refreshSession: vi.fn().mockResolvedValue(true),
}

function renderGameModifiersPage() {
  return renderWithAppProviders(
    <AuthContext.Provider value={authContextValue}>
      <GameModifiersPage />
    </AuthContext.Provider>,
  )
}

function createState() {
  return {
    availableQuizPoints: 24,
    spentQuizPoints: 9,
    earnedQuizPoints: 33,
    isOrderingOpen: true,
    activeModifiers: [
      {
        activationId: 'activation-1',
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'user-1',
        activatedByDisplayName: 'Player One',
        activationCost: 3,
        activatedAtUtc: '2026-07-21T18:01:00Z',
      },
      {
        activationId: 'activation-2',
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'user-2',
        activatedByDisplayName: 'Player Two',
        activationCost: 3,
        activatedAtUtc: '2026-07-21T18:02:00Z',
      },
      {
        activationId: 'activation-3',
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'user-3',
        activatedByDisplayName: 'Player Three',
        activationCost: 3,
        activatedAtUtc: '2026-07-21T18:03:00Z',
      },
    ],
    availableModifiers: [
      {
        modifier: {
          id: 'modifier-1',
          scoringType: 'non_scoring',
          category: 'round' as const,
          requiresHostControl: false,
          mechanicType: 'rule_only' as const,
          name: 'Расходники',
          description: 'Описание модификатора',
          activationCost: 3,
          defaultLimitPerGame: 3,
          activationLimit: { count: 3 },
          effect: {
            mechanicType: 'rule_only' as const,
            traits: [],
            durationSeconds: null,
            ruleText: null,
            scoreImpact: null,
            conditions: [],
            resolutionInputs: [],
            killEffect: null,
            multiplierEffect: null,
            mentorEffect: null,
          },
          conflictingModifierIds: [],
          iconEmoji: '🧰',
          activationCommand: null,
        },
        isActive: true,
        canActivate: true,
        blockedReason: null,
        activationsCount: 3,
        limit: 3,
      },
    ],
  }
}

const currentSnapshot = {
  gameId: 'game-1',
  title: 'Тестовая игра',
  status: 'active' as const,
  version: 1,
  rows: 1,
  cols: 1,
  rowLabels: ['Сложность'],
  colLabels: ['Категория'],
  cells: [
    {
      id: 'cell-1',
      row: 0,
      col: 0,
      cellType: 'regular',
      title: 'Битва в порту',
      description: null,
      cost: 500,
      state: 'open' as const,
      media: [],
    },
  ],
  enabledModifierIds: ['modifier-1'],
  activeModifiers: [],
  activeTeamId: 'team-1',
}

const currentRound = {
  roundId: 'round-1',
  gameId: 'game-1',
  cellId: 'cell-1',
  teamId: 'team-1',
  teamName: 'Морские волки',
  teamSlotIndex: 2,
  status: 'awaiting_modifiers',
  startedAtUtc: '2026-08-14T18:00:00Z',
  finishedAtUtc: null,
  baseScore: 0,
  finalScore: null,
  emptyCardPenaltyApplied: false,
  scoreDetails: {
    baseScore: 0,
    bountyScore: 0,
    modifierScore: 0,
    penaltyTotal: 0,
    finalScore: 0,
  },
  killsCount: 0,
  bountyCount: 0,
  notes: null,
  participants: [
    { userId: 'team-user-1', displayName: 'Капитан Флинт' },
    { userId: 'team-user-2', displayName: 'Энн Бонни' },
  ],
  modifierResults: [],
}

function mockPageQueries({
  modifierState = createState(),
  snapshot = currentSnapshot,
  activeRound = currentRound,
}: {
  modifierState?: ReturnType<typeof createState> | null
  snapshot?: typeof currentSnapshot | null
  activeRound?: typeof currentRound | null
} = {}) {
  mockedUseQuery.mockImplementation((options) => {
    const queryKey = options.queryKey
    const data =
      queryKey === gameModifierStateQueryOptions.queryKey
        ? modifierState
        : queryKey === currentGameBoardQueryOptions.queryKey
          ? snapshot
          : queryKey === activeGameRoundQueryOptions.queryKey
            ? activeRound
            : undefined

    return {
      isLoading: false,
      isError: false,
      data,
    } as ReturnType<typeof useQuery>
  })
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  mockPageQueries()
  modifierMocks.useActivateGameModifier.mockReturnValue({
    isActivating: false,
    pendingModifierId: null,
    activate: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameModifiersPage', () => {
  it('shows no active game state without treating it as a load error', () => {
    mockPageQueries({ modifierState: null, snapshot: null, activeRound: null })

    renderGameModifiersPage()

    expect(
      screen.getByText('Активной игры нет. Модификаторы появятся после старта.'),
    ).toBeInTheDocument()
    expect(screen.queryByText('Не удалось загрузить модификаторы.')).not.toBeInTheDocument()
  })

  it('shows the current team and active card in the existing summary', () => {
    renderGameModifiersPage()

    const summary = screen.getByRole('region', { name: 'Краткая сводка' })
    expect(within(summary).getByRole('status')).toHaveTextContent('Заказ открыт')
    expect(screen.getByTestId('modifier-summary-row')).toHaveStyle({
      display: 'flex',
      flexDirection: 'row',
      flexWrap: 'nowrap',
    })
    expect(within(summary).getByText('Текущая команда')).toBeInTheDocument()
    expect(within(summary).getByText('Морские волки')).toBeInTheDocument()
    expect(within(summary).queryByText('Участники команды')).not.toBeInTheDocument()
    expect(within(summary).getByText('Капитан Флинт')).toBeInTheDocument()
    expect(within(summary).getByText('Энн Бонни')).toBeInTheDocument()
    expect(within(summary).getByText('Энн Бонни')).toHaveStyle({ borderLeftWidth: '1px' })
    expect(within(summary).getByText('Активная карточка')).toBeInTheDocument()
    expect(within(summary).getByText('Битва в порту')).toBeInTheDocument()
    expect(within(summary).getByText('Посмотреть карточку')).toBeInTheDocument()
    expect(within(summary).getByRole('list')).toHaveStyle({ flexDirection: 'row' })

    const pointsMetric = within(summary).getByText('Доступно очков').parentElement
    expect(pointsMetric).toHaveStyle({
      alignItems: 'center',
      justifyContent: 'center',
      textAlign: 'center',
    })
    const viewCardButton = within(summary).getByRole('button', { name: 'Посмотреть карточку' })
    expect(viewCardButton).toHaveStyle({ minWidth: '148px' })
    expect(viewCardButton.parentElement).toHaveStyle({ paddingTop: '4.8px' })

    const summaryText = summary.textContent ?? ''
    expect(summaryText.indexOf('Краткая сводка')).toBeLessThan(
      summaryText.indexOf('Доступно очков'),
    )
    expect(summaryText.indexOf('Краткая сводка')).toBeLessThan(
      summaryText.indexOf('Текущая команда'),
    )
    expect(summaryText.indexOf('Текущая команда')).toBeLessThan(
      summaryText.indexOf('Активная карточка'),
    )
  })

  it('opens the active card in the shared card preview dialog', () => {
    renderGameModifiersPage()

    fireEvent.click(screen.getByRole('button', { name: 'Посмотреть карточку' }))

    const dialog = screen.getByRole('dialog', { name: 'Битва в порту' })
    expect(dialog).toBeInTheDocument()
    expect(within(dialog).getByText('У этой карточки нет прикреплённых медиа.')).toBeInTheDocument()
    expect(
      within(dialog).getByText('Карточка открыта, но итоги раунда ещё не подведены.'),
    ).toBeInTheDocument()
  })

  it('shows neutral round context when no card is active', () => {
    mockPageQueries({ activeRound: null })

    renderGameModifiersPage()

    const summary = screen.getByRole('region', { name: 'Краткая сводка' })
    expect(within(summary).getByText('Не выбрана')).toBeInTheDocument()
    expect(within(summary).getByText('Участники не указаны')).toBeInTheDocument()
    expect(within(summary).getByText('Не открыта')).toBeInTheDocument()
    expect(within(summary).queryByRole('button', { name: 'Посмотреть карточку' })).toBeNull()
  })

  it('shows grouped activator display names for regular users', () => {
    renderGameModifiersPage()

    expect(screen.getByTestId('game-modifiers-page')).toBeInTheDocument()
    expect(screen.getAllByText('Расходники')).toHaveLength(2)
    expect(screen.getByText('Player Three')).toBeInTheDocument()
    expect(screen.getByText('Player Two')).toBeInTheDocument()
    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.getByText('Активировали')).toBeInTheDocument()
    expect(screen.getByText('Потрачено вами')).toBeInTheDocument()
    expect(screen.getByText('Потрачено за раунд')).toBeInTheDocument()
    expect(screen.getAllByText('9 очк.')).toHaveLength(2)
    expect(screen.getAllByText('Активны в этой игре')).toHaveLength(1)
    expect(screen.getByText('3 модификатора')).toBeInTheDocument()
    expect(screen.getAllByText('1 модификатор')).toHaveLength(1)
    expect(screen.queryByText('1 модификаторов')).not.toBeInTheDocument()
    expect(screen.queryByText('Текущий игрок')).not.toBeInTheDocument()
    expect(screen.queryByText(/Последний:/)).not.toBeInTheDocument()
    expect(screen.queryByText('Что уже действует прямо сейчас.')).not.toBeInTheDocument()
    expect(
      screen.queryByText('Выберите следующий модификатор без отдельного экрана деталей.'),
    ).not.toBeInTheDocument()
  })

  it('uses correct Russian modifier count forms', () => {
    expect(i18n.t('gameModifiers.categoryCountLabel', { count: 1 })).toBe('1 модификатор')
    expect(i18n.t('gameModifiers.categoryCountLabel', { count: 2 })).toBe('2 модификатора')
    expect(i18n.t('gameModifiers.categoryCountLabel', { count: 5 })).toBe('5 модификаторов')
  })

  it('explains every summary metric in plain language', () => {
    renderGameModifiersPage()

    const metrics = [
      {
        label: 'Доступно очков',
        tooltip:
          'Очки викторины, которые вы можете потратить сейчас: заработанные за эту игру очки минус ваши расходы на модификаторы.',
      },
      {
        label: 'Потрачено вами',
        tooltip: 'Все очки викторины, которые вы потратили на модификаторы за текущую игру.',
      },
      {
        label: 'Потрачено за раунд',
        tooltip:
          'Сумма стоимости всех модификаторов, активных в текущем раунде, независимо от того, кто их активировал.',
      },
      {
        label: 'Краткая сводка',
        tooltip:
          'Показывает, можно ли сейчас заказывать модификаторы. Заказ открыт только в нужной фазе раунда.',
      },
      {
        label: 'Текущая команда',
        tooltip: 'Команда, которая сейчас играет, и участники этого раунда.',
      },
      {
        label: 'Активная карточка',
        tooltip:
          'Карточка, которая сейчас разыгрывается. Нажмите «Посмотреть карточку», чтобы открыть её полностью.',
      },
    ]

    for (const metric of metrics) {
      const metricElement = screen.getByText(metric.label).parentElement
      if (!metricElement) {
        throw new Error(`Metric container not found: ${metric.label}`)
      }

      expect(metricElement).toHaveAttribute('title', metric.tooltip)
    }

    for (const focusableLabel of [
      'Доступно очков',
      'Потрачено вами',
      'Потрачено за раунд',
      'Краткая сводка',
      'Текущая команда',
    ]) {
      expect(screen.getByText(focusableLabel).parentElement).toHaveAttribute('tabindex', '0')
    }
  })

  it('asks for confirmation before activating a modifier', async () => {
    renderGameModifiersPage()
    const activate = modifierMocks.useActivateGameModifier.mock.results.at(-1)?.value.activate

    fireEvent.click(screen.getByRole('button', { name: 'Активировать модификатор' }))

    expect(activate).not.toHaveBeenCalled()
    const dialog = screen.getByRole('dialog', { name: 'Активировать этот модификатор?' })
    expect(
      within(dialog).getByText('Активировать «Расходники» за 3 очк. викторины?'),
    ).toBeInTheDocument()
    fireEvent.click(within(dialog).getByRole('button', { name: 'Активировать модификатор' }))

    expect(activate).toHaveBeenCalledWith('modifier-1')
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Активировать этот модификатор?' }),
      ).not.toBeInTheDocument(),
    )
  })

  it('shows a compact ordering status with a detailed tooltip', async () => {
    const state = createState()
    state.isOrderingOpen = false
    const availability = state.availableModifiers[0]
    if (!availability) {
      throw new Error('Expected the base modifier fixture')
    }
    availability.canActivate = false
    availability.blockedReason = 'ordering_closed'
    mockPageQueries({ modifierState: state })

    renderGameModifiersPage()

    const summary = screen.getByRole('region', { name: 'Краткая сводка' })
    const orderingAlert = within(summary).getByRole('status')
    expect(orderingAlert).toHaveTextContent('Заказ закрыт')
    const orderingDescription = within(orderingAlert).getByText(
      'Сейчас не фаза заказа модификаторов.',
    )
    expect(orderingDescription).toHaveStyle({ color: 'rgba(232, 220, 200, 0.84)' })
    expect(
      screen.queryAllByText('Заказ закрыт: сейчас не фаза заказа модификаторов.'),
    ).toHaveLength(0)
    expect(screen.getAllByText('Сейчас не фаза заказа модификаторов.')).toHaveLength(1)
    const blockedButton = screen.getByRole('button', { name: 'Заказ закрыт' })
    expect(blockedButton).toBeDisabled()
    fireEvent.mouseOver(blockedButton.parentElement as HTMLElement)
    expect(await screen.findByRole('tooltip')).toHaveTextContent(
      'Заказ закрыт: сейчас не фаза заказа модификаторов.',
    )
  })

  it('shows a compact conflict status and names its cause in the tooltip and details', async () => {
    const state = createState()
    const baseAvailability = state.availableModifiers[0]
    if (!baseAvailability) {
      throw new Error('Expected the base modifier fixture')
    }

    state.availableModifiers.push({
      ...baseAvailability,
      modifier: {
        ...baseAvailability.modifier,
        id: 'modifier-2',
        name: 'Конфликтный модификатор',
        conflictingModifierIds: ['modifier-1'],
      },
      isActive: false,
      canActivate: false,
      blockedReason: 'conflict_active',
      activationsCount: 0,
    })
    mockPageQueries({ modifierState: state })

    renderGameModifiersPage()

    const blockedButton = screen.getByRole('button', { name: 'Есть конфликт' })
    expect(blockedButton).toBeDisabled()
    fireEvent.mouseOver(blockedButton.parentElement as HTMLElement)
    expect(await screen.findByRole('tooltip')).toHaveTextContent(
      'Заблокирован конфликтом с: Расходники',
    )
    const detailsButton = screen.getAllByRole('button', { name: 'Подробнее' }).at(-1)
    expect(detailsButton).toHaveAttribute('aria-expanded', 'false')
    fireEvent.click(detailsButton as HTMLElement)
    expect(detailsButton).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Конфликтует с: Расходники')).toBeInTheDocument()
  })

  it('shows a compact active-team status with a detailed tooltip', async () => {
    const state = createState()
    for (const availability of state.availableModifiers) {
      availability.canActivate = false
      availability.blockedReason = 'active_team_member'
    }
    mockPageQueries({ modifierState: state })

    renderGameModifiersPage()

    const blockedButton = screen.getByRole('button', { name: 'Ваша команда играет' })
    expect(blockedButton).toBeDisabled()
    fireEvent.mouseOver(blockedButton.parentElement as HTMLElement)
    expect(await screen.findByRole('tooltip')).toHaveTextContent(
      'Ваша команда сейчас играет этот раунд — активировать модификаторы для неё нельзя.',
    )
  })

  it.each([
    ['limit_reached', 'Лимит исчерпан', 'Лимит активаций исчерпан.'],
    ['insufficient_points', 'Не хватает очков', 'Не хватает очков викторины.'],
  ] as const)(
    'shows the compact %s status with its detailed tooltip',
    async (blockedReason, label, explanation) => {
      const state = createState()
      const availability = state.availableModifiers[0]
      if (!availability) {
        throw new Error('Expected the base modifier fixture')
      }
      availability.canActivate = false
      availability.blockedReason = blockedReason
      mockPageQueries({ modifierState: state })

      renderGameModifiersPage()

      const blockedButton = screen.getByRole('button', { name: label })
      expect(blockedButton).toBeDisabled()
      fireEvent.mouseOver(blockedButton.parentElement as HTMLElement)
      expect(await screen.findByRole('tooltip')).toHaveTextContent(explanation)
    },
  )
})
