import { Chip, Collapse, Stack, Tooltip, Typography } from '@mui/material'
import { useMemo, useState, type DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameRegistrationAdminSnapshot,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, ConfirmDialog, SectionCard } from '../../../shared/ui/index.ts'
import { AdminInvitePlayerDialog, type AdminInviteTeamTarget } from './AdminInvitePlayerDialog.tsx'
import { AdminAvailablePlayersPanel } from './AdminAvailablePlayersPanel.tsx'
import { RegistrationTeamNameEditor } from './RegistrationTeamNameEditor.tsx'
import {
  AdminRegistrationOperationalStatus,
  AdminRegistrationPlayerCard,
  AdminRegistrationTeamHeaderChips,
  AdminRegistrationTeamNameSummary,
  AdminRegistrationTeamReorderButton,
  type OrderedAdminTeamEntry,
} from './admin-registration-components.tsx'
import {
  createTeamButtonSx,
  readRegistrationDragPayload,
  teamActionButtonSx,
  writeRegistrationDragPayload,
  type RegistrationDragPayload,
} from './admin-registration-support.ts'

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
  const [activeRegistrationDragPayload, setActiveRegistrationDragPayload] =
    useState<RegistrationDragPayload | null>(null)
  const [expandedActionTeamId, setExpandedActionTeamId] = useState<string | null>(null)
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
      sortedTeamSlots.reduce<OrderedAdminTeamEntry[]>((entries, slot) => {
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

  const readyTeamsCount = snapshot.teams.filter((team) => {
    const pendingInvitations = team.pendingInvitations ?? []
    const membersCount = team.members.length

    return (
      team.status === 'forming' &&
      pendingInvitations.length === 0 &&
      membersCount >= snapshot.minPlayersPerTeam &&
      membersCount <= snapshot.maxPlayersPerTeam
    )
  }).length
  const hasAvailableCreateTeamSlot = sortedTeamSlots.some((slot) => slot.isAvailableForNewTeam)
  const disbandRequestEntries = orderedTeamEntries.filter(
    ({ team }) => team.disbandRequestedAtUtc != null,
  )

  const resolveRegistrationDragPayload = (event: DragEvent<HTMLElement>) =>
    activeRegistrationDragPayload ?? readRegistrationDragPayload(event)

  const clearDragState = () => {
    setActiveRegistrationDragPayload(null)
    setActiveDropTeamId(null)
    setActiveDropTeamSlotId(null)
  }

  return (
    <>
      <Stack spacing={2}>
        <AdminRegistrationOperationalStatus
          readyTeamsCount={readyTeamsCount}
          availablePlayersCount={snapshot.availablePlayers.length}
          disbandRequestsCount={disbandRequestEntries.length}
          minPlayers={snapshot.minPlayersPerTeam}
          maxPlayers={snapshot.maxPlayersPerTeam}
        />

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

        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} alignItems="stretch">
          <AdminAvailablePlayersPanel
            players={snapshot.availablePlayers}
            onDragStart={(event, payload) => {
              setActiveRegistrationDragPayload(payload)
              writeRegistrationDragPayload(event, payload)
            }}
            onDragEnd={clearDragState}
          />
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
              const teamStatusHint = team.isPlayed
                ? t('gameApplication.adminPanel.teamPlayedHint')
                : isTeamDropActive
                  ? t('gameApplication.adminPanel.dropPlayer')
                  : isTeamSlotDropActive
                    ? t('gameApplication.adminPanel.dropTeam')
                    : team.status === 'confirmed'
                      ? t('gameApplication.adminPanel.teamConfirmedHint')
                      : hasPendingInvitations
                        ? t('gameApplication.adminPanel.teamPendingInvitesHint')
                        : isTeamReady
                          ? t('gameApplication.adminPanel.teamReadyHint')
                          : t('gameApplication.adminPanel.teamNeedsPlayersHint', {
                              min: snapshot.minPlayersPerTeam,
                              max: snapshot.maxPlayersPerTeam,
                            })

              return (
                <SectionCard
                  key={slot.teamSlotId}
                  data-testid={`admin-slot-${slot.teamSlotIndex}`}
                  inset
                  sx={{
                    minWidth: 0,
                    p: { xs: 1.25, sm: 1.5 },
                    borderStyle: isTeamSlotDropActive || isTeamDropActive ? 'solid' : undefined,
                    borderColor:
                      isTeamSlotDropActive || isTeamDropActive ? 'primary.main' : undefined,
                    background:
                      isTeamSlotDropActive || isTeamDropActive
                        ? 'linear-gradient(180deg, rgba(198, 160, 95, 0.14) 0%, rgba(0, 0, 0, 0.08) 100%)'
                        : undefined,
                  }}
                  onDragOver={(event) => {
                    const payload = resolveRegistrationDragPayload(event)
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
                    setActiveDropTeamId((current) => (current === team.teamId ? null : current))
                    setActiveDropTeamSlotId((current) =>
                      current === slot.teamSlotId ? null : current,
                    )
                  }}
                  onDrop={(event) => {
                    event.preventDefault()
                    const payload = resolveRegistrationDragPayload(event)
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
                  <Stack spacing={1}>
                    <Stack
                      direction={{ xs: 'column', lg: 'row' }}
                      spacing={1}
                      justifyContent="space-between"
                      alignItems={{ xs: 'stretch', lg: 'flex-start' }}
                    >
                      <Stack spacing={0.6} sx={{ minWidth: 0 }}>
                        <Stack
                          direction="row"
                          spacing={0.75}
                          alignItems="center"
                          flexWrap="wrap"
                          useFlexGap
                        >
                          <AdminRegistrationTeamNameSummary team={team} />
                          <AdminRegistrationTeamHeaderChips team={team} />
                        </Stack>

                        <Stack
                          direction="row"
                          spacing={0.75}
                          alignItems="center"
                          flexWrap="wrap"
                          useFlexGap
                        >
                          <Tooltip title={teamStatusHint} describeChild arrow>
                            <Chip
                              size="small"
                              color={
                                hasPendingInvitations
                                  ? 'warning'
                                  : isTeamReady
                                    ? 'success'
                                    : 'default'
                              }
                              variant={isTeamReady ? 'filled' : 'outlined'}
                              label={t('gameApplication.adminPanel.membersChip', {
                                count: membersCount,
                              })}
                              tabIndex={0}
                            />
                          </Tooltip>
                          <Typography variant="caption" color="text.secondary">
                            {team.recruitmentOpen
                              ? t('gameApplication.recruitmentOpen')
                              : t('gameApplication.recruitmentClosed')}
                            {hasPendingInvitations
                              ? ` · ${t('gameApplication.adminPanel.pendingInviteChip')}: ${pendingInvitations.length}`
                              : ''}
                          </Typography>
                          <Stack
                            role="group"
                            aria-label={t('gameApplication.adminPanel.slotLabel', {
                              slot: team.teamSlotIndex,
                            })}
                            direction="row"
                            spacing={0.25}
                            alignItems="center"
                          >
                            <AdminRegistrationTeamReorderButton
                              label={t('gameApplication.adminPanel.moveTeamUp')}
                              direction="up"
                              disabled={!previousEntry || isMovingTeam}
                              onClick={() =>
                                previousEntry
                                  ? onMoveTeam(team.teamId, previousEntry.slot.teamSlotId)
                                  : undefined
                              }
                            />
                            <AdminRegistrationTeamReorderButton
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
                        </Stack>
                      </Stack>

                      <Stack spacing={0.75} sx={{ flex: '0 0 auto', alignItems: 'stretch' }}>
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={0.75}
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
                          {team.disbandRequestedAtUtc ? (
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
                          ) : null}
                          {team.status === 'forming' || team.status === 'confirmed' ? (
                            <AppButton
                              size="small"
                              tone="ghost"
                              sx={teamActionButtonSx}
                              aria-expanded={expandedActionTeamId === team.teamId}
                              aria-controls={`team-actions-${team.teamId}`}
                              onClick={() =>
                                setExpandedActionTeamId((current) =>
                                  current === team.teamId ? null : team.teamId,
                                )
                              }
                            >
                              {expandedActionTeamId === team.teamId
                                ? t('gameApplication.adminPanel.hideTeamActions')
                                : t('gameApplication.adminPanel.moreTeamActions')}
                            </AppButton>
                          ) : null}
                        </Stack>

                        <Collapse
                          in={expandedActionTeamId === team.teamId}
                          timeout="auto"
                          unmountOnExit
                        >
                          <Stack
                            id={`team-actions-${team.teamId}`}
                            spacing={1}
                            sx={{ pt: 0.5, minWidth: { lg: 320 } }}
                          >
                            <RegistrationTeamNameEditor
                              value={team.name}
                              canEdit={team.status === 'forming'}
                              isSaving={isUpdatingTeamName(team.teamId)}
                              onSave={(name) => onUpdateTeamName(team.teamId, name)}
                              buttonSx={{ ...teamActionButtonSx, minWidth: 112 }}
                            />
                            <Stack
                              direction={{ xs: 'column', sm: 'row' }}
                              spacing={0.75}
                              alignItems={{ xs: 'stretch', sm: 'flex-start' }}
                              sx={{ flexWrap: 'wrap' }}
                            >
                              {team.status === 'forming' ? (
                                <AppButton
                                  size="small"
                                  tone="warningGhost"
                                  sx={teamActionButtonSx}
                                  disabled={isRejectingTeam(team.teamId)}
                                  onClick={() => onRejectTeam(team.teamId)}
                                >
                                  {t('teamRegistrations.reject')}
                                </AppButton>
                              ) : null}
                              {team.status === 'confirmed' && !team.disbandRequestedAtUtc ? (
                                <AppButton
                                  size="small"
                                  tone="warningGhost"
                                  sx={teamActionButtonSx}
                                  disabled={team.isActiveInGame || isDisbandingTeam(team.teamId)}
                                  onClick={() => setPendingDisbandTeam(team)}
                                >
                                  {t('gameApplication.adminPanel.disbandTeam')}
                                </AppButton>
                              ) : null}
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
                        </Collapse>
                      </Stack>
                    </Stack>

                    <Stack
                      component="ul"
                      spacing={0}
                      sx={(theme) => ({
                        m: 0,
                        p: 0,
                        borderTop: `1px solid ${theme.palette.divider}`,
                      })}
                    >
                      {team.members.length === 0 && pendingInvitations.length === 0 ? (
                        <Stack
                          component="li"
                          sx={(theme) => ({
                            listStyle: 'none',
                            p: 1.5,
                            border: `1px dashed ${theme.palette.divider}`,
                            borderRadius: theme.shape.borderRadius,
                          })}
                        >
                          <Typography variant="body2" color="text.secondary">
                            {t('gameApplication.adminPanel.emptyTeam')}
                          </Typography>
                        </Stack>
                      ) : null}

                      {team.members.map((member) => (
                        <AdminRegistrationPlayerCard
                          key={member.player.userId}
                          player={member.player}
                          compact
                          testId={`admin-player-${member.player.userId}`}
                          actions={
                            <AppButton
                              size="small"
                              tone="warningGhost"
                              sx={teamActionButtonSx}
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
                            const payload: RegistrationDragPayload = {
                              kind: 'player',
                              userId: member.player.userId,
                            }
                            setActiveRegistrationDragPayload(payload)
                            writeRegistrationDragPayload(event, payload)
                          }}
                          onDragEnd={clearDragState}
                        />
                      ))}

                      {pendingInvitations.map((invitation) => (
                        <Stack
                          component="li"
                          key={invitation.invitationId}
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1}
                          alignItems={{ xs: 'stretch', sm: 'center' }}
                          justifyContent="space-between"
                          sx={(theme) => ({
                            listStyle: 'none',
                            gap: 1,
                            py: 1,
                            px: 1,
                            borderBottom: `1px solid ${theme.palette.divider}`,
                            backgroundColor: theme.palette.action.hover,
                          })}
                        >
                          <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                            <Stack
                              direction="row"
                              spacing={0.75}
                              alignItems="center"
                              flexWrap="wrap"
                              useFlexGap
                            >
                              <Typography variant="body2" fontWeight={700} noWrap>
                                {invitation.player.displayName}
                              </Typography>
                              <Chip
                                size="small"
                                color="warning"
                                label={t('gameApplication.adminPanel.pendingInviteChip')}
                              />
                            </Stack>
                            <Typography variant="caption" color="text.secondary" noWrap>
                              @{invitation.player.login}
                            </Typography>
                          </Stack>
                          <AppButton
                            size="small"
                            tone="warningGhost"
                            sx={teamActionButtonSx}
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
                      ))}
                    </Stack>
                  </Stack>
                </SectionCard>
              )
            })}

            <SectionCard inset sx={{ p: 1.25 }}>
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={1}
                justifyContent="space-between"
                alignItems={{ xs: 'stretch', sm: 'center' }}
              >
                <Tooltip
                  title={t('gameApplication.adminPanel.createTeamActionsDescription')}
                  describeChild
                  arrow
                >
                  <Typography variant="subtitle2" tabIndex={0} sx={{ width: 'fit-content' }}>
                    {t('gameApplication.adminPanel.createTeamActionsTitle')}
                  </Typography>
                </Tooltip>

                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={0.75}
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
