import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router-dom'
import { gameModifiersRoute } from '../../routes/app-routes.ts'
import { AppToast } from '../../shared/ui/index.ts'
import { GameManagementTool } from '../game-board/ui/GameManagementPanel.tsx'
import { useActiveGameTeam } from '../game-board/use-active-game-team.ts'
import { useGameBoardLaunchPanel } from '../game-board/use-game-board-launch-panel.ts'
import { useGameBoardPage } from '../game-board/use-game-board-page.ts'
import { useGameTeamPlayedState } from '../game-board/use-game-team-played-state.ts'
import { useManualQuizAward } from '../game-board/use-manual-quiz-award.ts'
import { useManualQuizAwardPlayers } from '../game-board/use-manual-quiz-award-players.ts'
import { useStartGameRound } from '../game-board/use-start-game-round.ts'
import { useGameFinish } from '../game-board/use-game-finish.ts'
import { AdminModifierTool } from '../game-modifiers/AdminModifierPanel.tsx'
import { AdminToolDrawer, type AdminToolDescriptor } from './ui/AdminToolDrawer.tsx'

type AdminToolId = 'game' | 'modifiers'

export function GameAdminToolsHost() {
  const { pathname } = useLocation()
  const initialToolId = resolveInitialToolId(pathname)

  if (!initialToolId) {
    return null
  }

  return <GameAdminToolsPanel initialToolId={initialToolId} />
}

export function GameAdminToolsPanel({ initialToolId }: { initialToolId: AdminToolId }) {
  const { t } = useTranslation()
  const { data, activeRound, teamQueue, isTeamQueueError, isTeamQueueLoading, isError, isLoading } =
    useGameBoardPage()
  const activeTeam = useActiveGameTeam()
  const teamPlayedState = useGameTeamPlayedState()
  const manualQuizAward = useManualQuizAward()
  const startRound = useStartGameRound()
  const gameFinish = useGameFinish()
  const launchPanel = useGameBoardLaunchPanel(data?.status ?? '')
  const manualQuizAwardPlayers = useManualQuizAwardPlayers(launchPanel.canManageGame)
  const isAdmin = launchPanel.canStartGame

  if (isLoading || isError || !data || !launchPanel.canManageGame) {
    return null
  }

  const tools: AdminToolDescriptor[] = [
    {
      id: 'game',
      label: t('adminTools.gameTool'),
      content: (
        <GameManagementTool
          snapshot={data}
          activeRound={activeRound}
          teams={teamQueue}
          isTeamQueueLoading={isTeamQueueLoading}
          isTeamQueueError={isTeamQueueError}
          isSelectingActiveTeam={activeTeam.isSelectingActiveTeam}
          onSelectActiveTeam={activeTeam.selectActiveTeam}
          manualQuizAwardPlayers={manualQuizAwardPlayers.players}
          isManualQuizAwardPlayersLoading={manualQuizAwardPlayers.isLoading}
          isManualQuizAwardPlayersError={manualQuizAwardPlayers.isError}
          isAwardingManualQuizPoints={manualQuizAward.isAwardingManualQuizPoints}
          onAwardManualQuizPoints={manualQuizAward.awardManualQuizPoints}
          isChangingRoundStage={startRound.isChangingRoundStage}
          onStartRound={startRound.startRound}
          onBeginGameplay={startRound.beginGameplay}
          onReviewRound={startRound.reviewRound}
          onRebuildRound={startRound.rebuildRound}
          onTechnicalCancelRound={startRound.technicalCancelRound}
          onCompleteRound={startRound.completeRound}
          isUpdatingPlayedState={teamPlayedState.isUpdatingPlayedState}
          onSetTeamPlayedState={teamPlayedState.setTeamPlayedState}
          launchPanel={launchPanel}
          finishState={gameFinish}
        />
      ),
    },
  ]

  if (isAdmin && data.status === 'active') {
    tools.push({
      id: 'modifiers',
      label: t('adminTools.modifierTool'),
      content: <AdminModifierTool />,
    })
  }

  const resolvedInitialToolId = tools.some((tool) => tool.id === initialToolId)
    ? initialToolId
    : tools[0].id

  return (
    <>
      <AdminToolDrawer tools={tools} initialToolId={resolvedInitialToolId} />

      <AppToast
        message={gameFinish.toastMessage}
        onClose={gameFinish.dismissToast}
        severity="success"
        autoHideDuration={5000}
      />
      <AppToast
        message={launchPanel.toastMessage}
        onClose={launchPanel.dismissToast}
        severity="error"
        autoHideDuration={5000}
      />
      <AppToast
        message={activeTeam.toastMessage}
        onClose={activeTeam.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
      <AppToast
        message={manualQuizAward.toastMessage}
        onClose={manualQuizAward.dismissToast}
        severity={manualQuizAward.toastSeverity}
        autoHideDuration={4000}
      />
      <AppToast
        message={startRound.toastMessage}
        onClose={startRound.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
      <AppToast
        message={teamPlayedState.toastMessage}
        onClose={teamPlayedState.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
    </>
  )
}

function resolveInitialToolId(pathname: string): AdminToolId | null {
  if (
    pathname === gameModifiersRoute.fullPath ||
    pathname.startsWith(`${gameModifiersRoute.fullPath}/`)
  ) {
    return 'modifiers'
  }

  return null
}
