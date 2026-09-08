import { Chip, Collapse, Stack, Tooltip, Typography } from '@mui/material'
import { useState, type Dispatch, type DragEvent, type SetStateAction } from 'react'
import { useTranslation } from 'react-i18next'
import type { RegistrationPlayer, RegistrationTeam } from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import type { AdminRegistrationPanelProps } from './AdminRegistrationPanel.tsx'
import {
  AdminRegistrationTeamHeaderChips,
  AdminRegistrationTeamNameSummary,
  AdminRegistrationTeamReorderButton,
  type OrderedAdminTeamEntry,
} from './admin-registration-components.tsx'
import { teamActionButtonSx, type RegistrationDragPayload } from './admin-registration-support.ts'
import { AdminRegistrationTeamRoster } from './AdminRegistrationTeamRoster.tsx'
import { RegistrationTeamNameEditor } from './RegistrationTeamNameEditor.tsx'

interface PendingRemovePlayer {
  teamId: string
  teamSlotIndex: number
  player: RegistrationPlayer
}

interface AdminRegistrationTeamsListProps {
  controls: AdminRegistrationPanelProps
  orderedTeamEntries: OrderedAdminTeamEntry[]
  activeDropTeamId: string | null
  activeDropTeamSlotId: string | null
  setActiveDropTeamId: Dispatch<SetStateAction<string | null>>
  setActiveDropTeamSlotId: Dispatch<SetStateAction<string | null>>
  resolveDragPayload: (event: DragEvent<HTMLElement>) => RegistrationDragPayload | null
  clearDragState: () => void
  onPlayerDragStart: (event: DragEvent<HTMLElement>, payload: RegistrationDragPayload) => void
  onInvite: (target: OrderedAdminTeamEntry) => void
  onRequestDisband: (team: RegistrationTeam) => void
  onRequestRemove: (request: PendingRemovePlayer) => void
}

export function AdminRegistrationTeamsList({
  controls,
  orderedTeamEntries,
  activeDropTeamId,
  activeDropTeamSlotId,
  setActiveDropTeamId,
  setActiveDropTeamSlotId,
  resolveDragPayload,
  clearDragState,
  onPlayerDragStart,
  onInvite,
  onRequestDisband,
  onRequestRemove,
}: AdminRegistrationTeamsListProps) {
  const { t } = useTranslation()
  const [expandedActionTeamId, setExpandedActionTeamId] = useState<string | null>(null)
  const {
    snapshot,
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
    onAssignPlayer,
    onCancelTeamInvitation,
    onMoveTeam,
    onConfirmTeam,
    onRejectTeam,
    onTogglePlayedState,
    onUpdateTeamName,
  } = controls

  return (
    <>
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
              borderColor: isTeamSlotDropActive || isTeamDropActive ? 'primary.main' : undefined,
              background:
                isTeamSlotDropActive || isTeamDropActive
                  ? 'linear-gradient(180deg, rgba(198, 160, 95, 0.14) 0%, rgba(0, 0, 0, 0.08) 100%)'
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
              setActiveDropTeamId((current) => (current === team.teamId ? null : current))
              setActiveDropTeamSlotId((current) => (current === slot.teamSlotId ? null : current))
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
                          hasPendingInvitations ? 'warning' : isTeamReady ? 'success' : 'default'
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
                          nextEntry ? onMoveTeam(team.teamId, nextEntry.slot.teamSlotId) : undefined
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
                        onClick={() => onInvite({ slot, team })}
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
                        onClick={() => onRequestDisband(team)}
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

                  <Collapse in={expandedActionTeamId === team.teamId} timeout="auto" unmountOnExit>
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
                            onClick={() => onRequestDisband(team)}
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

              <AdminRegistrationTeamRoster
                team={team}
                isRemovingPlayer={isRemovingPlayer}
                isCancellingTeamInvitation={isCancellingTeamInvitation}
                onRequestRemove={onRequestRemove}
                onCancelTeamInvitation={onCancelTeamInvitation}
                onPlayerDragStart={onPlayerDragStart}
                onPlayerDragEnd={clearDragState}
              />
            </Stack>
          </SectionCard>
        )
      })}
    </>
  )
}
