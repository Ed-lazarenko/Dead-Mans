import {
  Alert,
  Box,
  Chip,
  Drawer,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import { alpha, type Theme } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardSnapshot,
  GameRegistrationAdminSnapshot,
  GameTeamQueueItem,
} from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { AdminGameLaunchDrawer } from '../../game-registration/index.ts'
import { buildGameManagementFlow } from '../model/game-management-flow.ts'
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
  onCompleteRound: (cardRunId: string) => void
  launchPanel: GameManagementLaunchState
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
  launchPanel,
}: GameManagementPanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const canShowLaunchAction = launchPanel.shouldRender && launchPanel.snapshot
  const isActiveGame = snapshot.status === 'active'
  const selectedActiveTeamId = snapshot.activeTeamId ?? activeRun?.teamId ?? ''
  const isActiveTeamLocked = activeRun !== null

  return (
    <>
      <AppButton
        tone="secondary"
        size="small"
        onClick={() => setIsOpen(true)}
        sx={(theme) => ({
          position: 'fixed',
          zIndex: theme.zIndex.drawer - 1,
          right: { xs: 12, md: 0 },
          top: { xs: 'auto', md: '50%' },
          bottom: { xs: 16, md: 'auto' },
          transform: { xs: 'none', md: 'translateY(-50%)' },
          minWidth: { xs: 0, md: 44 },
          minHeight: { xs: 40, md: 164 },
          px: { xs: 1.5, md: 0.75 },
          py: { xs: 0.75, md: 1.25 },
          borderRadius: { xs: 999, md: '16px 0 0 16px' },
          writingMode: { xs: 'horizontal-tb', md: 'vertical-rl' },
          textOrientation: { xs: 'mixed', md: 'mixed' },
          justifyContent: 'center',
          boxShadow: `0 10px 24px ${alpha(theme.palette.common.black, 0.35)}`,
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
            width: { xs: '100vw', md: 460 },
            maxWidth: '100vw',
            p: 2,
          }}
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              spacing={1.5}
              alignItems="flex-start"
              justifyContent="space-between"
            >
              <Stack spacing={0.75}>
                <Typography variant="h6">{t('gameBoard.managementPanelTitle')}</Typography>
                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                  <Chip
                    variant="outlined"
                    label={t('gameBoard.managementBoardSize', {
                      rows: snapshot.rows,
                      cols: snapshot.cols,
                    })}
                  />
                  {activeRun ? (
                    <Chip
                      color="warning"
                      variant="outlined"
                      label={t('gameBoard.activeRunLabel', {
                        teamSlot: activeRun.teamSlotIndex,
                        score: activeRun.baseScore,
                      })}
                    />
                  ) : null}
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

            <ManagementFlowPanel snapshot={snapshot} activeRun={activeRun} />

            <AdminBlock
              title={t('gameBoard.managementActiveTeamTitle')}
              tooltip={t('gameBoard.managementActiveTeamTooltip')}
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
              ) : teams.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementActiveTeamNoTeams')}
                </Typography>
              ) : (
                <Stack spacing={1}>
                  <FormControl
                    size="small"
                    fullWidth
                    disabled={isSelectingActiveTeam || isActiveTeamLocked}
                  >
                    <InputLabel id="active-game-team-label">
                      {t('gameBoard.managementActiveTeamSelectLabel')}
                    </InputLabel>
                    <Select
                      labelId="active-game-team-label"
                      label={t('gameBoard.managementActiveTeamSelectLabel')}
                      value={selectedActiveTeamId}
                      onChange={(event) => onSelectActiveTeam(event.target.value || null)}
                    >
                      <MenuItem value="">{t('gameBoard.managementActiveTeamNone')}</MenuItem>
                      {teams.map((team) => (
                        <MenuItem key={team.teamId} value={team.teamId}>
                          {t('gameBoard.teamQueueTeamTitle', { slot: team.teamSlotIndex })}
                          {' · '}
                          {team.participants.map((participant) => participant.displayName).join(', ')}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  {selectedActiveTeamId === '' ? (
                    <InlineStateNotice tone="warning">
                      {t('gameBoard.managementActiveTeamRequired')}
                    </InlineStateNotice>
                  ) : null}
                  {isActiveTeamLocked ? (
                    <InlineStateNotice tone="warning">
                      {t('gameBoard.managementActiveTeamLocked')}
                    </InlineStateNotice>
                  ) : null}
                </Stack>
              )}
            </AdminBlock>

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
              title={t('gameBoard.managementRoundTitle')}
              tooltip={t('gameBoard.managementRoundTooltip')}
            >
              {activeRun ? (
                <Stack spacing={1}>
                  <Box
                    sx={(theme) => ({
                      border: `1px solid ${alpha(theme.palette.warning.main, 0.44)}`,
                      backgroundColor: alpha(theme.palette.warning.main, 0.1),
                      borderRadius: 1.5,
                      px: 1.25,
                      py: 1,
                    })}
                  >
                    <Typography variant="body2" fontWeight={700}>
                      {t(
                        activeRun.status === 'awaiting_modifiers'
                          ? 'gameBoard.managementRoundAwaitingTitle'
                          : 'gameBoard.managementRoundActiveTitle',
                      )}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('gameBoard.activeRunLabel', {
                        teamSlot: activeRun.teamSlotIndex,
                        score: activeRun.baseScore,
                      })}
                    </Typography>
                  </Box>
                  {activeRun.status === 'awaiting_modifiers' ? (
                    <AppButton
                      tone="primary"
                      size="small"
                      fullWidth
                      disabled={isChangingRoundStage}
                      onClick={() =>
                        onStartRound({
                          cellId: activeRun.cellId,
                          teamId: activeRun.teamId,
                        })
                      }
                    >
                      {t('gameBoard.runPanelStart')}
                    </AppButton>
                  ) : null}
                  {activeRun.status === 'in_progress' ? (
                    <AppButton
                      tone="primary"
                      size="small"
                      fullWidth
                      disabled={isChangingRoundStage}
                      onClick={() => onReviewRound(activeRun.cardRunId)}
                    >
                      {t('gameBoard.runPanelReview')}
                    </AppButton>
                  ) : null}
                  {activeRun.status === 'reviewing_results' ? (
                    <AppButton
                      tone="primary"
                      size="small"
                      fullWidth
                      disabled={isChangingRoundStage}
                      onClick={() => onCompleteRound(activeRun.cardRunId)}
                    >
                      {t('gameBoard.runPanelComplete')}
                    </AppButton>
                  ) : null}
                </Stack>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.managementRoundIdleDescription')}
                </Typography>
              )}
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
    </>
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
