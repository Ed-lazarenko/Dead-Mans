import { cleanup, screen } from '@testing-library/react'
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

    expect(screen.getAllByText('Расходники')).toHaveLength(2)
    expect(screen.getByText('Player Three')).toBeInTheDocument()
    expect(screen.getByText('Player Two')).toBeInTheDocument()
    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.queryByText('Текущий игрок')).not.toBeInTheDocument()
    expect(screen.queryByText(/Последний:/)).not.toBeInTheDocument()
    expect(screen.queryByText('Что уже действует прямо сейчас.')).not.toBeInTheDocument()
    expect(
      screen.queryByText('Выберите следующий модификатор без отдельного экрана деталей.'),
    ).not.toBeInTheDocument()
  })
})
