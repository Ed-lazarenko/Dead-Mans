import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
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
import { GameManagementPanel } from './ui/GameManagementPanel.tsx'
import { GameBoardGrid } from './ui/GameBoardGrid.tsx'
import { TeamQueuePanel } from './ui/TeamQueuePanel.tsx'
import { useActiveGameTeam } from './use-active-game-team.ts'
import { useGameBoardLaunchPanel } from './use-game-board-launch-panel.ts'
import { useGameBoardPage } from './use-game-board-page.ts'
import { useManualQuizAward } from './use-manual-quiz-award.ts'
import { useManualQuizAwardPlayers } from './use-manual-quiz-award-players.ts'
import { useOpenGameBoardCell } from './use-open-game-board-cell.ts'
import { useStartGameCardRun } from './use-start-game-card-run.ts'

export function GameBoardPage() {
  const { t } = useTranslation()
  const { data, activeRun, teamQueue, isTeamQueueError, isTeamQueueLoading, isError, isLoading } =
    useGameBoardPage()
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
    hasActiveRound: activeRun !== null,
  })
  const activeTeam = useActiveGameTeam()
  const manualQuizAward = useManualQuizAward()
  const startCardRun = useStartGameCardRun()
  const launchPanel = useGameBoardLaunchPanel(data?.status ?? '')
  const manualQuizAwardPlayers = useManualQuizAwardPlayers(launchPanel.canManageGame)

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
        activeTeamId={snapshot.activeTeamId ?? activeRun?.teamId ?? null}
      />

      <Stack
        direction={{ xs: 'column', xl: 'row' }}
        spacing={2}
        sx={{
          width: '100%',
          maxWidth: { xs: 1180, xl: 1536 },
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
          <SectionHeader
            title={snapshot.title || t('gameBoard.title')}
            description={snapshot.description}
            actions={
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                <Chip
                  size="small"
                  color={
                    snapshot.status === 'active'
                      ? 'success'
                      : snapshot.status === 'ready'
                        ? 'info'
                        : 'default'
                  }
                  label={t(
                    snapshot.status === 'active'
                      ? 'gameBoard.statusActive'
                      : snapshot.status === 'ready'
                        ? 'gameBoard.statusReady'
                        : 'gameBoard.statusFinished',
                  )}
                />
                {activeRun ? (
                  <Chip
                    size="small"
                    color="warning"
                    variant="outlined"
                    label={t('gameBoard.activeRunLabel', {
                      teamSlot: activeRun.teamSlotIndex,
                      score: activeRun.baseScore,
                    })}
                  />
                ) : null}
              </Stack>
            }
          />
          <GameBoardGrid
            snapshot={snapshot}
            canOpenCells={canOpenCells}
            onCellRequestOpen={requestOpenCell}
          />
        </SectionCard>

        {launchPanel.canManageGame ? (
          <GameManagementPanel
            snapshot={snapshot}
            activeRun={activeRun}
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
            isChangingRoundStage={startCardRun.isChangingRoundStage}
            onStartRound={startCardRun.startRound}
            onReviewRound={startCardRun.reviewRound}
            onCompleteRound={startCardRun.completeRound}
            launchPanel={launchPanel}
          />
        ) : null}
      </Stack>

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
        message={startCardRun.toastMessage}
        onClose={startCardRun.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
    </PageShell>
  )
}
