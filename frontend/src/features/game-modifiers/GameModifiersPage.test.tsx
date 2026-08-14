import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { useQuery } from '@tanstack/react-query'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { AuthContext, type AuthContextValue } from '../../shared/auth/auth-context.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameModifiersPage } from './GameModifiersPage.tsx'

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

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  mockedUseQuery.mockReturnValue({
    isLoading: false,
    isError: false,
    data: createState(),
  } as ReturnType<typeof useQuery>)
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
    mockedUseQuery.mockReturnValue({
      isLoading: false,
      isError: false,
      data: null,
    } as ReturnType<typeof useQuery>)

    renderGameModifiersPage()

    expect(
      screen.getByText('Активной игры нет. Модификаторы появятся после старта.'),
    ).toBeInTheDocument()
    expect(screen.queryByText('Не удалось загрузить модификаторы.')).not.toBeInTheDocument()
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
    expect(screen.getByText('9 очк.')).toBeInTheDocument()
    expect(screen.getAllByText('Активны в этой игре')).toHaveLength(1)
    expect(screen.getByText('3 модификатора')).toBeInTheDocument()
    expect(screen.getAllByText('1 модификатор')).toHaveLength(2)
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

  it('explains that ordering is closed outside the modifier-ordering phase', () => {
    const state = createState()
    state.isOrderingOpen = false
    const availability = state.availableModifiers[0]
    if (!availability) {
      throw new Error('Expected the base modifier fixture')
    }
    availability.canActivate = false
    availability.blockedReason = 'ordering_closed'
    mockedUseQuery.mockReturnValue({
      isLoading: false,
      isError: false,
      data: state,
    } as ReturnType<typeof useQuery>)

    renderGameModifiersPage()

    const summary = screen.getByRole('region', { name: 'Краткая сводка' })
    expect(within(summary).getByRole('status')).toHaveTextContent(
      'Заказ закрыт: текущая игра находится не в фазе заказа модификаторов.',
    )
    expect(
      screen.queryAllByText('Заказ закрыт: сейчас не фаза заказа модификаторов.'),
    ).toHaveLength(0)
    expect(
      screen.getAllByText('Заказ закрыт: текущая игра находится не в фазе заказа модификаторов.'),
    ).toHaveLength(1)
    expect(screen.getByRole('button', { name: 'Активировать модификатор' })).toBeDisabled()
  })

  it('names the modifier that causes a conflict in the blocked state and details', () => {
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
    mockedUseQuery.mockReturnValue({
      isLoading: false,
      isError: false,
      data: state,
    } as ReturnType<typeof useQuery>)

    renderGameModifiersPage()

    expect(
      screen.getByRole('status', { name: 'Заблокирован конфликтом с: Расходники' }),
    ).toBeInTheDocument()
    const detailsButton = screen.getAllByRole('button', { name: 'Подробнее' }).at(-1)
    expect(detailsButton).toHaveAttribute('aria-expanded', 'false')
    fireEvent.click(detailsButton as HTMLElement)
    expect(detailsButton).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Конфликтует с: Расходники')).toBeInTheDocument()
  })
})
