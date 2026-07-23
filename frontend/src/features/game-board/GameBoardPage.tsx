import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { gameApplicationRoute } from '../../routes/app-routes.ts'
import {
  AppDialog,
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
import { useGameTeamPlayedState } from './use-game-team-played-state.ts'
import { useStartGameCardRun } from './use-start-game-card-run.ts'

function getParticipantInitials(displayName: string) {
  return displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

export function GameBoardPage() {
  const { t } = useTranslation()
  const [previewCell, setPreviewCell] = useState<GameBoardCell | null>(null)
  const {
    data,
    activeRun,
    teamQueue,
    isTeamQueueError,
    isTeamQueueLoading,
    isError,
    isLoading,
  } =
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
  const teamPlayedState = useGameTeamPlayedState()
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
  const activeTeamParticipantNames = activeTeamEntry?.participants.map((participant) => participant.displayName) ?? []

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
          />
          {activeTeamEntry ? (
            <Box
              sx={(theme) => ({
                position: 'relative',
                overflow: 'hidden',
                mb: 1.75,
                borderRadius: 3,
                border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
                background: `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.2)} 0%, ${alpha(
                  theme.palette.success.main,
                  0.16,
                )} 42%, ${alpha(theme.palette.info.main, 0.18)} 100%)`,
                boxShadow: `0 20px 44px ${alpha(theme.palette.common.black, 0.3)}, inset 0 1px 0 ${alpha(
                  theme.palette.warning.light,
                  0.28,
                )}`,
                px: { xs: 1.4, sm: 1.8, md: 2.2 },
                py: { xs: 1.35, sm: 1.6, md: 1.9 },
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  inset: 0,
                  background: `radial-gradient(circle at top right, ${alpha(
                    theme.palette.warning.light,
                    0.3,
                  )}, transparent 32%), radial-gradient(circle at bottom left, ${alpha(
                    theme.palette.info.light,
                    0.18,
                  )}, transparent 34%)`,
                  pointerEvents: 'none',
                },
              })}
            >
              <Stack
                direction={{ xs: 'column', lg: 'row' }}
                spacing={1.6}
                alignItems={{ xs: 'stretch', lg: 'center' }}
                justifyContent="space-between"
                sx={{ position: 'relative', zIndex: 1 }}
              >
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1.5}
                  alignItems={{ xs: 'flex-start', sm: 'center' }}
                  sx={{ minWidth: 0, flex: 1 }}
                >
                  <Box
                    sx={(theme) => ({
                      display: 'grid',
                      placeItems: 'center',
                      width: { xs: 68, sm: 76 },
                      height: { xs: 68, sm: 76 },
                      flexShrink: 0,
                      borderRadius: '22px',
                      border: `1px solid ${alpha(theme.palette.warning.light, 0.48)}`,
                      background: `linear-gradient(160deg, ${alpha(theme.palette.common.white, 0.18)}, ${alpha(
                        theme.palette.common.black,
                        0.2,
                      )})`,
                      boxShadow: `inset 0 1px 0 ${alpha(theme.palette.common.white, 0.14)}, 0 12px 24px ${alpha(
                        theme.palette.common.black,
                        0.24,
                      )}`,
                    })}
                  >
                    <Stack spacing={0.15} alignItems="center">
                      <Typography
                        variant="caption"
                        sx={{ lineHeight: 1, letterSpacing: '0.18em', color: 'text.secondary' }}
                      >
                        TEAM
                      </Typography>
                      <Typography variant="h4" fontWeight={900} sx={{ lineHeight: 1 }}>
                        {activeTeamEntry.teamSlotIndex}
                      </Typography>
                    </Stack>
                  </Box>

                  <Box sx={{ minWidth: 0, flex: 1 }}>
                    <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.2 }}>
                      {t('gameBoard.managementActiveTeamTitle')}
                    </Typography>
                    <Stack
                      direction={{ xs: 'column', md: 'row' }}
                      spacing={1}
                      alignItems={{ xs: 'flex-start', md: 'center' }}
                      justifyContent="space-between"
                      sx={{ mt: 0.35 }}
                    >
                      <Box sx={{ minWidth: 0 }}>
                        <Stack
                          direction="row"
                          spacing={0.9}
                          alignItems="center"
                          flexWrap="wrap"
                          useFlexGap
                        >
                          <Typography
                            variant="h4"
                            sx={{
                              lineHeight: 1,
                              fontWeight: 900,
                              letterSpacing: '-0.03em',
                            }}
                          >
                            {t('gameBoard.teamQueueTeamTitle', {
                              slot: activeTeamEntry.teamSlotIndex,
                            })}
                          </Typography>
                          <Chip
                            size="small"
                            color="warning"
                            variant="filled"
                            label={t('gameBoard.teamQueueActiveChip')}
                          />
                          {activeRun ? (
                            <Chip
                              size="small"
                              color="info"
                              variant="outlined"
                              label={t('gameBoard.activeRunLabel', {
                                teamSlot: activeRun.teamSlotIndex,
                                score: activeRun.baseScore,
                              })}
                            />
                          ) : null}
                        </Stack>

                        {activeTeamParticipantNames.length > 0 ? (
                          <Typography
                            variant="body2"
                            color="text.secondary"
                            sx={{ mt: 0.75, maxWidth: 620 }}
                          >
                            {activeTeamParticipantNames.join(' • ')}
                          </Typography>
                        ) : null}
                      </Box>

                      {activeTeamEntry.participants.length > 0 ? (
                        <Stack
                          direction="row"
                          spacing={-0.6}
                          sx={{
                            pr: { xs: 0, md: 0.25 },
                            alignSelf: { xs: 'flex-start', md: 'center' },
                          }}
                        >
                          {activeTeamEntry.participants.slice(0, 4).map((participant) => (
                            <Box
                              key={participant.userId}
                              sx={(theme) => ({
                                display: 'grid',
                                placeItems: 'center',
                                width: 38,
                                height: 38,
                                borderRadius: '50%',
                                border: `2px solid ${alpha(theme.palette.background.paper, 0.92)}`,
                                background: `linear-gradient(135deg, ${alpha(
                                  theme.palette.warning.main,
                                  0.38,
                                )}, ${alpha(theme.palette.info.main, 0.32)})`,
                                boxShadow: `0 8px 18px ${alpha(theme.palette.common.black, 0.24)}`,
                                typography: 'subtitle2',
                                fontWeight: 900,
                              })}
                              title={participant.displayName}
                            >
                              {getParticipantInitials(participant.displayName)}
                            </Box>
                          ))}
                        </Stack>
                      ) : null}
                    </Stack>

                    {activeTeamEntry.participants.length > 0 ? (
                      <Stack
                        direction="row"
                        spacing={0.8}
                        flexWrap="wrap"
                        useFlexGap
                        sx={{ mt: 1.2 }}
                      >
                        {activeTeamEntry.participants.map((participant) => (
                          <Chip
                            key={participant.userId}
                            size="small"
                            variant="outlined"
                            label={participant.displayName}
                            sx={(theme) => ({
                              height: 32,
                              borderRadius: 999,
                              borderColor: alpha(theme.palette.warning.light, 0.34),
                              backgroundColor: alpha(theme.palette.common.black, 0.12),
                              backdropFilter: 'blur(6px)',
                              '& .MuiChip-label': {
                                px: 1.15,
                                fontWeight: 700,
                              },
                            })}
                          />
                        ))}
                      </Stack>
                    ) : null}
                  </Box>
                </Stack>
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
            onCellPreviewMedia={setPreviewCell}
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

      <AppDialog
        open={previewCell !== null}
        onClose={() => setPreviewCell(null)}
        maxWidth="md"
        title={previewCell?.title || t('gameBoard.cellMediaDialogTitle')}
        description={
          previewCell
            ? t('gameBoard.openConfirmDescription', {
                cost: previewCell.cost,
                row: previewCell.row,
                col: previewCell.col,
              })
            : undefined
        }
      >
        {previewCell ? (
          <Stack spacing={2}>
            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <Chip
                variant="outlined"
                label={t('gameBoard.costLabel', { cost: previewCell.cost })}
              />
              <Chip
                variant="outlined"
                label={t('gameBoard.cellMediaCountLabel', {
                  count: previewCell.media.length,
                })}
              />
            </Stack>

            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
              {previewCell.description || t('gameBoard.cellMediaEmpty')}
            </Typography>

            <Box
              sx={{
                display: 'grid',
                gap: 1,
                gridTemplateColumns: {
                  xs: '1fr',
                  sm: 'repeat(2, minmax(0, 1fr))',
                },
              }}
            >
              {previewCell.media.map((media, index) => (
                <Box
                  key={`${media.url}-${index}`}
                  component="img"
                  src={media.url}
                  alt={previewCell.title || t('gameBoard.cellMediaDialogTitle')}
                  loading="lazy"
                  decoding="async"
                  sx={{
                    width: '100%',
                    borderRadius: 2,
                    border: '1px solid',
                    borderColor: 'divider',
                    objectFit: 'cover',
                    maxHeight: 320,
                  }}
                />
              ))}
            </Box>
          </Stack>
        ) : null}
      </AppDialog>

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

      <AppToast
        message={teamPlayedState.toastMessage}
        onClose={teamPlayedState.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
    </PageShell>
  )
}
