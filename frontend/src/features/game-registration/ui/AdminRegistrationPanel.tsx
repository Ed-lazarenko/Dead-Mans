import {
  Box,
  Chip,
  Divider,
  IconButton,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useMemo, useState, type DragEvent, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameRegistrationAdminSnapshot,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, ConfirmDialog, SectionCard } from '../../../shared/ui/index.ts'
import { searchRegistrationPlayers } from '../model/player-search.ts'
import { formatRegistrationTeamStatus } from '../model/registration-team-status.ts'
import { AdminInvitePlayerDialog, type AdminInviteTeamTarget } from './AdminInvitePlayerDialog.tsx'
import { RegistrationTeamNameEditor } from './RegistrationTeamNameEditor.tsx'

interface AdminRegistrationPanelProps {
  snapshot: GameRegistrationAdminSnapshot
  isCreatingTeam: boolean
  isCreatingInvitation: (teamId: string) => boolean
  isAssigningPlayer: boolean
  isRemovingPlayer: (teamId: string, userId: string) => boolean
  isCancellingTeamInvitation: (teamId: string, invitationId: string) => boolean
  isMovingTeam: boolean
  isConfirmingTeam: (teamId: string) => boolean
  isRejectingTeam: (teamId: string) => boolean
  isDisbandingTeam: (teamId: string) => boolean
  isTogglingPlayedState: (teamId: string) => boolean
  isUpdatingTeamName: (teamId: string) => boolean
  onCreateTeam: (recruitmentOpen: boolean, teamSlotId?: string) => void
  onCreateInvitation: (teamSlotId: string, invitedUserId: string, teamId: string) => void
  onAssignPlayer: (teamId: string, userId: string) => void
  onRemovePlayer: (teamId: string, userId: string) => void
  onCancelTeamInvitation: (teamId: string, invitationId: string) => void
  onMoveTeam: (teamId: string, targetTeamSlotId: string) => void
  onConfirmTeam: (teamId: string) => void
  onRejectTeam: (teamId: string) => void
  onDisbandTeam: (teamId: string) => void
  onTogglePlayedState: (teamId: string, isPlayed: boolean) => void
  onUpdateTeamName: (teamId: string, name?: string) => void
}

type DragPayload = { kind: 'player'; userId: string } | { kind: 'team'; teamId: string }
type OrderedTeamEntry = AdminInviteTeamTarget

const registrationDragMimeType = 'application/x-deadmans-registration'
const defaultVisiblePlayersCount = 12
const maxVisibleSearchResults = 18
const teamActionButtonSx = {
  alignSelf: 'flex-start',
  flex: '0 0 auto',
  height: 32,
  whiteSpace: 'nowrap',
}
const teamReorderButtonSx = {
  border: 1,
  borderColor: 'divider',
  color: 'text.secondary',
  height: 32,
  width: 32,
  backgroundColor: 'action.hover',
  transition: 'background-color 120ms ease, border-color 120ms ease, color 120ms ease',
  '&:hover': {
    borderColor: 'primary.main',
    color: 'primary.main',
    backgroundColor: 'action.selected',
  },
  '&.Mui-disabled': {
    borderColor: 'divider',
    backgroundColor: 'transparent',
    opacity: 0.38,
  },
}
const createTeamButtonSx = {
  alignSelf: 'flex-start',
  flex: '0 0 auto',
  height: 40,
  whiteSpace: 'nowrap',
  width: { xs: '100%', sm: 'auto' },
}
const teamStatusHintSx = {
  display: '-webkit-box',
  minHeight: '2.5rem',
  overflow: 'hidden',
  WebkitBoxOrient: 'vertical',
  WebkitLineClamp: 2,
}
const minimumSearchLength = 2

function writeDragPayload(event: DragEvent<HTMLElement>, payload: DragPayload) {
  event.dataTransfer.effectAllowed = 'move'
  event.dataTransfer.setData(registrationDragMimeType, JSON.stringify(payload))
  event.dataTransfer.setData(
    'text/plain',
    payload.kind === 'player' ? payload.userId : payload.teamId,
  )
}

function readDragPayload(event: DragEvent<HTMLElement>): DragPayload | null {
  const rawPayload = event.dataTransfer.getData(registrationDragMimeType)
  if (!rawPayload) {
    return null
  }

  try {
    const parsed = JSON.parse(rawPayload) as Partial<DragPayload>
    if (parsed.kind === 'player' && typeof parsed.userId === 'string') {
      return { kind: 'player', userId: parsed.userId }
    }

    if (parsed.kind === 'team' && typeof parsed.teamId === 'string') {
      return { kind: 'team', teamId: parsed.teamId }
    }
  } catch {
    return null
  }

  return null
}

function PlayerCard({
  player,
  compact = false,
  onDragStart,
  onDragEnd,
  actions,
  testId,
}: {
  player: RegistrationPlayer
  compact?: boolean
  onDragStart?: (event: DragEvent<HTMLElement>) => void
  onDragEnd?: () => void
  actions?: ReactNode
  testId?: string
}) {
  return (
    <SectionCard
      data-testid={testId}
      inset
      draggable={Boolean(onDragStart)}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      sx={{
        p: compact ? 1 : 1.25,
        cursor: onDragStart ? 'grab' : undefined,
        minWidth: 0,
      }}
    >
      <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
        <Stack spacing={0.25} sx={{ minWidth: 0 }}>
          <Typography variant="body2" fontWeight={700} noWrap>
            {player.displayName}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap>
            @{player.login}
          </Typography>
        </Stack>
        {actions ? <Box sx={{ flexShrink: 0 }}>{actions}</Box> : null}
      </Stack>
    </SectionCard>
  )
}

function TeamHeaderChips({
  team,
  membersCount,
  t,
}: {
  team: RegistrationTeam
  membersCount: number
  t: ReturnType<typeof useTranslation>['t']
}) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
      <Chip
        label={t('gameApplication.adminPanel.slotLabel', { slot: team.teamSlotIndex })}
        draggable
        onDragStart={(event) => writeDragPayload(event, { kind: 'team', teamId: team.teamId })}
        sx={{ cursor: 'grab' }}
      />
      <Chip size="small" label={formatRegistrationTeamStatus(team.status, t)} />
      <Chip
        size="small"
        color={team.recruitmentOpen ? 'success' : 'default'}
        label={
          team.recruitmentOpen
            ? t('gameApplication.recruitmentOpen')
            : t('gameApplication.recruitmentClosed')
        }
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameApplication.adminPanel.membersChip', { count: membersCount })}
      />
      {team.isActiveInGame ? (
        <Chip size="small" color="primary" label={t('gameApplication.adminPanel.activeTeamChip')} />
      ) : null}
      {team.isPlayed ? (
        <Chip size="small" color="success" label={t('gameApplication.adminPanel.playedTeamChip')} />
      ) : null}
      {team.disbandRequestedAtUtc ? (
        <Chip
          size="small"
          color="warning"
          label={t('gameApplication.adminPanel.disbandRequestedChip')}
        />
      ) : null}
    </Stack>
  )
}

function TeamNameEditor({
  team,
  isUpdating,
  onUpdateName,
}: {
  team: RegistrationTeam
  isUpdating: boolean
  onUpdateName: (teamId: string, name?: string) => void
}) {
  const { t } = useTranslation()
  const currentName = team.name?.trim() ?? ''
  const fallbackName = t('gameApplication.adminPanel.teamTitle', { slot: team.teamSlotIndex })
  const canEdit = team.status === 'forming'

  return (
    <Stack spacing={0.8}>
      <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
        <Typography variant="subtitle2">{currentName || fallbackName}</Typography>
        {currentName ? (
          <Chip
            size="small"
            variant="outlined"
            label={t('gameApplication.adminPanel.slotLabel', { slot: team.teamSlotIndex })}
          />
        ) : null}
      </Stack>

      <RegistrationTeamNameEditor
        value={team.name}
        canEdit={canEdit}
        isSaving={isUpdating}
        onSave={(name) => onUpdateName(team.teamId, name)}
        buttonSx={{ ...teamActionButtonSx, mt: { md: 0.35 }, minWidth: 112 }}
      />
    </Stack>
  )
}

function TeamReorderButton({
  label,
  disabled,
  direction,
  onClick,
}: {
  label: string
  disabled: boolean
  direction: 'up' | 'down'
  onClick: () => void
}) {
  return (
    <Tooltip title={label} placement="right">
      <span>
        <IconButton
          size="small"
          aria-label={label}
          disabled={disabled}
          onClick={onClick}
          sx={teamReorderButtonSx}
        >
          <Box component="span" aria-hidden sx={{ fontSize: 18, fontWeight: 700, lineHeight: 1 }}>
            {direction === 'up' ? '↑' : '↓'}
          </Box>
        </IconButton>
      </span>
    </Tooltip>
  )
}

export function AdminRegistrationPanel({
  snapshot,
  isCreatingTeam,
  isCreatingInvitation,
  isAssigningPlayer,
  isRemovingPlayer,
  isCancellingTeamInvitation,
  isMovingTeam,
  isConfirmingTeam,
  isRejectingTeam,
  isDisbandingTeam,
  isTogglingPlayedState,
  isUpdatingTeamName,
  onCreateTeam,
  onCreateInvitation,
  onAssignPlayer,
  onRemovePlayer,
  onCancelTeamInvitation,
  onMoveTeam,
  onConfirmTeam,
  onRejectTeam,
  onDisbandTeam,
  onTogglePlayedState,
  onUpdateTeamName,
}: AdminRegistrationPanelProps) {
  const { t } = useTranslation()
  const [activeDropTeamId, setActiveDropTeamId] = useState<string | null>(null)
  const [activeDropTeamSlotId, setActiveDropTeamSlotId] = useState<string | null>(null)
  const [activeDragPayload, setActiveDragPayload] = useState<DragPayload | null>(null)
  const [playerQuery, setPlayerQuery] = useState('')
  const [inviteDialog, setInviteDialog] = useState<AdminInviteTeamTarget | null>(null)
  const [pendingDisbandTeam, setPendingDisbandTeam] = useState<RegistrationTeam | null>(null)
  const [pendingRemovePlayer, setPendingRemovePlayer] = useState<{
    teamId: string
    teamSlotIndex: number
    player: RegistrationPlayer
  } | null>(null)

  const sortedTeamSlots = useMemo(
    () => [...snapshot.teamSlots].sort((left, right) => left.teamSlotIndex - right.teamSlotIndex),
    [snapshot.teamSlots],
  )

  const teamsById = useMemo(
    () => new Map(snapshot.teams.map((team) => [team.teamId, team])),
    [snapshot.teams],
  )

  const orderedTeamEntries = useMemo(
    () =>
      sortedTeamSlots.reduce<OrderedTeamEntry[]>((entries, slot) => {
        if (!slot.teamId) {
          return entries
        }

        const team = teamsById.get(slot.teamId)
        if (!team) {
          return entries
        }

        entries.push({ slot, team })
        return entries
      }, []),
    [sortedTeamSlots, teamsById],
  )

  const playerSearch = useMemo(
    () =>
      searchRegistrationPlayers(snapshot.availablePlayers, {
        query: playerQuery,
        minQueryLength: minimumSearchLength,
        limit:
          playerQuery.trim().length === 0 ? defaultVisiblePlayersCount : maxVisibleSearchResults,
        includeAllWhenQueryEmpty: true,
      }),
    [playerQuery, snapshot.availablePlayers],
  )
  const normalizedPlayerQuery = playerSearch.normalizedQuery
  const visiblePlayers = playerSearch.visible
  const hiddenPlayersCount = playerSearch.hiddenCount
  const teamsCount = snapshot.teams.length
  const openTeamsCount = snapshot.teams.filter((team) => team.recruitmentOpen).length
  const confirmedTeamsCount = snapshot.teams.filter((team) => team.status === 'confirmed').length
  const hasAvailableCreateTeamSlot = sortedTeamSlots.some((slot) => slot.isAvailableForNewTeam)
  const disbandRequestEntries = orderedTeamEntries.filter(
    ({ team }) => team.disbandRequestedAtUtc != null,
  )

  const resolveDragPayload = (event: DragEvent<HTMLElement>) =>
    activeDragPayload ?? readDragPayload(event)

  const clearDragState = () => {
    setActiveDragPayload(null)
    setActiveDropTeamId(null)
    setActiveDropTeamSlotId(null)
  }

  return (
    <>
      <Stack spacing={2}>
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5}>
          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.adminPanel.summaryTeams')}
              </Typography>
              <Typography variant="subtitle2">
                {t('gameApplication.adminPanel.summaryTeamsValue', {
                  total: teamsCount,
                  open: openTeamsCount,
                })}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.adminPanel.summaryTeamsDescription')}
              </Typography>
            </Stack>
          </SectionCard>

          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.adminPanel.summaryPlayers')}
              </Typography>
              <Typography variant="subtitle2">
                {t('gameApplication.adminPanel.summaryPlayersValue', {
                  count: snapshot.availablePlayers.length,
                })}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.adminPanel.summaryPlayersDescription')}
              </Typography>
            </Stack>
          </SectionCard>

          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.adminPanel.summaryRules')}
              </Typography>
              <Typography variant="subtitle2">
                {t('gameApplication.adminPanel.summaryRulesValue', {
                  min: snapshot.minPlayersPerTeam,
                  max: snapshot.maxPlayersPerTeam,
                })}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.adminPanel.summaryRulesDescription', {
                  count: confirmedTeamsCount,
                })}
              </Typography>
            </Stack>
          </SectionCard>
        </Stack>

        {disbandRequestEntries.length > 0 ? (
          <SectionCard
            sx={{
              borderColor: 'warning.main',
              background:
                'linear-gradient(180deg, rgba(255, 193, 7, 0.18) 0%, rgba(0, 0, 0, 0.18) 100%)',
            }}
          >
            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              spacing={1.5}
              justifyContent="space-between"
              alignItems={{ xs: 'stretch', lg: 'center' }}
            >
              <Stack spacing={0.5}>
                <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                  <Chip
                    size="small"
                    color="warning"
                    label={t('gameApplication.adminPanel.disbandRequestsAlertChip', {
                      count: disbandRequestEntries.length,
                    })}
                  />
                  <Typography variant="subtitle1">
                    {t('gameApplication.adminPanel.disbandRequestsAlertTitle')}
                  </Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.adminPanel.disbandRequestsAlertDescription')}
                </Typography>
              </Stack>

              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                {disbandRequestEntries.slice(0, 3).map(({ team }) => (
                  <Chip
                    key={team.teamId}
                    color="warning"
                    variant="outlined"
                    label={t('gameApplication.adminPanel.disbandRequestsAlertTeam', {
                      slot: team.teamSlotIndex,
                      player:
                        team.disbandRequestedByDisplayName ?? t('gameApplication.unknownPlayer'),
                    })}
                  />
                ))}
              </Stack>
            </Stack>
          </SectionCard>
        ) : null}

        <SectionCard>
          <Stack spacing={2}>
            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              spacing={2}
              justifyContent="space-between"
              alignItems={{ xs: 'stretch', lg: 'flex-start' }}
            >
              <Stack spacing={0.5}>
                <Typography variant="subtitle1">{t('gameApplication.adminPanel.title')}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.adminPanel.description')}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {t('gameApplication.adminPanel.assignHint')}
                </Typography>
              </Stack>
            </Stack>

            <Divider />

            <Stack direction={{ xs: 'column', xl: 'row' }} spacing={2} alignItems="stretch">
              <SectionCard
                inset
                sx={{
                  width: { xs: '100%', xl: 320 },
                  flexShrink: 0,
                  alignSelf: 'stretch',
                  background:
                    'linear-gradient(180deg, rgba(198, 160, 95, 0.12) 0%, rgba(0, 0, 0, 0.16) 100%)',
                }}
              >
                <Stack spacing={1.5}>
                  <Stack spacing={0.5}>
                    <Typography variant="subtitle2">
                      {t('gameApplication.adminPanel.availablePlayers')}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('gameApplication.adminPanel.availablePlayersDescription')}
                    </Typography>
                  </Stack>

                  <TextField
                    fullWidth
                    size="small"
                    label={t('gameApplication.adminPanel.playerSearchLabel')}
                    placeholder={t('gameApplication.adminPanel.playerSearchPlaceholder')}
                    value={playerQuery}
                    onChange={(event) => setPlayerQuery(event.target.value)}
                  />

                  <Typography variant="caption" color="text.secondary">
                    {normalizedPlayerQuery.length === 0
                      ? t('gameApplication.adminPanel.playerSearchIdle', {
                          count: snapshot.availablePlayers.length,
                          visible: Math.min(
                            defaultVisiblePlayersCount,
                            snapshot.availablePlayers.length,
                          ),
                        })
                      : normalizedPlayerQuery.length < minimumSearchLength
                        ? t('gameApplication.adminPanel.playerSearchMin', {
                            min: minimumSearchLength,
                          })
                        : t('gameApplication.adminPanel.playerSearchResults', {
                            count: playerSearch.matches.length,
                          })}
                  </Typography>

                  <Stack spacing={1}>
                    {visiblePlayers.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">
                        {snapshot.availablePlayers.length === 0
                          ? t('gameApplication.adminPanel.noAvailablePlayers')
                          : t('gameApplication.adminPanel.noPlayersMatched')}
                      </Typography>
                    ) : (
                      visiblePlayers.map((player) => (
                        <PlayerCard
                          key={player.userId}
                          player={player}
                          testId={`admin-player-${player.userId}`}
                          onDragStart={(event) => {
                            const payload: DragPayload = { kind: 'player', userId: player.userId }
                            setActiveDragPayload(payload)
                            writeDragPayload(event, payload)
                          }}
                          onDragEnd={clearDragState}
                        />
                      ))
                    )}
                  </Stack>

                  {hiddenPlayersCount > 0 ? (
                    <Typography variant="caption" color="text.secondary">
                      {t('gameApplication.adminPanel.hiddenPlayersHint', {
                        count: hiddenPlayersCount,
                      })}
                    </Typography>
                  ) : null}
                </Stack>
              </SectionCard>

              <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
                {orderedTeamEntries.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    {t('gameApplication.adminPanel.emptyTeams')}
                  </Typography>
                ) : null}

                {orderedTeamEntries.map(({ slot, team }, index) => {
                  const previousEntry = orderedTeamEntries[index - 1]
                  const nextEntry = orderedTeamEntries[index + 1]
                  const membersCount = team.members.length
                  const pendingInvitations = team.pendingInvitations ?? []
                  const hasPendingInvitations = pendingInvitations.length > 0
                  const reservedPlayersCount = membersCount + pendingInvitations.length
                  const isTeamReady =
                    team.status === 'forming' &&
                    !hasPendingInvitations &&
                    membersCount >= snapshot.minPlayersPerTeam &&
                    membersCount <= snapshot.maxPlayersPerTeam
                  const canShowInvitePlayer = !team.recruitmentOpen
                  const canInvitePlayer =
                    canShowInvitePlayer &&
                    team.status === 'forming' &&
                    reservedPlayersCount < snapshot.maxPlayersPerTeam &&
                    snapshot.availablePlayers.length > 0
                  const isTeamSlotDropActive = activeDropTeamSlotId === slot.teamSlotId
                  const isTeamDropActive = activeDropTeamId === team.teamId

                  return (
                    <Stack key={slot.teamSlotId} direction="row" spacing={1} alignItems="stretch">
                      <Stack
                        spacing={0.75}
                        alignItems="center"
                        justifyContent="center"
                        sx={{ flex: '0 0 40px', alignSelf: 'stretch' }}
                      >
                        <TeamReorderButton
                          label={t('gameApplication.adminPanel.moveTeamUp')}
                          direction="up"
                          disabled={!previousEntry || isMovingTeam}
                          onClick={() =>
                            previousEntry
                              ? onMoveTeam(team.teamId, previousEntry.slot.teamSlotId)
                              : undefined
                          }
                        />
                        <TeamReorderButton
                          label={t('gameApplication.adminPanel.moveTeamDown')}
                          direction="down"
                          disabled={!nextEntry || isMovingTeam}
                          onClick={() =>
                            nextEntry
                              ? onMoveTeam(team.teamId, nextEntry.slot.teamSlotId)
                              : undefined
                          }
                        />
                      </Stack>

                      <SectionCard
                        data-testid={`admin-slot-${slot.teamSlotIndex}`}
                        inset
                        sx={{
                          flex: 1,
                          minWidth: 0,
                          borderStyle:
                            isTeamSlotDropActive || isTeamDropActive ? 'solid' : undefined,
                          borderColor:
                            isTeamSlotDropActive || isTeamDropActive ? 'primary.main' : undefined,
                          background:
                            isTeamSlotDropActive || isTeamDropActive
                              ? 'linear-gradient(180deg, rgba(198, 160, 95, 0.14) 0%, rgba(0, 0, 0, 0.08) 100%)'
                              : index % 2 === 1
                                ? 'linear-gradient(180deg, rgba(255, 255, 255, 0.035) 0%, rgba(198, 160, 95, 0.06) 100%)'
                                : undefined,
                        }}
                        onDragOver={(event) => {
                          const payload = resolveDragPayload(event)
                          if (!payload) {
                            return
                          }

                          if (payload.kind === 'team' && team.teamId === payload.teamId) {
                            return
                          }

                          event.preventDefault()

                          if (payload.kind === 'player') {
                            setActiveDropTeamId(team.teamId)
                            setActiveDropTeamSlotId(null)
                          }

                          if (payload.kind === 'team') {
                            setActiveDropTeamSlotId(slot.teamSlotId)
                            setActiveDropTeamId(null)
                          }
                        }}
                        onDragLeave={() => {
                          setActiveDropTeamId((current) =>
                            current === team.teamId ? null : current,
                          )
                          setActiveDropTeamSlotId((current) =>
                            current === slot.teamSlotId ? null : current,
                          )
                        }}
                        onDrop={(event) => {
                          event.preventDefault()
                          const payload = resolveDragPayload(event)
                          clearDragState()

                          if (!payload) {
                            return
                          }

                          if (payload.kind === 'player') {
                            onAssignPlayer(team.teamId, payload.userId)
                            return
                          }

                          if (payload.kind === 'team' && payload.teamId !== team.teamId) {
                            onMoveTeam(payload.teamId, slot.teamSlotId)
                          }
                        }}
                      >
                        <Stack spacing={1.5}>
                          <Stack
                            direction={{ xs: 'column', lg: 'row' }}
                            spacing={1.5}
                            justifyContent="space-between"
                            alignItems={{ xs: 'stretch', lg: 'flex-start' }}
                          >
                            <Stack spacing={1}>
                              <TeamHeaderChips team={team} membersCount={membersCount} t={t} />

                              <TeamNameEditor
                                team={team}
                                isUpdating={isUpdatingTeamName(team.teamId)}
                                onUpdateName={onUpdateTeamName}
                              />

                              <Typography
                                variant="body2"
                                color="text.secondary"
                                sx={teamStatusHintSx}
                              >
                                {team.isPlayed
                                  ? t('gameApplication.adminPanel.teamPlayedHint')
                                  : isTeamDropActive
                                    ? t('gameApplication.adminPanel.dropPlayer')
                                    : isTeamSlotDropActive
                                      ? t('gameApplication.adminPanel.dropTeam')
                                      : hasPendingInvitations
                                        ? t('gameApplication.adminPanel.teamPendingInvitesHint')
                                        : isTeamReady
                                          ? t('gameApplication.adminPanel.teamReadyHint')
                                          : t('gameApplication.adminPanel.teamNeedsPlayersHint', {
                                              min: snapshot.minPlayersPerTeam,
                                              max: snapshot.maxPlayersPerTeam,
                                            })}
                              </Typography>
                            </Stack>

                            <Stack
                              direction={{ xs: 'column', sm: 'row' }}
                              spacing={1}
                              alignItems="flex-start"
                              sx={{
                                flex: '0 0 auto',
                                flexWrap: 'wrap',
                                alignContent: 'flex-start',
                              }}
                            >
                              {canShowInvitePlayer ? (
                                <AppButton
                                  size="small"
                                  tone="secondary"
                                  sx={teamActionButtonSx}
                                  disabled={!canInvitePlayer || isCreatingInvitation(team.teamId)}
                                  onClick={() => setInviteDialog({ slot, team })}
                                >
                                  {t('gameApplication.adminPanel.invitePlayer')}
                                </AppButton>
                              ) : null}
                              <AppButton
                                size="small"
                                sx={teamActionButtonSx}
                                disabled={
                                  !isTeamReady ||
                                  isAssigningPlayer ||
                                  isMovingTeam ||
                                  isConfirmingTeam(team.teamId)
                                }
                                onClick={() => onConfirmTeam(team.teamId)}
                              >
                                {t('teamRegistrations.confirm')}
                              </AppButton>
                              <AppButton
                                size="small"
                                tone="warningGhost"
                                sx={teamActionButtonSx}
                                disabled={team.status !== 'forming' || isRejectingTeam(team.teamId)}
                                onClick={() => onRejectTeam(team.teamId)}
                              >
                                {t('teamRegistrations.reject')}
                              </AppButton>
                              <AppButton
                                size="small"
                                tone="warningGhost"
                                sx={teamActionButtonSx}
                                disabled={
                                  team.status !== 'confirmed' ||
                                  team.isActiveInGame ||
                                  isDisbandingTeam(team.teamId)
                                }
                                onClick={() => setPendingDisbandTeam(team)}
                              >
                                {t('gameApplication.adminPanel.disbandTeam')}
                              </AppButton>
                              {snapshot.gameStatus === 'active' && team.status === 'confirmed' ? (
                                <AppButton
                                  size="small"
                                  tone={team.isPlayed ? 'secondary' : 'warningGhost'}
                                  sx={teamActionButtonSx}
                                  disabled={isTogglingPlayedState(team.teamId)}
                                  onClick={() => onTogglePlayedState(team.teamId, !team.isPlayed)}
                                >
                                  {team.isPlayed
                                    ? t('gameApplication.adminPanel.resetPlayedTeam')
                                    : t('gameApplication.adminPanel.markPlayedTeam')}
                                </AppButton>
                              ) : null}
                            </Stack>
                          </Stack>

                          {team.disbandRequestedAtUtc ? (
                            <SectionCard inset variantStyle="dashed">
                              <Stack spacing={0.5}>
                                <Typography variant="subtitle2">
                                  {t('gameApplication.adminPanel.disbandRequestTitle')}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                  {t('gameApplication.adminPanel.disbandRequestDescription', {
                                    player:
                                      team.disbandRequestedByDisplayName ??
                                      t('gameApplication.unknownPlayer'),
                                  })}
                                </Typography>
                              </Stack>
                            </SectionCard>
                          ) : null}

                          <Box
                            sx={{
                              display: 'grid',
                              gap: 1,
                              gridTemplateColumns: {
                                xs: 'minmax(0, 1fr)',
                                md: 'repeat(2, minmax(0, 1fr))',
                              },
                            }}
                          >
                            {team.members.length === 0 && pendingInvitations.length === 0 ? (
                              <SectionCard inset variantStyle="dashed">
                                <Typography variant="body2" color="text.secondary">
                                  {t('gameApplication.adminPanel.emptyTeam')}
                                </Typography>
                              </SectionCard>
                            ) : (
                              team.members.map((member) => (
                                <PlayerCard
                                  key={member.player.userId}
                                  player={member.player}
                                  compact
                                  testId={`admin-player-${member.player.userId}`}
                                  actions={
                                    <AppButton
                                      size="small"
                                      tone="warningGhost"
                                      disabled={isRemovingPlayer(team.teamId, member.player.userId)}
                                      onClick={() =>
                                        setPendingRemovePlayer({
                                          teamId: team.teamId,
                                          teamSlotIndex: team.teamSlotIndex,
                                          player: member.player,
                                        })
                                      }
                                    >
                                      {t('gameApplication.adminPanel.removePlayer')}
                                    </AppButton>
                                  }
                                  onDragStart={(event) => {
                                    const payload: DragPayload = {
                                      kind: 'player',
                                      userId: member.player.userId,
                                    }
                                    setActiveDragPayload(payload)
                                    writeDragPayload(event, payload)
                                  }}
                                  onDragEnd={clearDragState}
                                />
                              ))
                            )}
                            {pendingInvitations.map((invitation) => (
                              <SectionCard
                                key={invitation.invitationId}
                                inset
                                variantStyle="dashed"
                                sx={(theme) => ({
                                  borderColor: 'warning.main',
                                  backgroundColor: theme.palette.action.hover,
                                })}
                              >
                                <Stack spacing={0.5}>
                                  <Typography variant="body2" fontWeight={700} noWrap>
                                    {invitation.player.displayName}
                                  </Typography>
                                  <Typography variant="caption" color="text.secondary" noWrap>
                                    @{invitation.player.login}
                                  </Typography>
                                  <Chip
                                    size="small"
                                    color="warning"
                                    label={t('gameApplication.adminPanel.pendingInviteChip')}
                                    sx={{ alignSelf: 'flex-start' }}
                                  />
                                  <AppButton
                                    size="small"
                                    tone="warningGhost"
                                    disabled={isCancellingTeamInvitation(
                                      team.teamId,
                                      invitation.invitationId,
                                    )}
                                    onClick={() =>
                                      onCancelTeamInvitation(team.teamId, invitation.invitationId)
                                    }
                                  >
                                    {t('gameApplication.adminPanel.cancelPendingInvite')}
                                  </AppButton>
                                </Stack>
                              </SectionCard>
                            ))}
                          </Box>
                        </Stack>
                      </SectionCard>
                    </Stack>
                  )
                })}

                <SectionCard inset>
                  <Stack
                    direction={{ xs: 'column', lg: 'row' }}
                    spacing={1.5}
                    justifyContent="space-between"
                    alignItems={{ xs: 'stretch', lg: 'center' }}
                  >
                    <Stack spacing={0.5}>
                      <Typography variant="subtitle2">
                        {t('gameApplication.adminPanel.createTeamActionsTitle')}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {t('gameApplication.adminPanel.createTeamActionsDescription')}
                      </Typography>
                    </Stack>

                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={1}
                      alignItems="flex-start"
                      sx={{ flex: '0 0 auto' }}
                    >
                      <AppButton
                        sx={createTeamButtonSx}
                        disabled={isCreatingTeam || !hasAvailableCreateTeamSlot}
                        onClick={() => onCreateTeam(true)}
                      >
                        {t('gameApplication.adminPanel.createOpenTeam')}
                      </AppButton>
                      <AppButton
                        tone="secondary"
                        sx={createTeamButtonSx}
                        disabled={isCreatingTeam || !hasAvailableCreateTeamSlot}
                        onClick={() => onCreateTeam(false)}
                      >
                        {t('gameApplication.adminPanel.createPrivateTeam')}
                      </AppButton>
                    </Stack>
                  </Stack>
                </SectionCard>
              </Stack>
            </Stack>
          </Stack>
        </SectionCard>
      </Stack>

      <ConfirmDialog
        open={pendingDisbandTeam !== null}
        onClose={() => setPendingDisbandTeam(null)}
        onConfirm={() => {
          if (pendingDisbandTeam) {
            onDisbandTeam(pendingDisbandTeam.teamId)
            setPendingDisbandTeam(null)
          }
        }}
        isBusy={pendingDisbandTeam ? isDisbandingTeam(pendingDisbandTeam.teamId) : false}
        title={t('gameApplication.adminPanel.disbandConfirmTitle')}
        description={t('gameApplication.adminPanel.disbandConfirmDescription', {
          slot: pendingDisbandTeam?.teamSlotIndex ?? '-',
          count: pendingDisbandTeam?.members.length ?? 0,
        })}
        cancelLabel={t('gameApplication.adminPanel.disbandConfirmCancel')}
        confirmLabel={t('gameApplication.adminPanel.disbandConfirmAction')}
      />
      <ConfirmDialog
        open={pendingRemovePlayer !== null}
        onClose={() => setPendingRemovePlayer(null)}
        onConfirm={() => {
          if (pendingRemovePlayer) {
            onRemovePlayer(pendingRemovePlayer.teamId, pendingRemovePlayer.player.userId)
            setPendingRemovePlayer(null)
          }
        }}
        isBusy={
          pendingRemovePlayer
            ? isRemovingPlayer(pendingRemovePlayer.teamId, pendingRemovePlayer.player.userId)
            : false
        }
        title={t('gameApplication.adminPanel.removePlayerConfirmTitle')}
        description={t('gameApplication.adminPanel.removePlayerConfirmDescription', {
          player: pendingRemovePlayer?.player.displayName ?? t('gameApplication.unknownPlayer'),
          slot: pendingRemovePlayer?.teamSlotIndex ?? '-',
        })}
        cancelLabel={t('gameApplication.adminPanel.removePlayerConfirmCancel')}
        confirmLabel={t('gameApplication.adminPanel.removePlayerConfirmAction')}
      />
      <AdminInvitePlayerDialog
        target={inviteDialog}
        availablePlayers={snapshot.availablePlayers}
        isBusy={inviteDialog ? isCreatingInvitation(inviteDialog.team.teamId) : false}
        onClose={() => setInviteDialog(null)}
        onInvite={(teamSlotId, invitedUserId, teamId) => {
          onCreateInvitation(teamSlotId, invitedUserId, teamId)
          setInviteDialog(null)
        }}
      />
    </>
  )
}
