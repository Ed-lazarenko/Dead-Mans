import { cleanup, fireEvent, screen, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { AuthContext, type AuthContextValue } from '../../shared/auth/auth-context.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { AdminModifierPanel } from './AdminModifierPanel.tsx'

const apiMocks = vi.hoisted(() => ({
  fetchAdminGameModifierPlayers: vi.fn(),
  fetchAdminGameModifierState: vi.fn(),
  fetchAdminActiveGameModifierActivations: vi.fn(),
}))

vi.mock('./api/game-modifiers-api.ts', async () => {
  const actual = await vi.importActual<typeof import('./api/game-modifiers-api.ts')>(
    './api/game-modifiers-api.ts',
  )

  return {
    ...actual,
    fetchAdminGameModifierPlayers: apiMocks.fetchAdminGameModifierPlayers,
    fetchAdminGameModifierState: apiMocks.fetchAdminGameModifierState,
    fetchAdminActiveGameModifierActivations: apiMocks.fetchAdminActiveGameModifierActivations,
  }
})

const adminAuthContext: AuthContextValue = {
  user: {
    id: 'admin-1',
    displayName: 'Admin',
    roles: ['admin'],
  },
  authStatus: 'authenticated',
  isAuthenticated: true,
  startTwitchLogin: vi.fn(),
  logout: vi.fn().mockResolvedValue(undefined),
  refreshSession: vi.fn().mockResolvedValue(true),
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  apiMocks.fetchAdminGameModifierPlayers.mockResolvedValue({
    players: [
      {
        userId: 'player-1',
        login: 'player_one',
        displayName: 'Player One',
        availableQuizPoints: 17,
      },
    ],
    summary: {
      playersCount: 1,
      totalAvailableQuizPoints: 17,
      totalEarnedQuizPoints: 20,
      totalSpentQuizPoints: 3,
    },
  })
  apiMocks.fetchAdminActiveGameModifierActivations.mockResolvedValue([])
  apiMocks.fetchAdminGameModifierState.mockResolvedValue({
    availableQuizPoints: 17,
    spentQuizPoints: 3,
    earnedQuizPoints: 20,
    isOrderingOpen: true,
    activeModifiers: [],
    availableModifiers: [
      {
        modifier: {
          id: 'modifier-1',
          scoringType: 'non_scoring',
          category: 'round',
          requiresHostControl: false,
          mechanicType: 'rule_only',
          name: 'Расходники',
          description: 'Описание модификатора',
          activationCost: 3,
          defaultLimitPerGame: 3,
          activationLimit: { count: 3 },
          effect: {
            mechanicType: 'rule_only',
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
          iconEmoji: null,
          activationCommand: null,
        },
        isActive: false,
        canActivate: true,
        blockedReason: null,
        activationsCount: 0,
        limit: 3,
      },
    ],
  })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('AdminModifierPanel quick wins', () => {
  it('omits the player counter and redundant instruction', async () => {
    renderPanel()
    fireEvent.click(screen.getByRole('button', { name: 'Панель администратора' }))

    expect(await screen.findByRole('combobox', { name: 'Игрок' })).toHaveValue('Player One')

    expect(screen.getByRole('heading', { name: 'Добавить модификатор' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Какой модификатор отменить' })).toBeInTheDocument()

    expect(screen.queryByText('1 игроков')).not.toBeInTheDocument()
    expect(
      screen.queryByText(
        'Отдельно добавляйте модификатор игроку или отменяйте неиспользованную активацию с возвратом очков.',
      ),
    ).not.toBeInTheDocument()
  })

  it('shows only the modifier name and price in the modifier dropdown', async () => {
    renderPanel()
    fireEvent.click(screen.getByRole('button', { name: 'Панель администратора' }))
    expect(await screen.findByRole('combobox', { name: 'Игрок' })).toHaveValue('Player One')

    const modifierSelect = await screen.findByRole('combobox', { name: 'Модификатор' })
    fireEvent.mouseDown(modifierSelect)

    const listbox = await screen.findByRole('listbox')
    const option = within(listbox).getByRole('option')
    expect(option).toHaveTextContent('Расходники')
    expect(option).toHaveTextContent('3 очк.')
    expect(within(option).queryByText('Во время раунда')).not.toBeInTheDocument()
    expect(within(option).queryByText('Не участвует в итогах')).not.toBeInTheDocument()
  })
})

function renderPanel() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  const rendered = renderWithAppProviders(
    <QueryClientProvider client={queryClient}>
      <AuthContext.Provider value={adminAuthContext}>
        <AdminModifierPanel enabledModifiersCount={1} />
      </AuthContext.Provider>
    </QueryClientProvider>,
  )

  return {
    ...rendered,
    queryClient,
  }
}
