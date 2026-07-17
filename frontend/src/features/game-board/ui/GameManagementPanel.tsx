import {
  Alert,
  Box,
  Chip,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardSnapshot,
  GameRegistrationAdminSnapshot,
  GameTeamQueueItem,
} from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { SectionCard } from '../../../shared/ui/index.ts'
import { AdminGameLaunchDrawer } from '../../game-registration/index.ts'
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
  launchPanel: GameManagementLaunchState
}

function getStatusColor(status: GameBoardSnapshot['status']) {
  if (status === 'active') {
    return 'success'
  }

  if (status === 'ready') {
    return 'info'
  }

  return 'default'
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
  launchPanel,
}: GameManagementPanelProps) {
  const { t } = useTranslation()
  const canShowLaunchAction = launchPanel.shouldRender && launchPanel.snapshot
  const isActiveGame = snapshot.status === 'active'
  const selectedActiveTeamId = snapshot.activeTeamId ?? ''

  return (
    <SectionCard
      component="aside"
      aria-label={t('gameBoard.managementPanelTitle')}
      sx={{
        width: { xs: '100%', xl: 340 },
        flex: { xs: '1 1 auto', xl: '0 0 340px' },
        alignSelf: 'stretch',
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
      }}
    >
      <Stack spacing={0.75}>
        <Typography variant="h6">{t('gameBoard.managementPanelTitle')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.managementPanelDescription')}
        </Typography>
      </Stack>

      <Divider />

      <Stack spacing={1}>
        <Typography variant="subtitle2">{t('gameBoard.managementStatusTitle')}</Typography>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
          <Chip
            size="small"
            color={getStatusColor(snapshot.status)}
            label={t(
              snapshot.status === 'active'
                ? 'gameBoard.statusActive'
                : snapshot.status === 'ready'
                  ? 'gameBoard.statusReady'
                  : 'gameBoard.statusFinished',
            )}
          />
          <Chip
            size="small"
            variant="outlined"
            label={t('gameBoard.managementBoardSize', {
              rows: snapshot.rows,
              cols: snapshot.cols,
            })}
          />
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {t(`gameBoard.managementStatusDescription.${snapshot.status}`)}
        </Typography>
      </Stack>

      <Divider />

      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.managementActiveTeamTitle')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.managementActiveTeamDescription')}
        </Typography>

        {!isActiveGame ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamInactive')}
          </Typography>
        ) : isTeamQueueLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamLoading')}
          </Typography>
        ) : isTeamQueueError ? (
          <Typography variant="body2" color="error">
            {t('gameBoard.managementActiveTeamError')}
          </Typography>
        ) : teams.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementActiveTeamNoTeams')}
          </Typography>
        ) : (
          <>
            <FormControl size="small" fullWidth disabled={isSelectingActiveTeam}>
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
              <Alert severity="warning" variant="outlined">
                {t('gameBoard.managementActiveTeamRequired')}
              </Alert>
            ) : null}
          </>
        )}
      </Stack>

      <Divider />

      <ManualQuizAwardControl
        isActiveGame={isActiveGame}
        players={manualQuizAwardPlayers}
        isLoading={isManualQuizAwardPlayersLoading}
        isError={isManualQuizAwardPlayersError}
        isAwarding={isAwardingManualQuizPoints}
        onAward={onAwardManualQuizPoints}
      />

      <Divider />

      <Stack spacing={1}>
        <Typography variant="subtitle2">{t('gameBoard.managementRoundTitle')}</Typography>
        {activeRun ? (
          <Box
            sx={(theme) => ({
              border: `1px solid ${alpha(theme.palette.warning.main, 0.44)}`,
              backgroundColor: alpha(theme.palette.warning.main, 0.1),
              px: 1.25,
              py: 1,
            })}
          >
            <Typography variant="body2" fontWeight={700}>
              {t('gameBoard.managementRoundActiveTitle')}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameBoard.activeRunLabel', {
                teamSlot: activeRun.teamSlotIndex,
                score: activeRun.baseScore,
              })}
            </Typography>
          </Box>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementRoundIdleDescription')}
          </Typography>
        )}
      </Stack>

      <Divider />

      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.managementLaunchTitle')}</Typography>
        {snapshot.status !== 'ready' ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementLaunchUnavailable')}
          </Typography>
        ) : launchPanel.isLoadingLaunchState ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementLaunchLoading')}
          </Typography>
        ) : canShowLaunchAction ? (
          <>
            <Typography variant="body2" color="text.secondary">
              {t('gameBoard.managementLaunchDescription')}
            </Typography>
            <AdminGameLaunchDrawer
              snapshot={launchPanel.snapshot}
              isStartingGame={launchPanel.isStartingGame}
              onStartGame={launchPanel.startGame}
            />
          </>
        ) : launchPanel.canStartGame ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementLaunchNoRegistrationState')}
          </Typography>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.managementLaunchAdminOnly')}
          </Typography>
        )}
      </Stack>
    </SectionCard>
  )
}
