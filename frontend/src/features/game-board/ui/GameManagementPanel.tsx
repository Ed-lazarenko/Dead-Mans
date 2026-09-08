import { Box, Stack, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardSnapshot,
  GameRegistrationAdminSnapshot,
  GameTeamQueueItem,
} from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { AppButton } from '../../../shared/ui/index.ts'
import { AdminGameLaunchDrawer } from '../../game-registration/index.ts'
import { buildGameManagementFlow } from '../model/game-management-flow.ts'
import type { CompleteRoundInput } from '../model/game-round-summary-form.ts'
import { GameRoundSummaryDialog } from './GameRoundSummaryDialog.tsx'
import { buildRoundActionModel, type GameRoundDetails } from '../model/game-management-panel.ts'
import { ManualQuizAwardControl } from './ManualQuizAwardControl.tsx'
import { RoundSafetyControls } from './RoundSafetyControls.tsx'
import type { TechnicalCancelRoundInput } from '../use-start-game-round.ts'
import { ManagementFlowPanel } from './management-panel/ManagementFlowPanel.tsx'
import { SecondaryManagementSection } from './management-panel/ManagementPanelSurfaces.tsx'
import { RoundAssistantSection } from './management-panel/RoundAssistantSection.tsx'
import { TeamControlSection } from './management-panel/TeamControlSection.tsx'
import { GameFinishDialog } from './GameFinishDialog.tsx'

type ManualQuizAwardPlayer = components['schemas']['ManualQuizAwardPlayerDto']

interface GameManagementLaunchState {
  canStartGame: boolean
  canFinishGame: boolean
  shouldRender: boolean
  snapshot: GameRegistrationAdminSnapshot | null | undefined
  isLoadingLaunchState: boolean
  isStartingGame: boolean
  startGame: () => void
}

interface GameFinishState {
  isFinishing: boolean
  error: unknown
  resetError: () => void
  finishGame: (input: {
    gameId: string
    expectedBoardVersion: number
    requestId: string
    acknowledgedWarningCodes: string[]
    note: string | null
  }) => Promise<unknown>
}

interface GameManagementToolProps {
  snapshot: GameBoardSnapshot
  activeRound: GameRoundDetails | null
  teams: readonly GameTeamQueueItem[]
  isTeamQueueLoading: boolean
  isTeamQueueError: boolean
  isSelectingActiveTeam: boolean
  onSelectActiveTeam: (teamId: string | null) => void | Promise<unknown>
  manualQuizAwardPlayers: readonly ManualQuizAwardPlayer[]
  isManualQuizAwardPlayersLoading: boolean
  isManualQuizAwardPlayersError: boolean
  isAwardingManualQuizPoints: boolean
  onAwardManualQuizPoints: (input: {
    awardedToUserId: string
    operationType: 'award' | 'deduct'
    points: number
    reason: string
    requestId: string
  }) => void
  isChangingRoundStage: boolean
  onStartRound: (input: { roundId: string; expectedRoundVersion: number }) => void
  onBeginGameplay: (input: { roundId: string; expectedRoundVersion: number }) => void
  onReviewRound: (input: { roundId: string; expectedRoundVersion: number }) => void
  onRebuildRound: (input: { roundId: string; expectedRoundVersion: number }) => void
  onTechnicalCancelRound: (input: TechnicalCancelRoundInput) => void
  onCompleteRound: (input: CompleteRoundInput) => Promise<unknown>
  isUpdatingPlayedState: boolean
  onSetTeamPlayedState: (input: { teamId: string; isPlayed: boolean }) => void | Promise<unknown>
  launchPanel: GameManagementLaunchState
  finishState: GameFinishState
}

export function GameManagementTool({
  snapshot,
  activeRound,
  teams,
  isTeamQueueLoading,
  isTeamQueueError,
  isSelectingActiveTeam,
  onSelectActiveTeam,
  manualQuizAwardPlayers,
  isManualQuizAwardPlayersLoading,
  isManualQuizAwardPlayersError,
  isAwardingManualQuizPoints,
  onAwardManualQuizPoints,
  isChangingRoundStage,
  onStartRound,
  onBeginGameplay,
  onReviewRound,
  onRebuildRound,
  onTechnicalCancelRound,
  onCompleteRound,
  isUpdatingPlayedState,
  onSetTeamPlayedState,
  launchPanel,
  finishState,
}: GameManagementToolProps) {
  const { t } = useTranslation()
  const [isRoundSummaryDialogOpen, setIsRoundSummaryDialogOpen] = useState(false)
  const [recentTeamId, setRecentTeamId] = useState<string | null>(null)
  const [isFinishDialogOpen, setIsFinishDialogOpen] = useState(false)
  const canShowLaunchAction = launchPanel.shouldRender && launchPanel.snapshot
  const isActiveGame = snapshot.status === 'active'
  const currentActiveTeamId = activeRound?.teamId ?? snapshot.activeTeamId ?? null
  const isActiveTeamLocked = activeRound !== null
  const orderedTeams = useMemo(
    () => [...teams].sort((left, right) => left.teamSlotIndex - right.teamSlotIndex),
    [teams],
  )
  const currentActiveTeam =
    (currentActiveTeamId
      ? (orderedTeams.find((team) => team.teamId === currentActiveTeamId) ?? null)
      : null) ?? null
  const recentTeam =
    (recentTeamId ? (orderedTeams.find((team) => team.teamId === recentTeamId) ?? null) : null) ??
    null
  const resumableTeam = !currentActiveTeam && recentTeam && !recentTeam.isPlayed ? recentTeam : null
  const flow = buildGameManagementFlow(snapshot, activeRound)
  const selectableTeams = orderedTeams.filter((team) => !team.isPlayed)
  const isRoundSummarySubmitting =
    isChangingRoundStage || isSelectingActiveTeam || isUpdatingPlayedState
  const handleSelectActiveTeam = (teamId: string | null) => {
    if (teamId) {
      setRecentTeamId(teamId)
    }

    return onSelectActiveTeam(teamId)
  }
  const roundAction = buildRoundActionModel({
    t,
    snapshot,
    activeRound,
    hasCurrentActiveTeam: currentActiveTeamId !== null,
    resumableTeam,
    onStartRound,
    onBeginGameplay,
    onReviewRound,
    onOpenSummary: () => setIsRoundSummaryDialogOpen(true),
    onResumeTeam: handleSelectActiveTeam,
  })

  return (
    <>
      <Box data-testid="game-management-tool">
        <Stack spacing={1.15}>
          {flow.phase === 'ready' ? (
            <SecondaryManagementSection
              sectionId="launch"
              title={t('gameBoard.managementLaunchTitle')}
              tooltip={t('gameBoard.managementLaunchTooltip')}
              defaultExpanded
            >
              {launchPanel.isLoadingLaunchState ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementLaunchLoading')}
                </Typography>
              ) : canShowLaunchAction ? (
                <Stack spacing={1}>
                  <Typography variant="body2" color="text.secondary">
                    {t('gameBoard.managementLaunchDescription')}
                  </Typography>
                  <AdminGameLaunchDrawer
                    snapshot={canShowLaunchAction}
                    isStartingGame={launchPanel.isStartingGame}
                    onStartGame={launchPanel.startGame}
                  />
                </Stack>
              ) : launchPanel.canStartGame ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementLaunchNoRegistrationState')}
                </Typography>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementLaunchAdminOnly')}
                </Typography>
              )}
            </SecondaryManagementSection>
          ) : null}

          {flow.phase === 'active_idle' ||
          flow.phase === 'round_running' ||
          flow.phase === 'reviewing' ? (
            <>
              <RoundAssistantSection
                roundAction={roundAction}
                isChangingRoundStage={isChangingRoundStage}
              />

              {activeRound ? (
                <SecondaryManagementSection
                  sectionId="round-safety"
                  title={t('gameBoard.roundPanelSafetyTitle')}
                  tooltip={t('gameBoard.roundPanelSafetyTooltip')}
                >
                  <RoundSafetyControls
                    activeRound={activeRound}
                    isBusy={isChangingRoundStage}
                    onRebuild={onRebuildRound}
                    onTechnicalCancel={onTechnicalCancelRound}
                  />
                </SecondaryManagementSection>
              ) : null}

              <TeamControlSection
                isActiveGame={isActiveGame}
                isLoading={isTeamQueueLoading}
                isError={isTeamQueueError}
                isSelectingActiveTeam={isSelectingActiveTeam}
                isUpdatingPlayedState={isUpdatingPlayedState}
                isActiveTeamLocked={isActiveTeamLocked}
                teams={orderedTeams}
                selectableTeams={selectableTeams}
                currentActiveTeam={currentActiveTeam}
                resumableTeam={resumableTeam}
                onSelectActiveTeam={handleSelectActiveTeam}
                onSetTeamPlayedState={onSetTeamPlayedState}
              />

              <SecondaryManagementSection
                sectionId="manual-quiz"
                title={t('gameBoard.manualQuizAwardTitle')}
                tooltip={t('gameBoard.manualQuizAwardTooltip')}
              >
                <ManualQuizAwardControl
                  isActiveGame={isActiveGame}
                  players={manualQuizAwardPlayers}
                  isLoading={isManualQuizAwardPlayersLoading}
                  isError={isManualQuizAwardPlayersError}
                  isAwarding={isAwardingManualQuizPoints}
                  onAward={onAwardManualQuizPoints}
                  showHeader={false}
                />
              </SecondaryManagementSection>

              {launchPanel.canFinishGame ? (
                <SecondaryManagementSection
                  sectionId="finish-game"
                  title={t('gameBoard.finishSectionTitle')}
                  tooltip={t('gameBoard.finishSectionTooltip')}
                >
                  <Stack spacing={1}>
                    <Typography variant="body2" color="text.secondary">
                      {activeRound
                        ? t('gameBoard.finishBlockedByRound')
                        : t('gameBoard.finishSectionDescription')}
                    </Typography>
                    <AppButton
                      tone="danger"
                      disabled={activeRound !== null}
                      onClick={() => {
                        finishState.resetError()
                        setIsFinishDialogOpen(true)
                      }}
                    >
                      {t('gameBoard.finishOpenAction')}
                    </AppButton>
                  </Stack>
                </SecondaryManagementSection>
              ) : null}
            </>
          ) : null}

          <SecondaryManagementSection
            sectionId="flow-details"
            title={t('gameBoard.flowTitle')}
            tooltip={t('gameBoard.flowTooltip')}
          >
            <ManagementFlowPanel snapshot={snapshot} activeRound={activeRound} />
          </SecondaryManagementSection>
        </Stack>
      </Box>

      {activeRound?.status === 'reviewing_results' ? (
        <GameRoundSummaryDialog
          open={isRoundSummaryDialogOpen}
          activeRound={activeRound}
          isSubmitting={isRoundSummarySubmitting}
          onClose={() => setIsRoundSummaryDialogOpen(false)}
          onSubmit={async ({ roundSummary, postRoundAction }) => {
            await onCompleteRound(roundSummary)

            if (postRoundAction === 'mark_team_played') {
              await onSetTeamPlayedState({
                teamId: activeRound.teamId,
                isPlayed: true,
              })
            } else {
              await handleSelectActiveTeam(activeRound.teamId)
            }

            setIsRoundSummaryDialogOpen(false)
          }}
        />
      ) : null}

      {launchPanel.canFinishGame && snapshot.status === 'active' && isFinishDialogOpen ? (
        <GameFinishDialog
          open={isFinishDialogOpen}
          gameId={snapshot.gameId}
          isFinishing={finishState.isFinishing}
          finishError={finishState.error}
          onClose={() => setIsFinishDialogOpen(false)}
          onFinish={finishState.finishGame}
        />
      ) : null}
    </>
  )
}
