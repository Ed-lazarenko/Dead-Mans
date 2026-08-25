import { Box, Drawer, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
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
import {
  ManagementFlowPanel,
  ManagementPanelHeader,
  RoundAssistantSection,
  SecondaryManagementSection,
  TeamControlSection,
} from './management-panel/ManagementPanelSections.tsx'

type ManualQuizAwardPlayer = components['schemas']['ManualQuizAwardPlayerDto']

interface GameManagementLaunchState {
  canStartGame: boolean
  shouldRender: boolean
  snapshot: GameRegistrationAdminSnapshot | null | undefined
  isLoadingLaunchState: boolean
  isStartingGame: boolean
  startGame: () => void
}

interface GameManagementPanelProps {
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
}

export function GameManagementPanel({
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
}: GameManagementPanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [isRoundSummaryDialogOpen, setIsRoundSummaryDialogOpen] = useState(false)
  const [recentTeamId, setRecentTeamId] = useState<string | null>(null)
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
      <AppButton
        tone="secondary"
        size="medium"
        onClick={() => setIsOpen(true)}
        sx={(theme) => ({
          position: 'fixed',
          zIndex: theme.zIndex.drawer - 1,
          right: { xs: 12, md: 0 },
          top: { xs: 'auto', md: '50%' },
          bottom: { xs: 16, md: 'auto' },
          transform: { xs: 'none', md: 'translateY(-50%)' },
          minWidth: { xs: 0, md: 52 },
          minHeight: { xs: 46, md: 192 },
          px: { xs: 1.6, md: 0.95 },
          py: { xs: 0.9, md: 1.6 },
          borderRadius: { xs: 999, md: '18px 0 0 18px' },
          writingMode: { xs: 'horizontal-tb', md: 'vertical-rl' },
          textOrientation: { xs: 'mixed', md: 'mixed' },
          justifyContent: 'center',
          letterSpacing: '0.03em',
          boxShadow: `0 14px 28px ${alpha(theme.palette.common.black, 0.38)}`,
        })}
      >
        {t('gameBoard.managementPanelOpenAction')}
      </AppButton>

      <Drawer
        anchor="right"
        open={isOpen}
        onClose={() => setIsOpen(false)}
        PaperProps={{
          sx: (theme) => ({
            width: { xs: '100vw', md: 520 },
            maxWidth: '100vw',
            position: 'fixed',
            top: 0,
            right: 0,
            bottom: 0,
            height: '100vh',
            maxHeight: '100dvh',
            display: 'flex',
            flexDirection: 'column',
            borderLeft: `1px solid ${alpha(theme.palette.divider, 0.86)}`,
            backgroundImage: 'none',
            overflow: 'hidden',
          }),
        }}
      >
        <Box
          component="aside"
          role="complementary"
          aria-label={t('gameBoard.managementPanelTitle')}
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gridTemplateRows: 'auto minmax(0, 1fr)',
            overflow: 'hidden',
          }}
        >
          <ManagementPanelHeader onClose={() => setIsOpen(false)} />
          <Box
            data-testid="game-management-panel-scroll-body"
            sx={{
              minHeight: 0,
              overflowY: 'auto',
              overflowX: 'hidden',
              overscrollBehavior: 'contain',
              WebkitOverflowScrolling: 'touch',
              px: { xs: 1, sm: 1.25 },
              py: 1.15,
            }}
          >
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
        </Box>
      </Drawer>

      {activeRound?.status === 'reviewing_results' ? (
        <GameRoundSummaryDialog
          open={isRoundSummaryDialogOpen}
          activeRound={activeRound}
          isSubmitting={isRoundSummarySubmitting}
          onClose={() => setIsRoundSummaryDialogOpen(false)}
          onSubmit={async ({ roundSummary, postRoundAction }) => {
            await onCompleteRound(roundSummary)

            if (postRoundAction === 'finish') {
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
    </>
  )
}
