import { Chip, Stack, Typography } from '@mui/material'
import type { DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import type { RegistrationPlayer, RegistrationTeam } from '../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../shared/ui/index.ts'
import { AdminRegistrationPlayerCard } from './admin-registration-components.tsx'
import { teamActionButtonSx, type RegistrationDragPayload } from './admin-registration-support.ts'

interface AdminRegistrationTeamRosterProps {
  team: RegistrationTeam
  isRemovingPlayer: (teamId: string, userId: string) => boolean
  isCancellingTeamInvitation: (teamId: string, invitationId: string) => boolean
  onRequestRemove: (request: {
    teamId: string
    teamSlotIndex: number
    player: RegistrationPlayer
  }) => void
  onCancelTeamInvitation: (teamId: string, invitationId: string) => void
  onPlayerDragStart: (event: DragEvent<HTMLElement>, payload: RegistrationDragPayload) => void
  onPlayerDragEnd: () => void
}

export function AdminRegistrationTeamRoster({
  team,
  isRemovingPlayer,
  isCancellingTeamInvitation,
  onRequestRemove,
  onCancelTeamInvitation,
  onPlayerDragStart,
  onPlayerDragEnd,
}: AdminRegistrationTeamRosterProps) {
  const { t } = useTranslation()
  const pendingInvitations = team.pendingInvitations ?? []

  return (
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
                onRequestRemove({
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
            onPlayerDragStart(event, payload)
          }}
          onDragEnd={onPlayerDragEnd}
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
            <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
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
            disabled={isCancellingTeamInvitation(team.teamId, invitation.invitationId)}
            onClick={() => onCancelTeamInvitation(team.teamId, invitation.invitationId)}
          >
            {t('gameApplication.adminPanel.cancelPendingInvite')}
          </AppButton>
        </Stack>
      ))}
    </Stack>
  )
}
