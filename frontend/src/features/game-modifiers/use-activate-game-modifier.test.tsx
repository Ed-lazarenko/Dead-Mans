import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { AuthContext } from '../../shared/auth/auth-context.ts'
import type { GameModifierState } from '../../shared/api/contracts/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { gameModifierQueryKeys } from './api/game-modifier-queries.ts'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

const apiMocks = vi.hoisted(() => ({
  activateGameModifier: vi.fn(),
  adminActivateGameModifier: vi.fn(),
  cancelGameModifierActivation: vi.fn(),
  fetchAdminActiveGameModifierActivations: vi.fn(),
  fetchAdminGameModifierPlayers: vi.fn(),
  fetchAdminGameModifierState: vi.fn(),
  fetchGameModifierCatalog: vi.fn(),
  fetchGameModifierState: vi.fn(),
}))

vi.mock('./api/game-modifiers-api.ts', () => apiMocks)

const authContextValue = {
  user: {
    id: 'user-1',
    displayName: 'Player One',
    roles: ['viewer'] as const,
  },
  authStatus: 'authenticated' as const,
  isAuthenticated: true,
  startTwitchLogin: vi.fn(),
  logout: vi.fn(),
  refreshSession: vi.fn(),
}

const baseState: GameModifierState = {
  gameId: 'game-1',
  availableQuizPoints: 30,
  earnedQuizPoints: 30,
  spentQuizPoints: 0,
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
        name: 'Consumable',
        description: 'Costs points and can be activated once.',
        activationCost: 15,
        defaultLimitPerGame: 1,
        activationLimit: { count: 1 },
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
        iconEmoji: '🔥',
        activationCommand: null,
      },
      isActive: false,
      canActivate: true,
      blockedReason: null,
      activationsCount: 0,
      limit: 1,
    },
    {
      modifier: {
        id: 'modifier-2',
        scoringType: 'non_scoring',
        category: 'round',
        requiresHostControl: false,
        mechanicType: 'rule_only',
        name: 'Conflicting',
        description: 'Blocked when consumable is active.',
        activationCost: 5,
        defaultLimitPerGame: 2,
        activationLimit: { count: 2 },
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
        conflictingModifierIds: ['modifier-1'],
        iconEmoji: '⛔',
        activationCommand: null,
      },
      isActive: false,
      canActivate: true,
      blockedReason: null,
      activationsCount: 0,
      limit: 2,
    },
  ],
}

function createDeferredPromise() {
  let resolve!: () => void
  const promise = new Promise<void>((innerResolve) => {
    resolve = innerResolve
  })

  return { promise, resolve }
}

function createWrapper(queryClient: QueryClient) {
  return function QueryWrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <I18nextProvider i18n={i18n}>
          <AuthContext.Provider value={authContextValue}>{children}</AuthContext.Provider>
        </I18nextProvider>
      </QueryClientProvider>
    )
  }
}

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

describe('useActivateGameModifier', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('clears pending state right after the activation request succeeds even if invalidations are still running', async () => {
    const queryClient = createQueryClient()
    const deferred = createDeferredPromise()
    queryClient.setQueryData(gameModifierQueryKeys.state(), baseState)
    queryClient.setQueryData(currentGameBoardQueryOptions.queryKey, null)
    apiMocks.activateGameModifier.mockReturnValue(deferred.promise)

    const invalidateQueries = vi
      .spyOn(queryClient, 'invalidateQueries')
      .mockReturnValue(new Promise(() => undefined))

    const { result } = renderHook(() => useActivateGameModifier(), {
      wrapper: createWrapper(queryClient),
    })

    act(() => {
      result.current.activate('modifier-1')
    })

    await waitFor(() => {
      expect(result.current.isActivating).toBe(true)
      expect(result.current.pendingModifierId).toBe('modifier-1')
    })

    await act(async () => {
      deferred.resolve()
      await Promise.resolve()
    })

    await waitFor(() => {
      expect(result.current.isActivating).toBe(false)
      expect(result.current.pendingModifierId).toBeNull()
      expect(result.current.toastMessage).toBe(i18n.t('gameModifiers.activateSuccess'))
    })

    expect(invalidateQueries).toHaveBeenCalled()
  })

  it('keeps the pending modifier id across unmount and remount while the request is still running', async () => {
    const queryClient = createQueryClient()
    const deferred = createDeferredPromise()
    queryClient.setQueryData(gameModifierQueryKeys.state(), baseState)
    apiMocks.activateGameModifier.mockReturnValue(deferred.promise)

    const firstRender = renderHook(() => useActivateGameModifier(), {
      wrapper: createWrapper(queryClient),
    })

    act(() => {
      firstRender.result.current.activate('modifier-1')
    })

    await waitFor(() => {
      expect(firstRender.result.current.isActivating).toBe(true)
      expect(firstRender.result.current.pendingModifierId).toBe('modifier-1')
    })

    firstRender.unmount()

    const secondRender = renderHook(() => useActivateGameModifier(), {
      wrapper: createWrapper(queryClient),
    })

    expect(secondRender.result.current.isActivating).toBe(true)
    expect(secondRender.result.current.pendingModifierId).toBe('modifier-1')

    await act(async () => {
      deferred.resolve()
      await Promise.resolve()
    })

    await waitFor(() => {
      expect(secondRender.result.current.isActivating).toBe(false)
      expect(secondRender.result.current.pendingModifierId).toBeNull()
    })
  })

  it('optimistically patches modifier state after a successful activation', async () => {
    const queryClient = createQueryClient()
    queryClient.setQueryData(gameModifierQueryKeys.state(), baseState)
    apiMocks.activateGameModifier.mockResolvedValue(undefined)

    const { result } = renderHook(() => useActivateGameModifier(), {
      wrapper: createWrapper(queryClient),
    })

    await act(async () => {
      result.current.activate('modifier-1')
      await Promise.resolve()
    })

    await waitFor(() => {
      const nextState = queryClient.getQueryData<GameModifierState>(gameModifierQueryKeys.state())
      expect(nextState?.availableQuizPoints).toBe(15)
      expect(nextState?.spentQuizPoints).toBe(15)
      expect(nextState?.activeModifiers).toHaveLength(1)
      expect(nextState?.availableModifiers[0]).toMatchObject({
        isActive: true,
        activationsCount: 1,
        canActivate: false,
        blockedReason: 'limit_reached',
      })
      expect(nextState?.availableModifiers[1]).toMatchObject({
        isActive: false,
        canActivate: false,
        blockedReason: 'conflict_active',
      })
    })
  })
})
