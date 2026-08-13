import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
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
  cancelGameModifierActivation: vi.fn(),
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
    cancelGameModifierActivation: apiMocks.cancelGameModifierActivation,
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
  apiMocks.cancelGameModifierActivation.mockResolvedValue(undefined)
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
    const openButton = screen.getByRole('button', { name: 'Панель администратора' })
    expect(openButton).toHaveStyle({ position: 'fixed' })
    fireEvent.click(openButton)

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

  it('counts every activation, shows earned points, and closes the refund dialog after success', async () => {
    let activations = [
      {
        activationId: 'activation-1',
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'player-1',
        activatedByDisplayName: 'Player One',
        activationCost: 3,
        activatedAtUtc: '2026-08-13T18:00:00Z',
      },
      {
        activationId: 'activation-2',
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'player-1',
        activatedByDisplayName: 'Player One',
        activationCost: 3,
        activatedAtUtc: '2026-08-13T18:05:00Z',
      },
    ]
    apiMocks.fetchAdminActiveGameModifierActivations.mockImplementation(async () => activations)
    apiMocks.cancelGameModifierActivation.mockImplementation(async (activationId: string) => {
      activations = activations.filter((activation) => activation.activationId !== activationId)
    })

    renderPanel()
    fireEvent.click(screen.getByRole('button', { name: 'Панель администратора' }))

    expect(await screen.findByText('Использовано: 2')).toBeInTheDocument()
    const earnedMetric = screen.getByText('Заработано игроками за игру').parentElement
    expect(earnedMetric).not.toBeNull()
    expect(within(earnedMetric as HTMLElement).getByText('20 очк.')).toBeInTheDocument()
    const usedMetric = screen.getByText('Всего использовано модификаторов').parentElement
    expect(usedMetric).not.toBeNull()
    expect(within(usedMetric as HTMLElement).getByText('2')).toBeInTheDocument()

    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Какой модификатор отменить' }))
    fireEvent.click(await screen.findByRole('option', { name: 'Расходники' }))
    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Какая активация' }))
    fireEvent.click((await screen.findAllByRole('option'))[0] as HTMLElement)
    fireEvent.click(screen.getByRole('button', { name: 'Отменить и вернуть очки' }))

    const confirmDialog = screen.getByRole('dialog', { name: 'Отменить эту активацию?' })
    fireEvent.click(within(confirmDialog).getByRole('button', { name: 'Отменить и вернуть очки' }))

    await waitFor(() => expect(apiMocks.cancelGameModifierActivation).toHaveBeenCalledTimes(1))
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Отменить эту активацию?' }),
      ).not.toBeInTheDocument(),
    )
    expect(await screen.findByText('Использовано: 1')).toBeInTheDocument()
    expect(within(usedMetric as HTMLElement).getByText('1')).toBeInTheDocument()
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
        <AdminModifierPanel />
      </AuthContext.Provider>
    </QueryClientProvider>,
  )

  return {
    ...rendered,
    queryClient,
  }
}
