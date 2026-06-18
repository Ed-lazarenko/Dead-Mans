import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { ThemeProvider } from '@mui/material'
import type { ReactNode } from 'react'
import { I18nextProvider } from 'react-i18next'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../i18n.ts'
import { appTheme } from '../app/theme/appTheme.ts'
import { AuthContext } from '../shared/auth/auth-context.ts'
import type { AuthContextValue, AuthUser } from '../shared/auth/auth-context.ts'
import { PanelNavigation } from './PanelNavigation.tsx'

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(() => {
  cleanup()
})

function renderNavigation(user: AuthUser, initialPath = '/panel/game-board') {
  const authValue: AuthContextValue = {
    user,
    authStatus: 'authenticated',
    isAuthenticated: true,
    startTwitchLogin: vi.fn(),
    logout: vi.fn(async () => undefined),
    refreshSession: vi.fn(async () => true),
  }

  function Providers({ children }: { children: ReactNode }) {
    return (
      <I18nextProvider i18n={i18n}>
        <ThemeProvider theme={appTheme}>
          <MemoryRouter initialEntries={[initialPath]}>
            <AuthContext.Provider value={authValue}>{children}</AuthContext.Provider>
          </MemoryRouter>
        </ThemeProvider>
      </I18nextProvider>
    )
  }

  return render(<PanelNavigation />, { wrapper: Providers })
}

describe('PanelNavigation', () => {
  it('keeps the primary navigation focused on player tasks', () => {
    renderNavigation({
      id: 'viewer-1',
      displayName: 'Player',
      roles: ['viewer'],
    })

    expect(screen.getAllByRole('link', { name: 'Игра' }).length).toBeGreaterThan(0)
    expect(screen.getAllByRole('link', { name: 'Подать заявку' }).length).toBeGreaterThan(0)

    fireEvent.click(screen.getByRole('button', { name: /Player/ }))

    expect(screen.queryByRole('menuitem', { name: 'Поле' })).not.toBeInTheDocument()
    expect(screen.queryByRole('menuitem', { name: 'Команды' })).not.toBeInTheDocument()
  })

  it('places administration links inside the admin profile menu', () => {
    renderNavigation({
      id: 'admin-1',
      displayName: 'Admin',
      roles: ['admin'],
    })

    expect(screen.queryByRole('menuitem', { name: 'Поле' })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Admin/ }))

    expect(screen.getByRole('menuitem', { name: 'Поле' })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: 'Команды' })).toBeInTheDocument()
  })
})
