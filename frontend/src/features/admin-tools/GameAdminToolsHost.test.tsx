import { cleanup, fireEvent, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { gameModifiersRoute } from '../../routes/app-routes.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameAdminToolsHost } from './GameAdminToolsHost.tsx'

const mocks = vi.hoisted(() => ({
  useGameBoardPage: vi.fn(),
  useGameBoardLaunchPanel: vi.fn(),
  useActiveGameTeam: vi.fn(),
  useGameTeamPlayedState: vi.fn(),
  useManualQuizAward: vi.fn(),
  useManualQuizAwardPlayers: vi.fn(),
  useStartGameRound: vi.fn(),
}))

vi.mock('../game-board/use-game-board-page.ts', () => ({
  useGameBoardPage: mocks.useGameBoardPage,
}))
vi.mock('../game-board/use-game-board-launch-panel.ts', () => ({
  useGameBoardLaunchPanel: mocks.useGameBoardLaunchPanel,
}))
vi.mock('../game-board/use-active-game-team.ts', () => ({
  useActiveGameTeam: mocks.useActiveGameTeam,
}))
vi.mock('../game-board/use-game-team-played-state.ts', () => ({
  useGameTeamPlayedState: mocks.useGameTeamPlayedState,
}))
vi.mock('../game-board/use-manual-quiz-award.ts', () => ({
  useManualQuizAward: mocks.useManualQuizAward,
}))
vi.mock('../game-board/use-manual-quiz-award-players.ts', () => ({
  useManualQuizAwardPlayers: mocks.useManualQuizAwardPlayers,
}))
vi.mock('../game-board/use-start-game-round.ts', () => ({
  useStartGameRound: mocks.useStartGameRound,
}))
vi.mock('../game-board/ui/GameManagementPanel.tsx', () => ({
  GameManagementTool: () => <div>Содержимое управления игрой</div>,
}))
vi.mock('../game-modifiers/AdminModifierPanel.tsx', () => ({
  AdminModifierTool: () => <div>Содержимое управления модификаторами</div>,
}))

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  mocks.useGameBoardPage.mockReturnValue({
    data: { gameId: 'game-1', status: 'active' },
    activeRound: null,
    teamQueue: [],
    isTeamQueueError: false,
    isTeamQueueLoading: false,
    isError: false,
    isLoading: false,
  })
  mocks.useGameBoardLaunchPanel.mockReturnValue({
    canManageGame: true,
    canStartGame: true,
    shouldRender: false,
    snapshot: null,
    isLoadingLaunchState: false,
    isStartingGame: false,
    startGame: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  mocks.useActiveGameTeam.mockReturnValue({
    isSelectingActiveTeam: false,
    selectActiveTeam: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  mocks.useGameTeamPlayedState.mockReturnValue({
    isUpdatingPlayedState: false,
    setTeamPlayedState: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
  mocks.useManualQuizAward.mockReturnValue({
    isAwardingManualQuizPoints: false,
    awardManualQuizPoints: vi.fn(),
    toastMessage: null,
    toastSeverity: 'success',
    dismissToast: vi.fn(),
  })
  mocks.useManualQuizAwardPlayers.mockReturnValue({
    players: [],
    isLoading: false,
    isError: false,
  })
  mocks.useStartGameRound.mockReturnValue({
    isChangingRoundStage: false,
    startRound: vi.fn(),
    beginGameplay: vi.fn(),
    reviewRound: vi.fn(),
    rebuildRound: vi.fn(),
    technicalCancelRound: vi.fn(),
    completeRound: vi.fn(),
    toastMessage: null,
    dismissToast: vi.fn(),
  })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameAdminToolsHost', () => {
  it('opens modifier management first on the modifier page and switches without navigation', () => {
    renderAt(gameModifiersRoute.fullPath)
    fireEvent.click(screen.getByRole('button', { name: 'Управление игрой' }))

    expect(screen.getByRole('tabpanel', { name: 'Управление модификаторами' })).toBeVisible()
    expect(screen.getByText('Содержимое управления модификаторами')).toBeVisible()

    fireEvent.click(screen.getByRole('button', { name: 'Предыдущая панель управления' }))

    expect(screen.getByRole('tabpanel', { name: 'Управление игрой' })).toBeVisible()
    expect(screen.getByText('Содержимое управления игрой')).toBeVisible()
  })

  it('does not expose modifier management to a moderator', () => {
    mocks.useGameBoardLaunchPanel.mockReturnValue({
      ...mocks.useGameBoardLaunchPanel(),
      canStartGame: false,
    })

    renderAt(gameModifiersRoute.fullPath)
    fireEvent.click(screen.getByRole('button', { name: 'Управление игрой' }))

    expect(screen.getByRole('tabpanel', { name: 'Управление игрой' })).toBeVisible()
    expect(screen.queryByText('Содержимое управления модификаторами')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Следующая панель управления' })).toBeDisabled()
  })

  it('does not mount administration queries outside supported pages', () => {
    renderAt('/panel/history')

    expect(screen.queryByRole('button', { name: 'Управление игрой' })).not.toBeInTheDocument()
    expect(mocks.useGameBoardPage).not.toHaveBeenCalled()
  })
})

function renderAt(pathname: string) {
  return renderWithAppProviders(
    <MemoryRouter initialEntries={[pathname]}>
      <GameAdminToolsHost />
    </MemoryRouter>,
  )
}
