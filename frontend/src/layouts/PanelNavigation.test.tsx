import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { ThemeProvider } from '@mui/material'
import type { ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { gameSetupDraftQueryOptions } from '../features/game-setup/index.ts'
import { createLoadedDraftState } from '../features/game-setup/model/game-setup-query-state.ts'
import type { GameSetupSnapshot } from '../shared/api/contracts/index.ts'
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

function renderNavigation(
  user: AuthUser,
  initialPath = '/panel/game-board',
  draftSnapshot: GameSetupSnapshot | null = createDraftSnapshot(),
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: Number.POSITIVE_INFINITY,
      },
    },
  })
  const authValue: AuthContextValue = {
    user,
    authStatus: 'authenticated',
    isAuthenticated: true,
    startTwitchLogin: vi.fn(),
    logout: vi.fn(async () => undefined),
    refreshSession: vi.fn(async () => true),
  }

  queryClient.setQueryData(
    gameSetupDraftQueryOptions.queryKey,
    createLoadedDraftState(draftSnapshot),
  )

  function Providers({ children }: { children: ReactNode }) {
    return (
      <I18nextProvider i18n={i18n}>
        <QueryClientProvider client={queryClient}>
          <ThemeProvider theme={appTheme}>
            <MemoryRouter initialEntries={[initialPath]}>
              <AuthContext.Provider value={authValue}>{children}</AuthContext.Provider>
            </MemoryRouter>
          </ThemeProvider>
        </QueryClientProvider>
      </I18nextProvider>
    )
  }

  return {
    queryClient,
    ...render(<PanelNavigation />, { wrapper: Providers }),
  }
}

function createDraftSnapshot(): GameSetupSnapshot {
  return {
    gameId: 'draft-1',
    title: 'Draft game',
    description: null,
    status: 'draft',
    version: 1,
    rows: 1,
    cols: 1,
    rowLabels: ['100'],
    colLabels: ['A'],
    cells: [
      {
        id: 'cell-1',
        row: 0,
        col: 0,
        cellType: 'question',
        title: null,
        description: null,
        cost: 100,
        state: 'closed',
        media: [],
      },
    ],
    enabledModifierIds: [],
    enabledQuestionIds: [],
  } as GameSetupSnapshot
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

  it('keeps admin entry points inside the admin profile menu', () => {
    renderNavigation({
      id: 'admin-1',
      displayName: 'Admin',
      roles: ['admin'],
    })

    expect(screen.queryByRole('menuitem', { name: 'Настройка игры' })).not.toBeInTheDocument()
    expect(screen.queryByRole('menuitem', { name: 'Команды' })).not.toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: /Admin/ }))

    expect(screen.getByRole('menuitem', { name: 'Настройка игры' })).toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: 'Команды' })).toBeInTheDocument()
  })

  it('shows two admin dropdowns in the header and keeps teams out of them', () => {
    renderNavigation(
      {
        id: 'admin-1',
        displayName: 'Admin',
        roles: ['admin'],
      },
      '/panel/game-setup',
    )

    expect(screen.getAllByRole('button', { name: /Настройка игры/i }).length).toBeGreaterThan(0)
    expect(screen.getAllByRole('button', { name: /Глобальные настройки/i }).length).toBeGreaterThan(
      0,
    )
    expect(screen.queryByRole('button', { name: /Команды/i })).not.toBeInTheDocument()
  })

  it('disables current-game modifiers and questions when there is no draft game', () => {
    renderNavigation(
      {
        id: 'admin-1',
        displayName: 'Admin',
        roles: ['admin'],
      },
      '/panel/game-setup',
      null,
    )

    const [gameSetupMenuButton] = screen.getAllByRole('button', { name: /Настройка игры/i })
    fireEvent.click(gameSetupMenuButton!)

    expect(screen.getByRole('menuitem', { name: 'Настройка доски' })).not.toHaveAttribute(
      'aria-disabled',
      'true',
    )
    expect(screen.getByRole('menuitem', { name: 'Настройка модификаторов' })).toHaveAttribute(
      'aria-disabled',
      'true',
    )
    expect(screen.getByRole('menuitem', { name: 'Настройка вопросов' })).toHaveAttribute(
      'aria-disabled',
      'true',
    )
  })
})
