import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Chip,
  Divider,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import { alpha, type Theme } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardSnapshot,
  GameTeamQueueItem,
  GameTeamQueueSummary,
} from '../../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../../shared/ui/index.ts'
import { buildGameManagementFlow } from '../../model/game-management-flow.ts'
import type { GameRoundDetails, RoundActionModel } from '../../model/game-management-panel.ts'
import {
  formatGameStatusLabel,
  getGameStatusColor,
  formatManagementTeamName,
} from '../../model/game-management-panel.ts'

export function ManagementPanelHeader({
  snapshot,
  activeRound,
  currentActiveTeam,
  teamStats,
  onClose,
}: {
  snapshot: GameBoardSnapshot
  activeRound: GameRoundDetails | null
  currentActiveTeam: GameTeamQueueItem | null
  teamStats: GameTeamQueueSummary
  onClose: () => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        flexShrink: 0,
        borderBottom: `1px solid ${alpha(theme.palette.divider, 0.82)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.82),
        px: { xs: 1.25, sm: 1.5 },
        py: 1.2,
      })}
    >
      <Stack direction="row" spacing={1.25} alignItems="flex-start" justifyContent="space-between">
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="h6" fontWeight={850} sx={{ lineHeight: 1.15 }}>
            {t('gameBoard.managementPanelTitle')}
          </Typography>
          <Stack direction="row" spacing={0.6} flexWrap="wrap" useFlexGap sx={{ mt: 0.7 }}>
            <Chip
              size="small"
              color={getGameStatusColor(snapshot.status)}
              variant="outlined"
              label={formatGameStatusLabel(t, snapshot.status)}
            />
            <Chip
              size="small"
              variant="outlined"
              label={`${t('gameBoard.teamQueuePlayedChip')}: ${teamStats.playedTeams}/${teamStats.totalTeams}`}
            />
            <Chip
              size="small"
              variant="outlined"
              label={`${t('gameBoard.managementTeamsRemainingMetric')}: ${teamStats.remainingTeams}`}
            />
            <Chip
              size="small"
              color={currentActiveTeam ? 'success' : 'default'}
              variant={currentActiveTeam ? 'filled' : 'outlined'}
              label={
                currentActiveTeam
                  ? formatManagementTeamName(
                      t,
                      currentActiveTeam.teamName,
                      currentActiveTeam.teamSlotIndex,
                    )
                  : t('gameBoard.managementActiveTeamNone')
              }
            />
            <Chip
              size="small"
              color={activeRound ? 'warning' : 'default'}
              variant={activeRound ? 'filled' : 'outlined'}
              label={
                activeRound
                  ? t('gameBoard.activeRoundLabel', {
                      teamSlot: activeRound.teamSlotIndex,
                      score: activeRound.baseScore,
                    })
                  : t('gameBoard.managementPanelNoActiveRoundMetric')
              }
            />
          </Stack>
        </Box>
        <IconButton
          size="small"
          aria-label={t('gameBoard.managementPanelCloseAction')}
          onClick={onClose}
          sx={(theme) => ({
            border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
            flexShrink: 0,
          })}
        >
          <Box component="span" aria-hidden sx={{ fontSize: 20, lineHeight: 1 }}>
            ×
          </Box>
        </IconButton>
      </Stack>
    </Box>
  )
}

export function RoundAssistantSection({
  roundAction,
  isChangingRoundStage,
}: {
  roundAction: RoundActionModel
  isChangingRoundStage: boolean
}) {
  const { t } = useTranslation()

  return (
    <ControlSurface accent={roundAction.statusTone}>
      <Stack spacing={1.05}>
        <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
          <SectionTitle
            title={t('gameBoard.managementRoundAssistantTitle')}
            tooltip={t('gameBoard.managementRoundAssistantTooltip')}
          />
          <Stack direction="row" spacing={0.55} alignItems="center" flexWrap="wrap" useFlexGap>
            {roundAction.stepNumber ? (
              <Chip
                size="small"
                variant="outlined"
                label={t('gameBoard.managementRoundStepProgress', {
                  current: roundAction.stepNumber,
                  total: 6,
                })}
              />
            ) : null}
            <Chip
              size="small"
              color={roundAction.statusTone}
              variant="filled"
              label={roundAction.statusLabel}
            />
          </Stack>
        </Stack>

        <Box>
          <Typography variant="subtitle1" fontWeight={850}>
            {roundAction.title}
          </Typography>
          {roundAction.description &&
          (roundAction.stepId !== 'select_team' || roundAction.actionLabel) ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
              {roundAction.description}
            </Typography>
          ) : null}
        </Box>

        {roundAction.actionLabel && roundAction.onAction ? (
          <AppButton
            tone={roundAction.actionTone}
            size="medium"
            fullWidth
            disabled={isChangingRoundStage}
            onClick={roundAction.onAction}
            sx={{ minHeight: 46, fontWeight: 850 }}
          >
            {roundAction.actionLabel}
          </AppButton>
        ) : null}
      </Stack>
    </ControlSurface>
  )
}

export function TeamControlSection({
  isActiveGame,
  isLoading,
  isError,
  isSelectingActiveTeam,
  isUpdatingPlayedState,
  isActiveTeamLocked,
  teams,
  selectableTeams,
  currentActiveTeam,
  resumableTeam,
  onSelectActiveTeam,
  onSetTeamPlayedState,
}: {
  isActiveGame: boolean
  isLoading: boolean
  isError: boolean
  isSelectingActiveTeam: boolean
  isUpdatingPlayedState: boolean
  isActiveTeamLocked: boolean
  teams: readonly GameTeamQueueItem[]
  selectableTeams: readonly GameTeamQueueItem[]
  currentActiveTeam: GameTeamQueueItem | null
  resumableTeam: GameTeamQueueItem | null
  onSelectActiveTeam: (teamId: string | null) => void | Promise<unknown>
  onSetTeamPlayedState: (input: { teamId: string; isPlayed: boolean }) => void | Promise<unknown>
}) {
  const { t } = useTranslation()
  const spotlightTeam = currentActiveTeam ?? resumableTeam
  const isTeamControlBusy = isSelectingActiveTeam || isUpdatingPlayedState

  return (
    <ControlSurface accent="info">
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
          <SectionTitle
            title={t('gameBoard.managementActiveTeamTitle')}
            tooltip={t('gameBoard.managementActiveTeamTooltip')}
          />
          <Chip
            size="small"
            variant="outlined"
            label={t('gameBoard.managementTeamsRemainingMetricValue', {
              count: selectableTeams.length,
            })}
          />
        </Stack>

        {!isActiveGame ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamInactive')}
          </Typography>
        ) : isLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamLoading')}
          </Typography>
        ) : isError ? (
          <Typography variant="body2" color="error.main">
            {t('gameBoard.managementActiveTeamError')}
          </Typography>
        ) : teams.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamNoTeams')}
          </Typography>
        ) : (
          <>
            <TeamSpotlight
              team={spotlightTeam}
              isCurrent={currentActiveTeam !== null}
              description={
                currentActiveTeam
                  ? null
                  : resumableTeam
                    ? t('gameBoard.managementActiveTeamResumeHint', {
                        slot: resumableTeam.teamSlotIndex,
                      })
                    : t('gameBoard.managementActiveTeamRequired')
              }
            />

            {isActiveTeamLocked ? (
              <InlineStateNotice tone="warning">
                {t('gameBoard.managementActiveTeamLocked')}
              </InlineStateNotice>
            ) : null}

            {currentActiveTeam?.isPlayed ? (
              <InlineStateNotice tone="success">
                {t('gameBoard.teamPlayedSelectedNotice')}
              </InlineStateNotice>
            ) : null}

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={0.75}>
              {resumableTeam && !currentActiveTeam ? (
                <AppButton
                  size="small"
                  tone="warningGhost"
                  disabled={isTeamControlBusy || isActiveTeamLocked}
                  onClick={() =>
                    onSetTeamPlayedState({
                      teamId: resumableTeam.teamId,
                      isPlayed: true,
                    })
                  }
                  sx={{ minHeight: 40 }}
                >
                  {t('gameBoard.teamPlayedMarkAction')}
                </AppButton>
              ) : null}

              {currentActiveTeam ? (
                <>
                  <AppButton
                    tone="secondary"
                    size="small"
                    onClick={() => onSelectActiveTeam(null)}
                    disabled={isTeamControlBusy || isActiveTeamLocked}
                    sx={{ minHeight: 40 }}
                  >
                    {t('gameBoard.managementActiveTeamClearAction')}
                  </AppButton>
                  <AppButton
                    size="small"
                    tone={currentActiveTeam.isPlayed ? 'secondary' : 'warningGhost'}
                    disabled={isTeamControlBusy || isActiveTeamLocked}
                    onClick={() =>
                      onSetTeamPlayedState({
                        teamId: currentActiveTeam.teamId,
                        isPlayed: !currentActiveTeam.isPlayed,
                      })
                    }
                    sx={{ minHeight: 40 }}
                  >
                    {currentActiveTeam.isPlayed
                      ? t('gameBoard.teamPlayedResetAction')
                      : t('gameBoard.teamPlayedMarkAction')}
                  </AppButton>
                </>
              ) : null}
            </Stack>

            <Divider />

            <Stack spacing={0.65}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 850 }}>
                {t('gameBoard.managementActiveTeamQuickListTitle')}
              </Typography>
              <Stack spacing={0.55} sx={{ maxHeight: 320, overflowY: 'auto', pr: 0.25 }}>
                {teams.map((team) => {
                  const isCurrent = team.teamId === currentActiveTeam?.teamId
                  const isDisabled =
                    isTeamControlBusy || isActiveTeamLocked || team.isPlayed || isCurrent

                  return (
                    <CompactTeamRow
                      key={team.teamId}
                      team={team}
                      isCurrent={isCurrent}
                      disabled={isDisabled}
                      onSelect={() => onSelectActiveTeam(team.teamId)}
                    />
                  )
                })}
              </Stack>
            </Stack>

            {selectableTeams.length === 0 && !currentActiveTeam ? (
              <InlineStateNotice tone="info">
                {t('gameBoard.managementActiveTeamNoSelectableTeams')}
              </InlineStateNotice>
            ) : null}
          </>
        )}
      </Stack>
    </ControlSurface>
  )
}

function ControlSurface({
  accent,
  children,
}: {
  accent: 'info' | 'warning' | 'success'
  children: ReactNode
}) {
  return (
    <SectionCard
      sx={(theme) => ({
        p: 1.15,
        borderRadius: 2,
        border: `1px solid ${alpha(
          accent === 'success'
            ? theme.palette.success.main
            : accent === 'warning'
              ? theme.palette.warning.main
              : theme.palette.info.main,
          0.3,
        )}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.5),
      })}
    >
      {children}
    </SectionCard>
  )
}

export function SecondaryManagementSection({
  sectionId,
  title,
  tooltip,
  children,
  defaultExpanded = false,
}: {
  sectionId: string
  title: string
  tooltip: string
  children: ReactNode
  defaultExpanded?: boolean
}) {
  const headerId = `management-${sectionId}-header`
  const contentId = `management-${sectionId}-content`

  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      aria-labelledby={headerId}
      sx={(theme) => ({
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        overflow: 'hidden',
        '&::before': { display: 'none' },
      })}
    >
      <AccordionSummary
        id={headerId}
        aria-controls={contentId}
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 1.15,
          py: 0,
          minHeight: 46,
          '& .MuiAccordionSummary-content': {
            my: 0.65,
          },
        }}
      >
        <SectionTitle title={title} tooltip={tooltip} />
      </AccordionSummary>
      <AccordionDetails id={contentId} sx={{ px: 1.15, pt: 0, pb: 1.15 }}>
        {children}
      </AccordionDetails>
    </Accordion>
  )
}

function SectionTitle({ title, tooltip }: { title: string; tooltip: string }) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center" sx={{ minWidth: 0 }}>
      <Typography variant="subtitle2" fontWeight={850} noWrap>
        {title}
      </Typography>
      <HintTooltip title={tooltip} />
    </Stack>
  )
}

function TeamSpotlight({
  team,
  isCurrent,
  description,
}: {
  team: GameTeamQueueItem | null
  isCurrent: boolean
  description: string | null
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        borderRadius: 1.7,
        border: `1px solid ${alpha(theme.palette.info.main, 0.28)}`,
        backgroundColor: alpha(theme.palette.info.main, 0.07),
        px: 1,
        py: 0.85,
      })}
    >
      <Stack spacing={0.65}>
        <Stack direction="row" spacing={0.6} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 850 }}>
            {isCurrent
              ? t('gameBoard.managementActiveTeamCurrentLabel')
              : team
                ? t('gameBoard.managementActiveTeamRecentLabel')
                : t('gameBoard.managementActiveTeamNone')}
          </Typography>
          {isCurrent ? (
            <Chip
              size="small"
              color="success"
              variant="filled"
              label={t('gameBoard.teamQueueActiveChip')}
            />
          ) : null}
        </Stack>

        <Typography variant="subtitle1" fontWeight={850} noWrap>
          {team ? formatManagementTeamName(t, team.teamName, team.teamSlotIndex) : '-'}
        </Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary">
            {description}
          </Typography>
        ) : null}

        {team?.participants.length ? (
          <Typography variant="caption" color="text.secondary" noWrap>
            {team.participants.map((participant) => participant.displayName).join(', ')}
          </Typography>
        ) : null}
      </Stack>
    </Box>
  )
}

function CompactTeamRow({
  team,
  isCurrent,
  disabled,
  onSelect,
}: {
  team: GameTeamQueueItem
  isCurrent: boolean
  disabled: boolean
  onSelect: () => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      component="button"
      type="button"
      disabled={disabled}
      aria-pressed={isCurrent}
      onClick={onSelect}
      sx={(theme) => ({
        width: '100%',
        minWidth: 0,
        display: 'grid',
        gridTemplateColumns: '34px minmax(0, 1fr) auto',
        gap: 0.8,
        alignItems: 'center',
        borderRadius: 1.5,
        border: `1px solid ${
          isCurrent
            ? alpha(theme.palette.success.main, 0.42)
            : team.isPlayed
              ? alpha(theme.palette.success.main, 0.22)
              : alpha(theme.palette.divider, 0.76)
        }`,
        backgroundColor: isCurrent
          ? alpha(theme.palette.success.main, 0.12)
          : team.isPlayed
            ? alpha(theme.palette.success.main, 0.05)
            : alpha(theme.palette.background.paper, 0.34),
        color: 'inherit',
        cursor: disabled ? 'default' : 'pointer',
        textAlign: 'left',
        px: 0.85,
        py: 0.65,
        opacity: team.isPlayed && !isCurrent ? 0.68 : 1,
        transition: 'background-color 0.15s ease, border-color 0.15s ease',
        '&:hover:not(:disabled)': {
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
          borderColor: alpha(theme.palette.primary.main, 0.36),
        },
        '&:focus-visible': {
          outline: '2px solid',
          outlineColor: theme.palette.primary.main,
          outlineOffset: 2,
        },
      })}
    >
      <Box
        sx={(theme) => ({
          width: 28,
          height: 28,
          borderRadius: 1.2,
          display: 'grid',
          placeItems: 'center',
          border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
          backgroundColor: alpha(theme.palette.common.black, 0.1),
          fontSize: '0.78rem',
          fontWeight: 900,
        })}
      >
        #{team.teamSlotIndex}
      </Box>

      <Box sx={{ minWidth: 0 }}>
        <Typography variant="body2" sx={{ fontWeight: 820 }} noWrap>
          {formatManagementTeamName(t, team.teamName, team.teamSlotIndex)}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
          {team.participants.length > 0
            ? team.participants.map((participant) => participant.displayName).join(', ')
            : t('gameBoard.roundSummaryNoParticipants')}
        </Typography>
      </Box>

      <Stack direction="row" spacing={0.35} justifyContent="flex-end" flexWrap="wrap" useFlexGap>
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
    </Box>
  )
}

function ExpandGlyph() {
  return (
    <Box component="span" aria-hidden sx={{ fontSize: 18, lineHeight: 1 }}>
      ▾
    </Box>
  )
}

function HintTooltip({ title }: { title: string }) {
  return (
    <Tooltip title={title} arrow placement="top">
      <Box
        component="span"
        role="img"
        tabIndex={0}
        aria-label={title}
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
          '&:focus-visible': {
            outline: '2px solid',
            outlineColor: theme.palette.primary.main,
            outlineOffset: 2,
          },
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

export function ManagementFlowPanel({
  snapshot,
  activeRound,
}: {
  snapshot: GameBoardSnapshot
  activeRound: GameRoundDetails | null
}) {
  const { t } = useTranslation()
  const flow = buildGameManagementFlow(snapshot, activeRound)

  return (
    <Stack spacing={0.65}>
      {flow.steps.map((step, index) => (
        <Box
          key={step.id}
          sx={(theme) => {
            const palette = getFlowStepPalette(theme, step.state)

            return {
              border: `1px solid ${palette.border}`,
              backgroundColor: palette.background,
              borderRadius: 1.4,
              px: 0.85,
              py: 0.75,
            }
          }}
        >
          <Stack direction="row" spacing={0.8} alignItems="flex-start">
            <Box
              sx={(theme) => {
                const palette = getFlowStepPalette(theme, step.state)

                return {
                  width: 22,
                  height: 22,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                  color: palette.accent,
                  border: `1px solid ${palette.border}`,
                  backgroundColor: alpha(theme.palette.common.black, 0.12),
                  fontSize: '0.75rem',
                  fontWeight: 850,
                }
              }}
            >
              {index + 1}
            </Box>

            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack
                direction="row"
                spacing={0.65}
                alignItems="center"
                justifyContent="space-between"
                flexWrap="wrap"
                useFlexGap
              >
                <Typography variant="body2" fontWeight={780}>
                  {t(step.titleKey)}
                </Typography>
                <FlowStateBadge state={step.state} />
              </Stack>

              <Typography variant="caption" color="text.secondary">
                {t(step.descriptionKey)}
              </Typography>
            </Box>
          </Stack>
        </Box>
      ))}
    </Stack>
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
    background: state === 'blocked' ? alpha(theme.palette.common.black, 0.14) : alpha(accent, 0.1),
    badgeBackground:
      state === 'blocked' ? alpha(theme.palette.common.black, 0.22) : alpha(accent, 0.16),
  }
}
