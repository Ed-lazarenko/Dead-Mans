import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { gameApplicationRoute } from '../../routes/app-routes.ts'
import {
  AppLinkButton,
  AppToast,
  ConfirmDialog,
  PageShell,
  PageStatePanel,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../game-registration/model/team-name.ts'
import { GameManagementPanel } from './ui/GameManagementPanel.tsx'
import { GameBoardCardPreviewDialog } from './ui/GameBoardCardPreviewDialog.tsx'
import { GameBoardGrid } from './ui/GameBoardGrid.tsx'
import { TeamQueuePanel } from './ui/TeamQueuePanel.tsx'
import { buildGameManagementFlow } from './model/game-management-flow.ts'
import { useActiveGameTeam } from './use-active-game-team.ts'
import { useCardPlayResult } from './use-card-play-result.ts'
import { useGameBoardCellResults } from './use-game-board-cell-results.ts'
import { useGameBoardLaunchPanel } from './use-game-board-launch-panel.ts'
import { useGameBoardPage } from './use-game-board-page.ts'
import { useManualQuizAward } from './use-manual-quiz-award.ts'
import { useManualQuizAwardPlayers } from './use-manual-quiz-award-players.ts'
import { useOpenGameBoardCell } from './use-open-game-board-cell.ts'
import { useGameTeamPlayedState } from './use-game-team-played-state.ts'
import { useStartGameRound } from './use-start-game-round.ts'

export function GameBoardPage() {
  const { t } = useTranslation()
  const [previewCell, setPreviewCell] = useState<GameBoardCell | null>(null)
  const {
    data,
    activeRound,
    teamQueue,
    teamQueueSummary,
    isTeamQueueError,
    isTeamQueueLoading,
    isError,
    isLoading,
  } = useGameBoardPage()
  const {
    pendingCell,
    toastMessage,
    canOpenCells,
    isSubmitting,
    requestOpenCell,
    confirmOpenCell,
    dismissPendingCell,
    dismissToast,
  } = useOpenGameBoardCell({
    activeTeamId: data?.activeTeamId ?? null,
    gameStatus: data?.status ?? null,
    hasActiveRound: activeRound !== null,
  })
  const activeTeam = useActiveGameTeam()
  const teamPlayedState = useGameTeamPlayedState()
  const manualQuizAward = useManualQuizAward()
  const startRound = useStartGameRound()
  const launchPanel = useGameBoardLaunchPanel(data?.status ?? '')
  const manualQuizAwardPlayers = useManualQuizAwardPlayers(launchPanel.canManageGame)
  const previewPlayResult = useCardPlayResult(data?.gameId ?? null, previewCell)
  const boardCellResults = useGameBoardCellResults(data?.gameId ?? null, data?.cells ?? [])

  if (isLoading) {
    return (
      <PageStatePanel title={t('gameBoard.title')} message={t('gameBoard.loading')} showSpinner />
    )
  }

  if (isError) {
    return (
      <PageStatePanel
        title={t('gameBoard.title')}
        message={t('gameBoard.errorLoading')}
        tone="error"
      />
    )
  }

  if (data === null) {
    return <PageStatePanel title={t('gameBoard.title')} message={t('gameBoard.empty')} />
  }

  if (!data) {
    return (
      <PageStatePanel
        title={t('gameBoard.title')}
        message={t('gameBoard.errorLoading')}
        tone="error"
      />
    )
  }

  const snapshot = data
  const flow = buildGameManagementFlow(snapshot, activeRound)
  const currentActiveTeamId = activeRound?.teamId ?? snapshot.activeTeamId ?? null
  const activeTeamEntry =
    (currentActiveTeamId
      ? (teamQueue.find((team) => team.teamId === currentActiveTeamId) ?? null)
      : null) ??
    (activeRound
      ? {
          teamId: activeRound.teamId,
          teamName: activeRound.teamName,
          teamSlotIndex: activeRound.teamSlotIndex,
          isPlayed: false,
          participants: [],
        }
      : null)
  const highlightedStep =
    flow.steps.find((step) => step.state === 'current') ??
    flow.steps.find((step) => step.state === 'ready') ??
    null
  const highlightedStepColor =
    highlightedStep?.state === 'ready'
      ? 'warning'
      : highlightedStep?.state === 'blocked'
        ? 'default'
        : 'info'
  const activeTeamParticipantNames =
    activeTeamEntry?.participants.map((participant) => participant.displayName) ?? []

  return (
    <PageShell
      variant="centered"
      sx={{
        width: '100%',
        px: 0,
        flexDirection: 'column',
        gap: 2,
        justifyContent: 'flex-start',
      }}
    >
      {snapshot.status === 'ready' ? (
        <Box
          sx={(theme) => ({
            width: '100%',
            maxWidth: 1180,
            border: `1px solid ${alpha(theme.palette.warning.main, 0.72)}`,
            background: `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.2)}, ${alpha(
              theme.palette.primary.main,
              0.12,
            )})`,
            boxShadow: `0 12px 34px ${alpha(theme.palette.common.black, 0.34)}, inset 0 1px 0 ${alpha(
              theme.palette.warning.light,
              0.28,
            )}`,
            px: { xs: 2, sm: 2.5, md: 3 },
            py: { xs: 1.75, sm: 2 },
          })}
        >
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            alignItems={{ xs: 'stretch', sm: 'center' }}
            justifyContent="space-between"
          >
            <Box sx={{ minWidth: 0 }}>
              <Typography variant="subtitle1" fontWeight={800}>
                {t('gameBoard.registrationNoticeTitle')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.registrationNoticeDescription')}
              </Typography>
            </Box>
            <AppLinkButton
              to={gameApplicationRoute.fullPath}
              tone="primary"
              sx={{
                flexShrink: 0,
                alignSelf: { xs: 'flex-start', sm: 'center' },
                px: 2.5,
              }}
            >
              {t('gameBoard.registrationNoticeAction')}
            </AppLinkButton>
          </Stack>
        </Box>
      ) : null}

      <TeamQueuePanel
        teams={teamQueue}
        isLoading={isTeamQueueLoading}
        isError={isTeamQueueError}
        activeTeamId={snapshot.activeTeamId ?? activeRound?.teamId ?? null}
      />

      <Stack
        spacing={2}
        sx={{
          width: '100%',
          maxWidth: 1536,
          alignItems: 'stretch',
        }}
      >
        <SectionCard
          sx={{
            flex: '1 1 0',
            minWidth: 0,
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <SectionHeader title={snapshot.title || t('gameBoard.title')} />
          {!launchPanel.canManageGame && activeTeamEntry ? (
            <Box
              sx={(theme) => ({
                mb: 1.25,
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
                background: `linear-gradient(135deg, ${alpha(
                  theme.palette.warning.main,
                  0.13,
                )}, ${alpha(theme.palette.background.paper, 0.56)})`,
                px: { xs: 1.2, sm: 1.5 },
                py: { xs: 1.1, sm: 1.25 },
              })}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={1.2}
                alignItems={{ xs: 'stretch', md: 'center' }}
                justifyContent="space-between"
              >
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1}
                  alignItems={{ xs: 'flex-start', sm: 'center' }}
                  sx={{ minWidth: 0, flex: 1 }}
                >
                  <Box
                    sx={(theme) => ({
                      display: 'grid',
                      placeItems: 'center',
                      width: 44,
                      height: 44,
                      flexShrink: 0,
                      borderRadius: 1.5,
                      border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
                      backgroundColor: alpha(theme.palette.background.paper, 0.5),
                    })}
                  >
                    <Typography variant="subtitle1" fontWeight={900} sx={{ lineHeight: 1 }}>
                      #{activeTeamEntry.teamSlotIndex}
                    </Typography>
                  </Box>

                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Stack spacing={0.8}>
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', fontWeight: 850 }}
                      >
                        {t('gameBoard.managementActiveTeamTitle')}
                      </Typography>
                      <Typography
                        variant="h6"
                        sx={{
                          lineHeight: 1.18,
                          fontWeight: 900,
                        }}
                      >
                        {formatGameBoardTeamName(
                          t,
                          activeTeamEntry.teamName,
                          activeTeamEntry.teamSlotIndex,
                        )}
                      </Typography>

                      {activeTeamParticipantNames.length > 0 ? (
                        <Stack
                          component="div"
                          direction="row"
                          spacing={0.65}
                          flexWrap="wrap"
                          useFlexGap
                        >
                          {activeTeamParticipantNames.map((participantName) => (
                            <Chip
                              key={participantName}
                              size="small"
                              variant="filled"
                              label={participantName}
                              sx={(theme) => ({
                                maxWidth: '100%',
                                borderRadius: 1.4,
                                border: `1px solid ${alpha(theme.palette.warning.main, 0.32)}`,
                                backgroundColor: alpha(theme.palette.warning.main, 0.18),
                                color: 'text.primary',
                                fontWeight: 800,
                                '& .MuiChip-label': {
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                },
                              })}
                            />
                          ))}
                        </Stack>
                      ) : null}
                    </Stack>
                  </Box>
                </Stack>

                {activeRound ? (
                  <Chip
                    color="info"
                    variant="outlined"
                    label={t('gameBoard.activeTeamRoundBadge', {
                      teamSlot: activeRound.teamSlotIndex,
                    })}
                    sx={{
                      alignSelf: { xs: 'flex-start', md: 'center' },
                      flexShrink: 0,
                      fontWeight: 850,
                    }}
                  />
                ) : null}
              </Stack>
            </Box>
          ) : null}
          {!launchPanel.canManageGame ? (
            <Box
              sx={(theme) => ({
                mb: 1.25,
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.info.main, 0.34)}`,
                backgroundColor: alpha(theme.palette.info.main, 0.07),
                px: { xs: 1.2, sm: 1.5 },
                py: { xs: 1.1, sm: 1.25 },
              })}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={1.2}
                alignItems={{ xs: 'flex-start', md: 'flex-start' }}
                justifyContent="space-between"
              >
                <Stack spacing={0.45} sx={{ minWidth: 0, flex: 1 }}>
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', fontWeight: 800 }}
                  >
                    {t('gameBoard.flowTitle')}
                  </Typography>
                  {highlightedStep ? (
                    <Typography variant="subtitle1" sx={{ fontWeight: 900, lineHeight: 1.22 }}>
                      {t(highlightedStep.titleKey)}
                    </Typography>
                  ) : null}
                  <Typography variant="body2" color="text.secondary">
                    {t(flow.summaryKey)}
                  </Typography>
                </Stack>

                {highlightedStep ? (
                  <Chip
                    color={highlightedStepColor}
                    variant="outlined"
                    label={t(`gameBoard.flowStepState.${highlightedStep.state}`)}
                    sx={{ alignSelf: { xs: 'flex-start', md: 'center' } }}
                  />
                ) : null}
              </Stack>
            </Box>
          ) : null}
          <GameBoardGrid
            snapshot={snapshot}
            playResultsByCellId={boardCellResults.playResultsByCellId}
            canOpenCells={canOpenCells}
            onCellRequestOpen={requestOpenCell}
            onCellPreviewMedia={setPreviewCell}
          />
        </SectionCard>
      </Stack>

      {launchPanel.canManageGame ? (
        <GameManagementPanel
          snapshot={snapshot}
          activeRound={activeRound}
          teams={teamQueue}
          teamStats={teamQueueSummary}
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
          onReviewRound={startRound.reviewRound}
          onCompleteRound={startRound.completeRound}
          isUpdatingPlayedState={teamPlayedState.isUpdatingPlayedState}
          onSetTeamPlayedState={teamPlayedState.setTeamPlayedState}
          launchPanel={launchPanel}
        />
      ) : null}

      <ConfirmDialog
        open={pendingCell !== null}
        onClose={dismissPendingCell}
        onConfirm={confirmOpenCell}
        isBusy={isSubmitting}
        title={t('gameBoard.openConfirmTitle')}
        description={t('gameBoard.openConfirmDescription', {
          cost: pendingCell?.cost ?? 0,
          row: pendingCell?.row ?? '-',
          col: pendingCell?.col ?? '-',
        })}
        cancelLabel={t('gameBoard.openCancel')}
        confirmLabel={t('gameBoard.openConfirm')}
      />

      <GameBoardCardPreviewDialog
        cell={previewCell}
        playResult={previewPlayResult}
        onClose={() => setPreviewCell(null)}
      />

      <AppToast
        message={toastMessage}
        onClose={dismissToast}
        severity="info"
        autoHideDuration={3000}
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
    </PageShell>
  )
}

function formatGameBoardTeamName(
  t: ReturnType<typeof useTranslation>['t'],
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(
    teamName,
    t('gameBoard.teamQueueTeamTitle', { slot: teamSlotIndex }),
  )
}
