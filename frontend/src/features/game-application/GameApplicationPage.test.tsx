import { cleanup, fireEvent, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameApplicationPage } from './GameApplicationPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useGameApplicationPage: vi.fn(),
}))

vi.mock('./use-game-application-page.ts', () => ({
  useGameApplicationPage: pageMocks.useGameApplicationPage,
}))

function createPageController(data: unknown) {
  return {
    snapshotQuery: {
      isLoading: false,
      isError: false,
      data,
    },
    createTeam: { isPending: false, mutate: vi.fn() },
    joinTeam: { isPending: false, mutate: vi.fn() },
    leaveTeam: { isPending: false, mutate: vi.fn() },
    requestTeamDisband: { isPending: false, mutate: vi.fn() },
    createPlayerInvitation: { isPending: false, mutate: vi.fn() },
    cancelPlayerInvitation: { isPending: false, mutate: vi.fn() },
    acceptInvitation: { isPending: false, variables: undefined, mutate: vi.fn() },
    declineInvitation: { isPending: false, variables: undefined, mutate: vi.fn() },
    toastMessage: null,
    dismissToast: vi.fn(),
  }
}

function renderPage() {
  return renderWithAppProviders(
    <MemoryRouter>
      <GameApplicationPage />
    </MemoryRouter>,
  )
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  pageMocks.useGameApplicationPage.mockReturnValue(createPageController(null))
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameApplicationPage', () => {
  it('shows a clean unavailable state while registration is closed', () => {
    renderPage()

    expect(screen.getByText('Заявка на игру')).toBeInTheDocument()
    expect(
      screen.getByText('Приём заявок закрыт. Дождитесь публикации игры администратором.'),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders invitation, create-team and open-team concerns as separate sections', () => {
    pageMocks.useGameApplicationPage.mockReturnValue(
      createPageController({
        gameId: 'game-1',
        gameStatus: 'ready',
        minPlayersPerTeam: 1,
        maxPlayersPerTeam: 4,
        slots: [],
        teams: [
          {
            teamId: 'team-1',
            slotIndex: 2,
            slotAvailability: 'public',
            reservedLabel: null,
            recruitmentOpen: true,
            status: 'forming',
            members: [],
            pendingInvitations: [
              {
                invitationId: 'invitation-2',
                player: {
                  userId: 'user-2',
                  login: 'pending',
                  displayName: 'Pending Player',
                },
                createdAtUtc: '2026-06-11T12:05:00Z',
              },
            ],
          },
        ],
        myTeam: null,
        myPendingInvitations: [
          {
            invitationId: 'invitation-1',
            slotId: 'slot-1',
            slotIndex: 1,
            teamId: null,
            status: 'pending',
            createdAtUtc: '2026-06-11T12:00:00Z',
            invitedByDisplayName: 'Captain One',
            invitedUserDisplayName: 'Player',
          },
        ],
        myOutgoingInvitations: [],
        canInvitePlayersToMyTeam: false,
        invitablePlayers: [],
      }),
    )

    renderPage()

    expect(screen.getAllByText('Приглашения')).not.toHaveLength(0)
    expect(screen.getByText('Как хотите собрать команду?')).toBeInTheDocument()
    expect(screen.getByText('Созданные команды')).toBeInTheDocument()
    expect(screen.getByText('Pending Player · ожидает подтверждения')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Принять' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Открытая команда' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Вступить' })).toBeInTheDocument()
  })

  it('hides create-team controls once the player already has a team', () => {
    pageMocks.useGameApplicationPage.mockReturnValue(
      createPageController({
        gameId: 'game-1',
        gameStatus: 'ready',
        minPlayersPerTeam: 1,
        maxPlayersPerTeam: 2,
        slots: [],
        teams: [],
        myTeam: {
          teamId: 'team-1',
          slotIndex: 1,
          slotAvailability: 'public',
          reservedLabel: null,
          recruitmentOpen: false,
          status: 'forming',
          members: [
            {
              player: {
                userId: 'user-1',
                login: 'player',
                displayName: 'Player One',
              },
              joinedAtUtc: '2026-06-11T12:00:00Z',
            },
          ],
        },
        myPendingInvitations: [],
        myOutgoingInvitations: [],
        canInvitePlayersToMyTeam: false,
        invitablePlayers: [],
      }),
    )

    renderPage()

    expect(screen.queryByText('Как хотите собрать команду?')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Выйти из команды' })).toBeInTheDocument()
    expect(screen.getByText('Созданные команды')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Вступить' })).not.toBeInTheDocument()
  })

  it('blocks leaving while a private invitation is pending and shows cancel action', () => {
    pageMocks.useGameApplicationPage.mockReturnValue(
      createPageController({
        gameId: 'game-1',
        gameStatus: 'ready',
        minPlayersPerTeam: 1,
        maxPlayersPerTeam: 2,
        slots: [],
        teams: [],
        myTeam: {
          teamId: 'team-1',
          slotIndex: 1,
          slotAvailability: 'public',
          reservedLabel: null,
          recruitmentOpen: false,
          status: 'forming',
          members: [
            {
              player: {
                userId: 'user-1',
                login: 'captain',
                displayName: 'Captain One',
              },
              joinedAtUtc: '2026-06-11T12:00:00Z',
            },
          ],
        },
        myPendingInvitations: [],
        myOutgoingInvitations: [
          {
            invitationId: 'invitation-1',
            slotId: 'slot-1',
            slotIndex: 1,
            teamId: 'team-1',
            status: 'pending',
            createdAtUtc: '2026-06-11T12:00:00Z',
            invitedByDisplayName: 'Captain One',
            invitedUserDisplayName: 'Player Two',
          },
        ],
        canInvitePlayersToMyTeam: false,
        invitablePlayers: [],
      }),
    )

    renderPage()

    expect(screen.getByRole('button', { name: 'Отменить приглашение' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Выйти из команды' })).toBeDisabled()
    expect(
      screen.getByText(
        'Нельзя выйти из команды, пока отправленное приглашение ожидает ответа. Сначала отмените приглашение.',
      ),
    ).toBeInTheDocument()
  })

  it('replaces direct leave with an admin disband request for confirmed teams', () => {
    const requestTeamDisband = { isPending: false, mutate: vi.fn() }
    const pageController = createPageController({
      gameId: 'game-1',
      gameStatus: 'ready',
      minPlayersPerTeam: 1,
      maxPlayersPerTeam: 2,
      slots: [],
      teams: [],
      myTeam: {
        teamId: 'team-1',
        slotIndex: 1,
        slotAvailability: 'public',
        reservedLabel: null,
        recruitmentOpen: false,
        status: 'confirmed',
        disbandRequestedAtUtc: null,
        disbandRequestedByUserId: null,
        disbandRequestedByDisplayName: null,
        members: [
          {
            player: {
              userId: 'user-1',
              login: 'player',
              displayName: 'Player One',
            },
            joinedAtUtc: '2026-06-11T12:00:00Z',
          },
        ],
      },
      myPendingInvitations: [],
      myOutgoingInvitations: [],
      canInvitePlayersToMyTeam: false,
      invitablePlayers: [],
    })

    pageMocks.useGameApplicationPage.mockReturnValue({
      ...pageController,
      requestTeamDisband,
    })

    renderPage()

    expect(screen.queryByRole('button', { name: 'Выйти из команды' })).not.toBeInTheDocument()
    const requestButton = screen.getByRole('button', { name: 'Попросить распустить команду' })
    fireEvent.click(requestButton)
    expect(requestTeamDisband.mutate).toHaveBeenCalled()
  })
})
