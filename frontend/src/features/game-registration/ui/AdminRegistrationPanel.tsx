import { Chip, Stack, Tooltip, Typography } from '@mui/material'
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
import { AdminRegistrationTeamsList } from './AdminRegistrationTeamsList.tsx'
import {
  AdminRegistrationOperationalStatus,
  type OrderedAdminTeamEntry,
} from './admin-registration-components.tsx'
import {
  createTeamButtonSx,
  readRegistrationDragPayload,
  writeRegistrationDragPayload,
  type RegistrationDragPayload,
} from './admin-registration-support.ts'

export interface AdminRegistrationPanelProps {
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

export function AdminRegistrationPanel(props: AdminRegistrationPanelProps) {
  const {
    snapshot,
    isCreatingTeam,
    isCreatingInvitation,
    isRemovingPlayer,
    isDisbandingTeam,
    onCreateTeam,
    onCreateInvitation,
    onRemovePlayer,
    onDisbandTeam,
  } = props
  const { t } = useTranslation()
  const [activeDropTeamId, setActiveDropTeamId] = useState<string | null>(null)
  const [activeDropTeamSlotId, setActiveDropTeamSlotId] = useState<string | null>(null)
  const [activeRegistrationDragPayload, setActiveRegistrationDragPayload] =
    useState<RegistrationDragPayload | null>(null)
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

            <AdminRegistrationTeamsList
              controls={props}
              orderedTeamEntries={orderedTeamEntries}
              activeDropTeamId={activeDropTeamId}
              activeDropTeamSlotId={activeDropTeamSlotId}
              setActiveDropTeamId={setActiveDropTeamId}
              setActiveDropTeamSlotId={setActiveDropTeamSlotId}
              resolveDragPayload={resolveRegistrationDragPayload}
              clearDragState={clearDragState}
              onPlayerDragStart={(event, payload) => {
                setActiveRegistrationDragPayload(payload)
                writeRegistrationDragPayload(event, payload)
              }}
              onInvite={setInviteDialog}
              onRequestDisband={setPendingDisbandTeam}
              onRequestRemove={setPendingRemovePlayer}
            />

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
