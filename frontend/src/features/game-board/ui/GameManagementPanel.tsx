import { Alert, Box, Chip, Drawer, IconButton, Stack, Tooltip, Typography } from '@mui/material'
import { alpha, type Theme } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardSnapshot,
  GameRegistrationAdminSnapshot,
  GameTeamQueueItem,
} from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import {
  AppButton,
  SectionCard,
  type AppButtonTone,
} from '../../../shared/ui/index.ts'
import { AdminGameLaunchDrawer } from '../../game-registration/index.ts'
import { buildGameManagementFlow } from '../model/game-management-flow.ts'
import type { CompleteRoundInput } from '../model/game-card-run-summary-form.ts'
import { GameCardRunSummaryDialog } from './GameCardRunSummaryDialog.tsx'
import { ManualQuizAwardControl } from './ManualQuizAwardControl.tsx'

type GameCardRunDetails = components['schemas']['GameCardRunDetailsDto']
type ManualQuizAwardPlayer = components['schemas']['ManualQuizAwardPlayerDto']

interface GameManagementLaunchState {
  canStartGame: boolean
  shouldRender: boolean
  snapshot?: GameRegistrationAdminSnapshot
  isLoadingLaunchState: boolean
  isStartingGame: boolean
  startGame: () => void
}

interface GameManagementPanelProps {
  snapshot: GameBoardSnapshot
  activeRun: GameCardRunDetails | null
  teams: readonly GameTeamQueueItem[]
  isTeamQueueLoading: boolean
  isTeamQueueError: boolean
  isSelectingActiveTeam: boolean
  onSelectActiveTeam: (teamId: string | null) => void
  manualQuizAwardPlayers: readonly ManualQuizAwardPlayer[]
  isManualQuizAwardPlayersLoading: boolean
  isManualQuizAwardPlayersError: boolean
  isAwardingManualQuizPoints: boolean
  onAwardManualQuizPoints: (input: { awardedToUserId: string; points: number }) => void
  isChangingRoundStage: boolean
  onStartRound: (input: { cellId: string; teamId: string }) => void
  onReviewRound: (cardRunId: string) => void
  onCompleteRound: (input: CompleteRoundInput) => Promise<unknown>
  isUpdatingPlayedState: boolean
  onSetTeamPlayedState: (input: { teamId: string; isPlayed: boolean }) => void
  launchPanel: GameManagementLaunchState
}

interface RoundActionModel {
  statusTone: 'info' | 'warning' | 'success'
  statusLabel: string
  title: string
  description: string
  actionLabel: string | null
  actionTone: AppButtonTone
  onAction: (() => void) | null
}

export function GameManagementPanel({
  snapshot,
  activeRun,
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
  onReviewRound,
  onCompleteRound,
  isUpdatingPlayedState,
  onSetTeamPlayedState,
  launchPanel,
}: GameManagementPanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [isRunSummaryDialogOpen, setIsRunSummaryDialogOpen] = useState(false)
  const [recentTeamId, setRecentTeamId] = useState<string | null>(null)
  const canShowLaunchAction = launchPanel.shouldRender && launchPanel.snapshot
  const isActiveGame = snapshot.status === 'active'
  const selectedActiveTeamId = snapshot.activeTeamId ?? activeRun?.teamId ?? null
  const isActiveTeamLocked = activeRun !== null
  const orderedTeams = useMemo(
    () => [...teams].sort((left, right) => left.teamSlotIndex - right.teamSlotIndex),
    [teams],
  )
  const selectedActiveTeam =
    (selectedActiveTeamId
      ? orderedTeams.find((team) => team.teamId === selectedActiveTeamId) ?? null
      : null) ?? null
  const recentTeam =
    (recentTeamId ? orderedTeams.find((team) => team.teamId === recentTeamId) ?? null : null) ?? null
  const resumableTeam =
    !selectedActiveTeam && recentTeam && !recentTeam.isPlayed ? recentTeam : null
  const flow = buildGameManagementFlow(snapshot, activeRun)
  const selectableTeams = orderedTeams.filter((team) => !team.isPlayed)

  useEffect(() => {
    if (selectedActiveTeamId) {
      setRecentTeamId(selectedActiveTeamId)
    }
  }, [selectedActiveTeamId])

  const roundAction = buildRoundActionModel({
    t,
    snapshot,
    activeRun,
    selectedActiveTeam,
    resumableTeam,
    onStartRound,
    onReviewRound,
    onOpenSummary: () => setIsRunSummaryDialogOpen(true),
    onResumeTeam: (teamId) => onSelectActiveTeam(teamId),
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

      <Drawer anchor="right" open={isOpen} onClose={() => setIsOpen(false)}>
        <Box
          component="aside"
          role="complementary"
          aria-label={t('gameBoard.managementPanelTitle')}
          sx={{
            width: { xs: '100vw', md: 560 },
            maxWidth: '100vw',
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            p: 2,
            gap: 2,
          }}
        >
          <Stack
            direction="row"
            spacing={1.5}
            alignItems="flex-start"
            justifyContent="space-between"
            sx={{ flexShrink: 0 }}
          >
            <Stack spacing={0.75}>
              <Typography variant="h5" fontWeight={800}>
                {t('gameBoard.managementPanelTitle')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.managementPanelDescription')}
              </Typography>
              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                <Chip
                  variant="outlined"
                  label={`${t('gameBoard.managementPanelBoardMetric')}: ${t(
                    'gameBoard.managementBoardSize',
                    {
                      rows: snapshot.rows,
                      cols: snapshot.cols,
                    },
                  )}`}
                />
                <Chip
                  color={activeRun ? 'warning' : snapshot.status === 'active' ? 'success' : 'default'}
                  variant={activeRun ? 'filled' : 'outlined'}
                  label={
                    activeRun
                      ? t('gameBoard.activeRunLabel', {
                          teamSlot: activeRun.teamSlotIndex,
                          score: activeRun.baseScore,
                        })
                      : t('gameBoard.managementPanelNoActiveRoundMetric')
                  }
                />
              </Stack>
            </Stack>
            <IconButton
              size="small"
              aria-label={t('gameBoard.managementPanelCloseAction')}
              onClick={() => setIsOpen(false)}
            >
              <Box component="span" aria-hidden sx={{ fontSize: 20, lineHeight: 1 }}>
                ×
              </Box>
            </IconButton>
          </Stack>

          <Stack
            spacing={2}
            sx={{
              overflowY: 'auto',
              pr: { xs: 0, md: 0.5 },
            }}
          >
            <PriorityBlock
              title={t('gameBoard.managementActiveTeamTitle')}
              tooltip={t('gameBoard.managementActiveTeamTooltip')}
              accent="info"
            >
              {!isActiveGame ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementActiveTeamInactive')}
                </Typography>
              ) : isTeamQueueLoading ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementActiveTeamLoading')}
                </Typography>
              ) : isTeamQueueError ? (
                <Typography variant="body2" color="error.main">
                  {t('gameBoard.managementActiveTeamError')}
                </Typography>
              ) : orderedTeams.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementActiveTeamNoTeams')}
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  <TeamHeroCard
                    title={
                      selectedActiveTeam
                        ? t('gameBoard.managementActiveTeamCurrentLabel')
                        : resumableTeam
                          ? t('gameBoard.managementActiveTeamRecentLabel')
                          : t('gameBoard.managementActiveTeamDescription')
                    }
                    description={
                      selectedActiveTeam
                        ? activeRun
                          ? t('gameBoard.activeRunLabel', {
                              teamSlot: selectedActiveTeam.teamSlotIndex,
                              score: activeRun.baseScore,
                            })
                          : t('gameBoard.managementRoundNextActionBoardHint')
                        : resumableTeam
                          ? t('gameBoard.managementActiveTeamResumeHint', {
                              slot: resumableTeam.teamSlotIndex,
                            })
                          : t(flow.summaryKey)
                    }
                    team={selectedActiveTeam ?? resumableTeam}
                    chips={
                      selectedActiveTeam ? (
                        <Chip
                          size="small"
                          color="success"
                          variant="filled"
                          label={t('gameBoard.teamQueueActiveChip')}
                        />
                      ) : resumableTeam ? (
                        <Chip
                          size="small"
                          color="info"
                          variant="outlined"
                          label={t('gameBoard.managementActiveTeamRecentLabel')}
                        />
                      ) : null
                    }
                  />

                  {!selectedActiveTeam && !resumableTeam ? (
                    <InlineStateNotice tone="warning">
                      {t('gameBoard.managementActiveTeamRequired')}
                    </InlineStateNotice>
                  ) : null}

                  {isActiveTeamLocked ? (
                    <InlineStateNotice tone="warning">
                      {t('gameBoard.managementActiveTeamLocked')}
                    </InlineStateNotice>
                  ) : null}

                  {selectedActiveTeam?.isPlayed ? (
                    <InlineStateNotice tone="success">
                      {t('gameBoard.teamPlayedSelectedNotice')}
                    </InlineStateNotice>
                  ) : null}

                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                    {resumableTeam && !selectedActiveTeam ? (
                      <AppButton
                        tone="primary"
                        size="large"
                        onClick={() => onSelectActiveTeam(resumableTeam.teamId)}
                        disabled={isSelectingActiveTeam || isActiveTeamLocked}
                        sx={{ minHeight: 52 }}
                      >
                        {t('gameBoard.managementActiveTeamResumeAction')}
                      </AppButton>
                    ) : null}

                    {selectedActiveTeam ? (
                      <>
                        <AppButton
                          tone="secondary"
                          size="large"
                          onClick={() => onSelectActiveTeam(null)}
                          disabled={isSelectingActiveTeam || isActiveTeamLocked}
                          sx={{ minHeight: 52 }}
                        >
                          {t('gameBoard.managementActiveTeamClearAction')}
                        </AppButton>
                        <AppButton
                          size="large"
                          tone={selectedActiveTeam.isPlayed ? 'secondary' : 'warningGhost'}
                          disabled={isUpdatingPlayedState || isActiveTeamLocked}
                          onClick={() =>
                            onSetTeamPlayedState({
                              teamId: selectedActiveTeam.teamId,
                              isPlayed: !selectedActiveTeam.isPlayed,
                            })
                          }
                          sx={{ minHeight: 52 }}
                        >
                          {selectedActiveTeam.isPlayed
                            ? t('gameBoard.teamPlayedResetAction')
                            : t('gameBoard.teamPlayedMarkAction')}
                        </AppButton>
                      </>
                    ) : null}
                  </Stack>

                  <Stack spacing={1}>
                    <Typography variant="subtitle2">
                      {t('gameBoard.managementActiveTeamQuickListTitle')}
                    </Typography>
                    <Stack spacing={1}>
                      {orderedTeams.map((team) => {
                        const isCurrent = team.teamId === selectedActiveTeam?.teamId
                        const isDisabled =
                          isSelectingActiveTeam ||
                          isActiveTeamLocked ||
                          team.isPlayed ||
                          isCurrent

                        return (
                          <AppButton
                            key={team.teamId}
                            tone={
                              isCurrent
                                ? 'success'
                                : team.isPlayed
                                  ? 'secondary'
                                  : 'secondary'
                            }
                            fullWidth
                            disabled={isDisabled}
                            onClick={() => onSelectActiveTeam(team.teamId)}
                            sx={(theme) => ({
                              minHeight: 74,
                              px: 1.5,
                              py: 1.25,
                              justifyContent: 'flex-start',
                              alignItems: 'stretch',
                              borderRadius: 2.25,
                              borderColor:
                                isCurrent || team.isPlayed
                                  ? alpha(theme.palette.success.main, 0.4)
                                  : alpha(theme.palette.info.main, 0.24),
                              background:
                                isCurrent
                                  ? `linear-gradient(135deg, ${alpha(theme.palette.success.main, 0.22)}, ${alpha(theme.palette.info.main, 0.16)})`
                                  : team.isPlayed
                                    ? `linear-gradient(135deg, ${alpha(theme.palette.success.dark, 0.16)}, ${alpha(theme.palette.common.black, 0.08)})`
                                    : `linear-gradient(135deg, ${alpha(theme.palette.info.main, 0.1)}, ${alpha(theme.palette.common.black, 0.08)})`,
                              opacity: team.isPlayed && !isCurrent ? 0.84 : 1,
                            })}
                          >
                            <Stack spacing={0.55} alignItems="flex-start" sx={{ width: '100%' }}>
                              <Stack
                                direction="row"
                                spacing={0.75}
                                alignItems="center"
                                flexWrap="wrap"
                                useFlexGap
                              >
                                <Typography variant="subtitle2" fontWeight={800}>
                                  {t('gameBoard.teamQueueTeamTitle', { slot: team.teamSlotIndex })}
                                </Typography>
                                {isCurrent ? (
                                  <Chip
                                    size="small"
                                    color="success"
                                    variant="filled"
                                    label={t('gameBoard.teamQueueActiveChip')}
                                  />
                                ) : null}
                                {team.isPlayed ? (
                                  <Chip
                                    size="small"
                                    color="success"
                                    variant="outlined"
                                    label={t('gameBoard.teamQueuePlayedChip')}
                                  />
                                ) : null}
                              </Stack>
                              <Typography variant="body2" color="text.secondary" textAlign="left">
                                {team.participants.length > 0
                                  ? team.participants
                                      .map((participant) => participant.displayName)
                                      .join(', ')
                                  : t('gameBoard.runSummaryNoParticipants')}
                              </Typography>
                            </Stack>
                          </AppButton>
                        )
                      })}
                    </Stack>
                  </Stack>

                  {selectableTeams.length === 0 && !selectedActiveTeam ? (
                    <InlineStateNotice tone="info">
                      {t('gameBoard.managementActiveTeamNoSelectableTeams')}
                    </InlineStateNotice>
                  ) : null}
                </Stack>
              )}
            </PriorityBlock>

            <PriorityBlock
              title={t('gameBoard.managementRoundNextActionTitle')}
              tooltip={t('gameBoard.managementRoundTooltip')}
              accent={roundAction.statusTone}
            >
              <Stack spacing={1.5}>
                <Box
                  sx={(theme) => ({
                    borderRadius: 2.5,
                    border: `1px solid ${alpha(
                      roundAction.statusTone === 'success'
                        ? theme.palette.success.main
                        : roundAction.statusTone === 'warning'
                          ? theme.palette.warning.main
                          : theme.palette.info.main,
                      0.4,
                    )}`,
                    background:
                      roundAction.statusTone === 'success'
                        ? `linear-gradient(135deg, ${alpha(theme.palette.success.main, 0.18)}, ${alpha(theme.palette.info.main, 0.14)})`
                        : roundAction.statusTone === 'warning'
                          ? `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.18)}, ${alpha(theme.palette.common.black, 0.1)})`
                          : `linear-gradient(135deg, ${alpha(theme.palette.info.main, 0.16)}, ${alpha(theme.palette.common.black, 0.1)})`,
                    px: 1.5,
                    py: 1.5,
                  })}
                >
                  <Stack spacing={1.1}>
                    <Stack
                      direction="row"
                      spacing={0.75}
                      alignItems="center"
                      flexWrap="wrap"
                      useFlexGap
                    >
                      <Chip
                        size="small"
                        color={roundAction.statusTone}
                        variant="filled"
                        label={roundAction.statusLabel}
                      />
                      {activeRun ? (
                        <Chip
                          size="small"
                          variant="outlined"
                          label={t('gameBoard.teamQueueTeamTitle', {
                            slot: activeRun.teamSlotIndex,
                          })}
                        />
                      ) : null}
                    </Stack>

                    <Box>
                      <Typography variant="h6" fontWeight={800}>
                        {roundAction.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
                        {roundAction.description}
                      </Typography>
                    </Box>

                    {roundAction.actionLabel && roundAction.onAction ? (
                      <AppButton
                        tone={roundAction.actionTone}
                        size="large"
                        fullWidth
                        disabled={isChangingRoundStage}
                        onClick={roundAction.onAction}
                        sx={{ minHeight: 62, fontSize: '1rem', fontWeight: 800 }}
                      >
                        {roundAction.actionLabel}
                      </AppButton>
                    ) : (
                      <InlineStateNotice
                        tone={
                          roundAction.statusTone === 'warning'
                            ? 'warning'
                            : roundAction.statusTone === 'success'
                              ? 'success'
                              : 'info'
                        }
                      >
                        {roundAction.description}
                      </InlineStateNotice>
                    )}
                  </Stack>
                </Box>
              </Stack>
            </PriorityBlock>

            <ManagementFlowPanel snapshot={snapshot} activeRun={activeRun} />

            <AdminBlock
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
            </AdminBlock>

            <AdminBlock
              title={t('gameBoard.managementLaunchTitle')}
              tooltip={t('gameBoard.managementLaunchTooltip')}
            >
              {snapshot.status !== 'ready' ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementLaunchUnavailable')}
                </Typography>
              ) : launchPanel.isLoadingLaunchState ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementLaunchLoading')}
                </Typography>
              ) : canShowLaunchAction ? (
                <Stack spacing={1}>
                  <Typography variant="body2" color="text.secondary">
                    {t('gameBoard.managementLaunchDescription')}
                  </Typography>
                  <AdminGameLaunchDrawer
                    snapshot={launchPanel.snapshot}
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
            </AdminBlock>
          </Stack>
        </Box>
      </Drawer>

      {activeRun?.status === 'reviewing_results' ? (
        <GameCardRunSummaryDialog
          open={isRunSummaryDialogOpen}
          activeRun={activeRun}
          isSubmitting={isChangingRoundStage}
          onClose={() => setIsRunSummaryDialogOpen(false)}
          onSubmit={async (input) => {
            await onCompleteRound(input)
            setIsRunSummaryDialogOpen(false)
          }}
        />
      ) : null}
    </>
  )
}

function TeamHeroCard({
  title,
  description,
  team,
  chips,
}: {
  title: string
  description: string
  team: GameTeamQueueItem | null
  chips: ReactNode
}) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: 2.5,
        border: `1px solid ${alpha(theme.palette.info.main, 0.32)}`,
        background: `linear-gradient(135deg, ${alpha(theme.palette.info.main, 0.14)}, ${alpha(theme.palette.common.black, 0.08)})`,
        px: 1.5,
        py: 1.4,
      })}
    >
      <Stack spacing={1.1}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="overline" color="text.secondary" sx={{ lineHeight: 1.1 }}>
            {title}
          </Typography>
          {chips}
        </Stack>

        <Typography variant="h5" fontWeight={800} sx={{ lineHeight: 1.08 }}>
          {team ? `#${team.teamSlotIndex}` : '-'}
        </Typography>

        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>

        {team?.participants.length ? (
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            {team.participants.map((participant) => (
              <Chip
                key={participant.userId}
                size="small"
                variant="outlined"
                label={participant.displayName}
              />
            ))}
          </Stack>
        ) : null}
      </Stack>
    </Box>
  )
}

function PriorityBlock({
  title,
  tooltip,
  accent,
  children,
}: {
  title: string
  tooltip: string
  accent: 'info' | 'warning' | 'success'
  children: ReactNode
}) {
  return (
    <SectionCard
      sx={(theme) => ({
        p: 1.6,
        borderRadius: 2.8,
        border: `1px solid ${alpha(
          accent === 'success'
            ? theme.palette.success.main
            : accent === 'warning'
              ? theme.palette.warning.main
              : theme.palette.info.main,
          0.34,
        )}`,
        boxShadow: `0 18px 36px ${alpha(theme.palette.common.black, 0.14)}`,
      })}
    >
      <Stack spacing={1.2}>
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="subtitle1" fontWeight={800}>
            {title}
          </Typography>
          <HintTooltip title={tooltip} />
        </Stack>
        {children}
      </Stack>
    </SectionCard>
  )
}

function AdminBlock({
  title,
  tooltip,
  children,
}: {
  title: string
  tooltip: string
  children: ReactNode
}) {
  return (
    <SectionCard sx={{ p: 1.5 }}>
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="subtitle2">{title}</Typography>
          <HintTooltip title={tooltip} />
        </Stack>
        {children}
      </Stack>
    </SectionCard>
  )
}

function HintTooltip({ title }: { title: string }) {
  return (
    <Tooltip title={title} arrow placement="top">
      <Box
        component="span"
        sx={(theme) => ({
          width: 18,
          height: 18,
          borderRadius: '50%',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
          color: 'text.secondary',
          fontSize: '0.7rem',
          cursor: 'help',
          flexShrink: 0,
        })}
      >
        ?
      </Box>
    </Tooltip>
  )
}

function InlineStateNotice({
  children,
  tone = 'warning',
}: {
  children: ReactNode
  tone?: 'warning' | 'error' | 'info' | 'success'
}) {
  return (
    <Alert
      severity={tone}
      variant="outlined"
      sx={{
        borderRadius: 1.5,
        m: 0,
      }}
    >
      {children}
    </Alert>
  )
}

function ManagementFlowPanel({
  snapshot,
  activeRun,
}: {
  snapshot: GameBoardSnapshot
  activeRun: GameCardRunDetails | null
}) {
  const { t } = useTranslation()
  const flow = buildGameManagementFlow(snapshot, activeRun)
  const summaryTone =
    flow.summaryKey === 'gameBoard.flowSummary.finished'
      ? 'error'
      : flow.summaryKey === 'gameBoard.flowSummary.awaitingModifiers' ||
          flow.summaryKey === 'gameBoard.flowSummary.roundInProgress' ||
          flow.summaryKey === 'gameBoard.flowSummary.reviewingResults'
        ? 'success'
        : 'info'

  return (
    <AdminBlock title={t('gameBoard.flowTitle')} tooltip={t('gameBoard.flowTooltip')}>
      <Stack spacing={1}>
        <InlineStateNotice tone={summaryTone}>{t(flow.summaryKey)}</InlineStateNotice>

        <Stack spacing={0.8}>
          {flow.steps.map((step, index) => (
            <Box
              key={step.id}
              sx={(theme) => {
                const palette = getFlowStepPalette(theme, step.state)

                return {
                  border: `1px solid ${palette.border}`,
                  backgroundColor: palette.background,
                  borderRadius: 1.5,
                  px: 1,
                  py: 0.95,
                }
              }}
            >
              <Stack direction="row" spacing={1} alignItems="flex-start">
                <Box
                  sx={(theme) => {
                    const palette = getFlowStepPalette(theme, step.state)

                    return {
                      width: 24,
                      height: 24,
                      borderRadius: '50%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      flexShrink: 0,
                      color: palette.accent,
                      border: `1px solid ${palette.border}`,
                      backgroundColor: alpha(theme.palette.common.black, 0.18),
                      fontSize: '0.8rem',
                      fontWeight: 800,
                    }
                  }}
                >
                  {index + 1}
                </Box>

                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Stack
                    direction="row"
                    spacing={0.75}
                    alignItems="center"
                    justifyContent="space-between"
                    flexWrap="wrap"
                    useFlexGap
                  >
                    <Typography variant="subtitle2" fontWeight={700}>
                      {t(step.titleKey)}
                    </Typography>
                    <FlowStateBadge state={step.state} />
                  </Stack>

                  <Typography variant="body2" color="text.secondary">
                    {t(step.descriptionKey)}
                  </Typography>
                </Box>
              </Stack>
            </Box>
          ))}
        </Stack>
      </Stack>
    </AdminBlock>
  )
}

function FlowStateBadge({
  state,
}: {
  state: ReturnType<typeof buildGameManagementFlow>['steps'][number]['state']
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => {
        const palette = getFlowStepPalette(theme, state)

        return {
          border: `1px solid ${palette.border}`,
          backgroundColor: palette.badgeBackground,
          color: palette.accent,
          borderRadius: 999,
          px: 0.85,
          py: 0.2,
          fontSize: '0.7rem',
          fontWeight: 700,
          letterSpacing: '0.03em',
          lineHeight: 1.2,
        }
      }}
    >
      {t(`gameBoard.flowStepState.${state}`)}
    </Box>
  )
}

function buildRoundActionModel({
  t,
  snapshot,
  activeRun,
  selectedActiveTeam,
  resumableTeam,
  onStartRound,
  onReviewRound,
  onOpenSummary,
  onResumeTeam,
}: {
  t: ReturnType<typeof useTranslation>['t']
  snapshot: GameBoardSnapshot
  activeRun: GameCardRunDetails | null
  selectedActiveTeam: GameTeamQueueItem | null
  resumableTeam: GameTeamQueueItem | null
  onStartRound: (input: { cellId: string; teamId: string }) => void
  onReviewRound: (cardRunId: string) => void
  onOpenSummary: () => void
  onResumeTeam: (teamId: string) => void
}): RoundActionModel {
  if (snapshot.status !== 'active') {
    return {
      statusTone: 'info',
      statusLabel: t('gameBoard.managementLaunchTitle'),
      title: t('gameBoard.managementRoundIdleDescription'),
      description: t('gameBoard.managementActiveTeamInactive'),
      actionLabel: null,
      actionTone: 'primary',
      onAction: null,
    }
  }

  if (activeRun?.status === 'awaiting_modifiers') {
    return {
      statusTone: 'warning',
      statusLabel: t('gameBoard.managementRoundAwaitingTitle'),
      title: t('gameBoard.runPanelStart'),
      description: t('gameBoard.flowSummary.awaitingModifiers'),
      actionLabel: t('gameBoard.runPanelStart'),
      actionTone: 'primary',
      onAction: () =>
        onStartRound({
          cellId: activeRun.cellId,
          teamId: activeRun.teamId,
        }),
    }
  }

  if (activeRun?.status === 'in_progress') {
    return {
      statusTone: 'success',
      statusLabel: t('gameBoard.managementRoundActiveTitle'),
      title: t('gameBoard.runPanelReview'),
      description: t('gameBoard.flowSummary.roundInProgress'),
      actionLabel: t('gameBoard.runPanelReview'),
      actionTone: 'primary',
      onAction: () => onReviewRound(activeRun.cardRunId),
    }
  }

  if (activeRun?.status === 'reviewing_results') {
    return {
      statusTone: 'success',
      statusLabel: t('gameBoard.managementRoundReviewingTitle'),
      title: t('gameBoard.runPanelOpenSummary'),
      description: t('gameBoard.flowSummary.reviewingResults'),
      actionLabel: t('gameBoard.runPanelOpenSummary'),
      actionTone: 'success',
      onAction: onOpenSummary,
    }
  }

  if (selectedActiveTeam) {
    return {
      statusTone: 'info',
      statusLabel: t('gameBoard.flowSteps.select_card.title'),
      title: t('gameBoard.flowSteps.select_card.title'),
      description: t('gameBoard.managementRoundNextActionBoardHint'),
      actionLabel: null,
      actionTone: 'primary',
      onAction: null,
    }
  }

  if (resumableTeam) {
    return {
      statusTone: 'warning',
      statusLabel: t('gameBoard.flowSteps.select_team.title'),
      title: t('gameBoard.managementActiveTeamResumeAction'),
      description: t('gameBoard.managementActiveTeamResumeHint', {
        slot: resumableTeam.teamSlotIndex,
      }),
      actionLabel: t('gameBoard.managementActiveTeamResumeAction'),
      actionTone: 'primary',
      onAction: () => onResumeTeam(resumableTeam.teamId),
    }
  }

  return {
    statusTone: 'warning',
    statusLabel: t('gameBoard.flowSteps.select_team.title'),
    title: t('gameBoard.flowSteps.select_team.title'),
    description: t('gameBoard.managementRoundNextActionTeamHint'),
    actionLabel: null,
    actionTone: 'primary',
    onAction: null,
  }
}

function getFlowStepPalette(
  theme: Theme,
  state: ReturnType<typeof buildGameManagementFlow>['steps'][number]['state'],
) {
  const accent =
    state === 'complete'
      ? theme.palette.success.main
      : state === 'current'
        ? theme.palette.info.main
        : state === 'ready'
          ? theme.palette.warning.main
          : state === 'blocked'
            ? theme.palette.grey[600]
            : theme.palette.divider

  return {
    accent,
    border: alpha(accent, state === 'blocked' ? 0.4 : 0.6),
    background:
      state === 'blocked'
        ? alpha(theme.palette.common.black, 0.14)
        : alpha(accent, 0.1),
    badgeBackground:
      state === 'blocked'
        ? alpha(theme.palette.common.black, 0.22)
        : alpha(accent, 0.16),
  }
}
