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
import { buildGameManagementFlow } from './model/game-management-flow.ts'
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
  const flow = buildGameManagementFlow(snapshot, activeRun)
  const selectedActiveTeamId = activeRun?.teamId ?? snapshot.activeTeamId ?? null
  const activeTeamEntry =
    (selectedActiveTeamId
      ? teamQueue.find((team) => team.teamId === selectedActiveTeamId) ?? null
      : null) ??
    (activeRun
      ? {
          teamId: activeRun.teamId,
          teamSlotIndex: activeRun.teamSlotIndex,
          participants: [],
        }
      : null)
  const highlightedStep =
    flow.steps.find((step) => step.state === 'current') ??
    flow.steps.find((step) => step.state === 'ready') ??
    null

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
          <SectionHeader
            title={snapshot.title || t('gameBoard.title')}
            description={snapshot.description}
            actions={
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
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
          {activeTeamEntry ? (
            <Box
              sx={(theme) => ({
                mb: 1.5,
                borderRadius: 2.25,
                border: `1px solid ${alpha(theme.palette.success.main, 0.36)}`,
                background: `linear-gradient(135deg, ${alpha(theme.palette.success.main, 0.18)}, ${alpha(
                  theme.palette.info.main,
                  0.1,
                )})`,
                boxShadow: `0 14px 32px ${alpha(theme.palette.common.black, 0.2)}`,
                px: { xs: 1.25, sm: 1.6 },
                py: { xs: 1.1, sm: 1.35 },
              })}
            >
              <Stack
                direction={{ xs: 'column', lg: 'row' }}
                spacing={1.2}
                alignItems={{ xs: 'flex-start', lg: 'center' }}
                justifyContent="space-between"
              >
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.2 }}>
                    {t('gameBoard.managementActiveTeamTitle')}
                  </Typography>
                  <Stack
                    direction="row"
                    spacing={0.9}
                    alignItems="center"
                    flexWrap="wrap"
                    useFlexGap
                    sx={{ mt: 0.25 }}
                  >
                    <Typography variant="h6" sx={{ lineHeight: 1.15 }}>
                      {t('gameBoard.teamQueueTeamTitle', {
                        slot: activeTeamEntry.teamSlotIndex,
                      })}
                    </Typography>
                    <Chip
                      size="small"
                      color="success"
                      variant="filled"
                      label={t('gameBoard.teamQueueActiveChip')}
                    />
                  </Stack>
                </Box>

                {activeTeamEntry.participants.length > 0 ? (
                  <Stack
                    direction="row"
                    spacing={0.8}
                    flexWrap="wrap"
                    useFlexGap
                    justifyContent={{ xs: 'flex-start', lg: 'flex-end' }}
                  >
                    {activeTeamEntry.participants.map((participant) => (
                      <Chip
                        key={participant.userId}
                        size="small"
                        variant="outlined"
                        label={participant.displayName}
                        sx={(theme) => ({
                          borderColor: alpha(theme.palette.success.light, 0.42),
                          backgroundColor: alpha(theme.palette.common.black, 0.12),
                        })}
                      />
                    ))}
                  </Stack>
                ) : null}
              </Stack>
            </Box>
          ) : null}
          <Box
            sx={(theme) => ({
              mb: 1.75,
              borderRadius: 2,
              border: `1px solid ${alpha(theme.palette.info.main, 0.28)}`,
              background: `linear-gradient(135deg, ${alpha(theme.palette.info.main, 0.14)}, ${alpha(
                theme.palette.common.black,
                0.18,
              )})`,
              px: { xs: 1.25, sm: 1.5 },
              py: { xs: 1, sm: 1.15 },
            })}
          >
            <Stack
              direction={{ xs: 'column', md: 'row' }}
              spacing={1}
              alignItems={{ xs: 'flex-start', md: 'center' }}
              justifyContent="space-between"
            >
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.2 }}>
                  {t('gameBoard.flowTitle')}
                </Typography>
                <Typography variant="body2" sx={{ mt: 0.35 }}>
                  {t(flow.summaryKey)}
                </Typography>
              </Box>

              {highlightedStep ? (
                <Chip
                  color={highlightedStep.state === 'ready' ? 'warning' : 'info'}
                  variant="outlined"
                  label={t(highlightedStep.titleKey)}
                  sx={{ alignSelf: { xs: 'flex-start', md: 'center' } }}
                />
              ) : null}
            </Stack>
          </Box>
          <GameBoardGrid
            snapshot={snapshot}
            canOpenCells={canOpenCells}
            onCellRequestOpen={requestOpenCell}
          />
        </SectionCard>
      </Stack>

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
