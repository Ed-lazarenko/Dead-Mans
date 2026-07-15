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
    assignPlayerToTeam: { isPending: false, mutate: vi.fn() },
    moveTeamToSlot: { isPending: false, mutate: vi.fn() },
    confirmTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
    rejectTeam: { isPending: false, variables: undefined, mutate: vi.fn() },
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
          },
        ],
        availablePlayers: [],
      }),
    )
    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.getByText('Player One')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Подтвердить' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Отклонить' })).toBeEnabled()
  })

  it('shows empty slot actions so an admin can create a team directly in place', () => {
    pageMocks.useTeamRegistrationsPage.mockReturnValue(
      createPageController({
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
      }),
    )

    renderWithAppProviders(<TeamRegistrationsPage />)

    expect(screen.getByText('Слот свободен')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Открытую сюда' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Закрытую сюда' })).toBeEnabled()
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
})
