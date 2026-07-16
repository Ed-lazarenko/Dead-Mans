import { cleanup, fireEvent, screen } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { TeamRegistrationsPage } from './TeamRegistrationsPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useTeamRegistrationsPage: vi.fn(),
}))

vi.mock('./use-team-registrations-page.ts', () => ({
  useTeamRegistrationsPage: pageMocks.useTeamRegistrationsPage,
}))

function createPageController(data: unknown, overrides: Record<string, unknown> = {}) {
  return {
    adminSnapshotQuery: {
      isLoading: false,
      isError: false,
      data,
    },
    createAdminTeam: { isPending: false, mutate: vi.fn() },
    createAdminInvitation: { isPending: false, variables: undefined, mutate: vi.fn() },
    assignPlayerToTeam: { isPending: false, mutate: vi.fn() },
    removePlayerFromTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
    cancelTeamInvitation: { isPending: false, variables: undefined, mutate: vi.fn() },
    moveTeamToSlot: { isPending: false, mutate: vi.fn() },
    confirmTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
    rejectTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
    disbandTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
    toastMessage: null,
    dismissToast: vi.fn(),
    ...overrides,
  }
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  pageMocks.useTeamRegistrationsPage.mockReturnValue(createPageController(null))
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('TeamRegistrationsPage', () => {
  it('renders loading and error states', () => {
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(null, {
        adminSnapshotQuery: { isLoading: true, isError: false, data: undefined },
      }),
    )
    renderWithAppProviders(<TeamRegistrationsPage />)
    expect(screen.getByText('Загрузка команд...')).toBeInTheDocument()

    cleanup()
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(null, {
        adminSnapshotQuery: { isLoading: false, isError: true, data: undefined },
      }),
    )
    renderWithAppProviders(<TeamRegistrationsPage />)
    expect(screen.getByText('Не удалось загрузить команды.')).toBeInTheDocument()
  })

  it('shows a clean unavailable state while registration is closed', () => {
    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.getByText('Заявки команд')).toBeInTheDocument()
    expect(
      screen.getByText('Приём заявок для игры в статусе ready пока не открыт.'),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders both an empty ready state and actionable team rows', () => {
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController({
        gameId: 'game-1',
        gameStatus: 'ready',
        minPlayersPerTeam: 1,
        maxPlayersPerTeam: 2,
        slots: [],
        teams: [],
        availablePlayers: [],
      }),
    )
    renderWithAppProviders(<TeamRegistrationsPage />)
    expect(
      screen.getByText('Пока нет команд. Создайте пустой состав и распределите игроков вручную.'),
    ).toBeInTheDocument()

    cleanup()
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController({
        gameId: 'game-1',
        gameStatus: 'ready',
        minPlayersPerTeam: 1,
        maxPlayersPerTeam: 2,
        slots: [
          {
            slotId: 'slot-1',
            slotIndex: 2,
            availability: 'public',
            reservedLabel: null,
            isAvailableForNewTeam: false,
            teamId: 'team-1',
            teamStatus: 'forming',
          },
        ],
        teams: [
          {
            teamId: 'team-1',
            slotIndex: 2,
            slotAvailability: 'public',
            reservedLabel: null,
            recruitmentOpen: true,
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
            pendingInvitations: [
              {
                invitationId: 'inv-1',
                player: {
                  userId: 'user-2',
                  login: 'invited',
                  displayName: 'Invited Player',
                },
                createdAtUtc: '2026-06-11T12:05:00Z',
              },
            ],
          },
        ],
        availablePlayers: [],
      }),
    )
    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.getByText('Invited Player')).toBeInTheDocument()
    expect(screen.getByText('Ожидает подтверждения')).toBeInTheDocument()
    expect(
      screen.getByText('Перед подтверждением дождитесь ответа на приглашения или отмените их.'),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Подтвердить' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Отклонить' })).toBeEnabled()
  })

  it('shows bottom room creation actions without rendering empty slots', () => {
    const createAdminTeam = { isPending: false, mutate: vi.fn() }
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 1,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: true,
              teamId: null,
              teamStatus: null,
            },
          ],
          teams: [],
          availablePlayers: [],
        },
        { createAdminTeam },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.queryByText('Слот свободен')).not.toBeInTheDocument()
    expect(screen.getByText('Создать комнату')).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Создать открытую команду' }))
    expect(createAdminTeam.mutate).toHaveBeenCalledWith({
      recruitmentOpen: true,
      slotId: undefined,
    })
  })

  it('assigns a dragged free player into a team slot', () => {
    const assignPlayerToTeam = { isPending: false, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 2,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'forming',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 2,
              slotAvailability: 'public',
              reservedLabel: null,
              recruitmentOpen: true,
              status: 'forming',
              members: [],
            },
          ],
          availablePlayers: [
            {
              userId: 'user-77',
              login: 'freeplayer',
              displayName: 'Free Player',
            },
          ],
        },
        {
          assignPlayerToTeam,
        },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    const transferStore = new Map<string, string>()
    const dataTransfer = {
      effectAllowed: 'all',
      setData: vi.fn((type: string, value: string) => {
        transferStore.set(type, value)
      }),
      getData: vi.fn((type: string) => transferStore.get(type) ?? ''),
    }

    fireEvent.dragStart(screen.getByTestId('admin-player-user-77'), { dataTransfer })
    fireEvent.dragOver(screen.getByTestId('admin-slot-2'), { dataTransfer })
    fireEvent.drop(screen.getByTestId('admin-slot-2'), { dataTransfer })

    expect(assignPlayerToTeam.mutate).toHaveBeenCalledWith({
      teamId: 'team-1',
      userId: 'user-77',
    })
  })

  it('sends an admin invitation to an available player for a selected team', () => {
    const createAdminInvitation = { isPending: false, variables: undefined, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 2,
              availability: 'private',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'forming',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 2,
              slotAvailability: 'private',
              reservedLabel: null,
              recruitmentOpen: false,
              status: 'forming',
              members: [],
              pendingInvitations: [],
            },
          ],
          availablePlayers: [
            {
              userId: 'user-77',
              login: 'candidate',
              displayName: 'Candidate Player',
            },
          ],
        },
        {
          createAdminInvitation,
        },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Пригласить игрока' }))
    expect(screen.getByText('Пригласить в команду #2')).toBeInTheDocument()
    expect(screen.getAllByText('Candidate Player')).toHaveLength(2)

    fireEvent.click(screen.getByRole('button', { name: 'Пригласить' }))

    expect(createAdminInvitation.mutate).toHaveBeenCalledWith({
      slotId: 'slot-1',
      invitedUserId: 'user-77',
      teamId: 'team-1',
    })
  })

  it('asks for confirmation before removing a player from a team', () => {
    const removePlayerFromTeam = { isPending: false, variables: undefined, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 2,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'confirmed',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 2,
              slotAvailability: 'public',
              reservedLabel: null,
              recruitmentOpen: false,
              status: 'confirmed',
              members: [
                {
                  player: {
                    userId: 'user-1',
                    login: 'player',
                    displayName: 'Player One',
                  },
                  joinedAtUtc: '2026-06-11T11:00:00Z',
                },
              ],
              pendingInvitations: [],
            },
          ],
          availablePlayers: [],
        },
        { removePlayerFromTeam },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Исключить' }))
    expect(screen.getByText('Исключить игрока из команды?')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Исключить игрока' }))

    expect(removePlayerFromTeam.mutate).toHaveBeenCalledWith({
      teamId: 'team-1',
      userId: 'user-1',
    })
  })

  it('cancels a pending invitation from the team roster', () => {
    const cancelTeamInvitation = { isPending: false, variables: undefined, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 2,
              availability: 'private',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'forming',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 2,
              slotAvailability: 'private',
              reservedLabel: null,
              recruitmentOpen: false,
              status: 'forming',
              members: [],
              pendingInvitations: [
                {
                  invitationId: 'inv-1',
                  player: {
                    userId: 'user-2',
                    login: 'invited',
                    displayName: 'Invited Player',
                  },
                  createdAtUtc: '2026-06-11T12:05:00Z',
                },
              ],
            },
          ],
          availablePlayers: [],
        },
        { cancelTeamInvitation },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Отменить приглашение' }))

    expect(cancelTeamInvitation.mutate).toHaveBeenCalledWith({
      teamId: 'team-1',
      invitationId: 'inv-1',
    })
  })

  it('moves teams up and down to define game order', () => {
    const moveTeamToSlot = { isPending: false, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 1,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'forming',
            },
            {
              slotId: 'slot-2',
              slotIndex: 2,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-2',
              teamStatus: 'forming',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 1,
              slotAvailability: 'public',
              reservedLabel: null,
              recruitmentOpen: true,
              status: 'forming',
              members: [],
            },
            {
              teamId: 'team-2',
              slotIndex: 2,
              slotAvailability: 'public',
              reservedLabel: null,
              recruitmentOpen: false,
              status: 'forming',
              members: [],
            },
          ],
          availablePlayers: [],
        },
        { moveTeamToSlot },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    fireEvent.click(screen.getAllByRole('button', { name: 'Ниже' })[0])

    expect(moveTeamToSlot.mutate).toHaveBeenCalledWith({
      teamId: 'team-1',
      targetSlotId: 'slot-2',
    })
  })

  it('shows disband requests and asks for confirmation before disbanding a confirmed team', () => {
    const disbandTeam = { isPending: false, variables: undefined, mutate: vi.fn() }

    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController(
        {
          gameId: 'game-1',
          gameStatus: 'ready',
          minPlayersPerTeam: 1,
          maxPlayersPerTeam: 2,
          slots: [
            {
              slotId: 'slot-1',
              slotIndex: 2,
              availability: 'public',
              reservedLabel: null,
              isAvailableForNewTeam: false,
              teamId: 'team-1',
              teamStatus: 'confirmed',
            },
          ],
          teams: [
            {
              teamId: 'team-1',
              slotIndex: 2,
              slotAvailability: 'public',
              reservedLabel: null,
              recruitmentOpen: false,
              status: 'confirmed',
              disbandRequestedAtUtc: '2026-06-11T12:00:00Z',
              disbandRequestedByUserId: 'user-1',
              disbandRequestedByDisplayName: 'Player One',
              members: [
                {
                  player: {
                    userId: 'user-1',
                    login: 'player',
                    displayName: 'Player One',
                  },
                  joinedAtUtc: '2026-06-11T11:00:00Z',
                },
              ],
            },
          ],
          availablePlayers: [],
        },
        {
          disbandTeam,
        },
      ),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.getByText('Игроки запросили роспуск команды')).toBeInTheDocument()
    expect(screen.getByText('Очередь 2 · Player One')).toBeInTheDocument()
    expect(screen.getByText('Запрос на роспуск')).toBeInTheDocument()
    expect(screen.getByText(/Player One попросил администратора/i)).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Распустить' }))
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Распустить команду' }))

    expect(disbandTeam.mutate).toHaveBeenCalledWith('team-1')
  })
})
