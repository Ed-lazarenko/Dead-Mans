import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { AuthRole } from '../shared/api/contracts/index.ts'
import { AuthContext, type AuthContextValue } from '../shared/auth/auth-context.ts'
import { gameBoardRoute, gameHistoryRoute, gameSetupRoute } from './app-routes.ts'
import { RequirePanelRouteAccess } from './RequirePanelRouteAccess.tsx'

afterEach(() => {
  cleanup()
})

function renderGuard(roles: AuthRole[]) {
  const authValue: AuthContextValue = {
    user: {
      id: 'user-1',
      displayName: 'Player',
      roles,
    },
    authStatus: 'authenticated',
    isAuthenticated: true,
    startTwitchLogin: vi.fn(),
    logout: vi.fn(async () => undefined),
    refreshSession: vi.fn(async () => true),
  }

  return render(
    <AuthContext.Provider value={authValue}>
      <MemoryRouter initialEntries={[gameSetupRoute.fullPath]}>
        <Routes>
          <Route
            path={gameSetupRoute.fullPath}
            element={
              <RequirePanelRouteAccess route={gameSetupRoute}>
                <div>Admin setup</div>
              </RequirePanelRouteAccess>
            }
          />
          <Route path={gameHistoryRoute.fullPath} element={<div>Game history</div>} />
          <Route path={gameBoardRoute.fullPath} element={<div>Game board</div>} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('RequirePanelRouteAccess', () => {
  it('renders an allowed route for an admin', () => {
    renderGuard(['admin'])

    expect(screen.getByText('Admin setup')).toBeInTheDocument()
  })

  it('redirects a viewer to the first accessible panel route', () => {
    renderGuard(['viewer'])

    expect(screen.getByText('Game board')).toBeInTheDocument()
    expect(screen.queryByText('Admin setup')).not.toBeInTheDocument()
  })
})
