import { cleanup, render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { currentGameBoardQueryOptions } from '../features/game-board/index.ts'
import { gameRegistrationSnapshotQueryOptions } from '../features/game-registration/index.ts'
import type { GameRegistrationSnapshot } from '../shared/api/contracts/index.ts'
import { AuthContext, type AuthContextValue } from '../shared/auth/auth-context.ts'
import { gameApplicationRoute, gameBoardRoute, panelRootPath } from './app-routes.ts'
import { PanelIndexRedirect } from './PanelIndexRedirect.tsx'

afterEach(() => {
  cleanup()
})

function createAuthValue(): AuthContextValue {
  return {
    user: {
      id: 'user-1',
      displayName: 'Player',
      roles: ['viewer'],
    },
    authStatus: 'authenticated',
    isAuthenticated: true,
    startTwitchLogin: vi.fn(),
    logout: vi.fn(async () => undefined),
    refreshSession: vi.fn(async () => true),
  }
}

function createRegistrationSnapshot(): GameRegistrationSnapshot {
  return {
    gameId: 'game-1',
    gameStatus: 'ready',
    minPlayersPerTeam: 1,
    maxPlayersPerTeam: 4,
    slots: [],
    teams: [],
    myTeam: null,
    myPendingInvitations: [],
    myOutgoingInvitations: [],
    canInvitePlayersToMyTeam: false,
    invitablePlayers: [],
  }
}

function renderRedirect(
  registrationSnapshot: GameRegistrationSnapshot | null,
  currentGameStatus: 'ready' | 'active' | 'finished' | null = registrationSnapshot
    ? 'ready'
    : 'active',
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: Number.POSITIVE_INFINITY,
      },
    },
  })

  queryClient.setQueryData(
    currentGameBoardQueryOptions.queryKey,
    currentGameStatus == null
      ? null
      : {
          gameId: 'game-1',
          title: 'Current game',
          description: null,
          status: currentGameStatus,
          version: 1,
          rows: 1,
          cols: 1,
          rowLabels: ['A'],
          colLabels: ['1'],
          cells: [],
          enabledModifierIds: [],
          activeModifiers: [],
        },
  )
  queryClient.setQueryData(gameRegistrationSnapshotQueryOptions.queryKey, registrationSnapshot)

  return {
    queryClient,
    ...render(
      <AuthContext.Provider value={createAuthValue()}>
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[panelRootPath]}>
            <Routes>
              <Route path={panelRootPath} element={<PanelIndexRedirect />} />
              <Route path={gameApplicationRoute.fullPath} element={<div>Application page</div>} />
              <Route path={gameBoardRoute.fullPath} element={<div>Board page</div>} />
              <Route path="/" element={<div>Home page</div>} />
            </Routes>
          </MemoryRouter>
        </QueryClientProvider>
      </AuthContext.Provider>,
    ),
  }
}

describe('PanelIndexRedirect', () => {
  it('uses the application page as the landing route while registration is open', () => {
    renderRedirect(createRegistrationSnapshot())

    expect(screen.getByText('Application page')).toBeInTheDocument()
    expect(screen.queryByText('Board page')).not.toBeInTheDocument()
  })

  it('keeps the normal first accessible route when registration is closed', () => {
    renderRedirect(null)

    expect(screen.getByText('Board page')).toBeInTheDocument()
    expect(screen.queryByText('Application page')).not.toBeInTheDocument()
  })

  it('keeps registration query idle when the current game is active', () => {
    const { queryClient } = renderRedirect(null, 'active')

    expect(
      queryClient.getQueryState(gameRegistrationSnapshotQueryOptions.queryKey)?.fetchStatus,
    ).toBe('idle')
  })
})
