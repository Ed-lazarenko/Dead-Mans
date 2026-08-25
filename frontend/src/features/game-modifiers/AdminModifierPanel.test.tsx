import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { AuthContext, type AuthContextValue } from '../../shared/auth/auth-context.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { AdminModifierTool } from './AdminModifierPanel.tsx'

const apiMocks = vi.hoisted(() => ({
  fetchAdminGameModifierPlayers: vi.fn(),
  fetchAdminGameModifierState: vi.fn(),
  fetchAdminActiveGameModifierActivations: vi.fn(),
  cancelGameModifierActivation: vi.fn(),
  emergencyDisableGameModifier: vi.fn(),
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
    emergencyDisableGameModifier: apiMocks.emergencyDisableGameModifier,
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
  apiMocks.emergencyDisableGameModifier.mockResolvedValue(undefined)
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
          category: 'round',
          name: 'Расходники',
          description: 'Описание модификатора',
          activationCost: 3,
          activationLimit: { count: 3 },
          conflictingModifierIds: [],
          iconEmoji: null,
          activationCommand: null,
          revision: 1,
          normalizedTags: [],
          behaviorV2: {
            schemaVersion: 2,
            kind: 'rule',
            phase: 'round',
            performer: 'activeTeam',
            requiresHostMonitoring: false,
            rule: 'Test rule',
            stackingPolicy: 'aggregateParameters',
            resolution: { type: 'ruleStatus' },
            reward: 'none',
            formulaReference: null,
          },
          isLockedByActiveGame: true,
        },
        isActive: false,
        canActivate: true,
        blockedReason: null,
        activationsCount: 0,
        limit: 3,
        isEmergencyDisabled: false,
        emergencyDisabledAtUtc: null,
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

    expect(await screen.findByRole('combobox', { name: 'Игрок' })).toHaveValue('Player One')

    expect(screen.getByRole('heading', { name: /Добавить модификатор/ })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Какой модификатор отменить/ })).toBeInTheDocument()

    expect(screen.queryByText('1 игроков')).not.toBeInTheDocument()
    expect(
      screen.queryByText(
        'Отдельно добавляйте модификатор игроку или отменяйте неиспользованную активацию с возвратом очков.',
      ),
    ).not.toBeInTheDocument()
  })

  it('shows only the modifier name and price in the modifier dropdown', async () => {
    renderPanel()
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
        roundId: 'round-1',
        roundVersion: 3,
        modifierId: 'modifier-1',
        modifierName: 'Расходники',
        activatedByUserId: 'player-1',
        activatedByDisplayName: 'Player One',
        activationCost: 3,
        activatedAtUtc: '2026-08-13T18:00:00Z',
      },
      {
        activationId: 'activation-2',
        roundId: 'round-1',
        roundVersion: 3,
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
    fireEvent.change(screen.getByRole('textbox', { name: 'Причина отмены' }), {
      target: { value: 'Ошибочная покупка' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Отменить и вернуть очки' }))

    const confirmDialog = screen.getByRole('dialog', { name: 'Отменить эту активацию?' })
    fireEvent.click(within(confirmDialog).getByRole('button', { name: 'Отменить и вернуть очки' }))

    await waitFor(() => expect(apiMocks.cancelGameModifierActivation).toHaveBeenCalledTimes(1))
    expect(apiMocks.cancelGameModifierActivation).toHaveBeenCalledWith(
      'activation-2',
      3,
      'Ошибочная покупка',
    )
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Отменить эту активацию?' }),
      ).not.toBeInTheDocument(),
    )
    expect(await screen.findByText('Использовано: 1')).toBeInTheDocument()
    expect(within(usedMetric as HTMLElement).getByText('1')).toBeInTheDocument()
  })

  it('requires a reason and confirmation before emergency-disabling new activations', async () => {
    renderPanel()
    expect(await screen.findByRole('combobox', { name: 'Игрок' })).toHaveValue('Player One')

    fireEvent.mouseDown(await screen.findByRole('combobox', { name: 'Модификатор' }))
    fireEvent.click(within(await screen.findByRole('listbox')).getByRole('option'))
    const disableButton = screen.getByRole('button', { name: 'Отключить новые активации' })
    expect(disableButton).toBeDisabled()

    fireEvent.change(screen.getByRole('textbox', { name: 'Причина аварийного отключения' }), {
      target: { value: 'Обнаружена ошибка правила' },
    })
    expect(disableButton).toBeEnabled()
    fireEvent.click(disableButton)

    const confirmDialog = screen.getByRole('dialog', { name: 'Отключить новые активации?' })
    expect(confirmDialog).toHaveTextContent('Существующие активации и история не изменятся')
    fireEvent.click(
      within(confirmDialog).getByRole('button', { name: 'Отключить новые активации' }),
    )

    await waitFor(() =>
      expect(apiMocks.emergencyDisableGameModifier).toHaveBeenCalledWith(
        'modifier-1',
        'Обнаружена ошибка правила',
      ),
    )
    expect(await screen.findByText('Новые активации отключены для этой игры.')).toBeInTheDocument()
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
        <AdminModifierTool />
      </AuthContext.Provider>
    </QueryClientProvider>,
  )

  return {
    ...rendered,
    queryClient,
  }
}
