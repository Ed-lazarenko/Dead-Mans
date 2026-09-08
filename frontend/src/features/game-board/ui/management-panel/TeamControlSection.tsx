import { Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameTeamQueueItem } from '../../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../../shared/ui/index.ts'
import { formatManagementTeamName } from '../../model/game-management-panel.ts'
import {
  ManagementControlSurface,
  ManagementSectionTitle,
  ManagementStateNotice,
} from './ManagementPanelSurfaces.tsx'

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
    <ManagementControlSurface accent="info">
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
          <ManagementSectionTitle
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
              <ManagementStateNotice tone="warning">
                {t('gameBoard.managementActiveTeamLocked')}
              </ManagementStateNotice>
            ) : null}

            {currentActiveTeam?.isPlayed ? (
              <ManagementStateNotice tone="success">
                {t('gameBoard.teamPlayedSelectedNotice')}
              </ManagementStateNotice>
            ) : null}

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={0.75}>
              {resumableTeam && !currentActiveTeam ? (
                <AppButton
                  size="small"
                  tone="warningGhost"
                  disabled={isTeamControlBusy || isActiveTeamLocked}
                  onClick={() =>
                    onSetTeamPlayedState({ teamId: resumableTeam.teamId, isPlayed: true })
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
                {selectableTeams.map((team) => {
                  const isCurrent = team.teamId === currentActiveTeam?.teamId
                  const isDisabled = isTeamControlBusy || isActiveTeamLocked || isCurrent

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
              <ManagementStateNotice tone="info">
                {t('gameBoard.managementActiveTeamNoSelectableTeams')}
              </ManagementStateNotice>
            ) : null}
          </>
        )}
      </Stack>
    </ManagementControlSurface>
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
