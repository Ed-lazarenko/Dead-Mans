import { Box, Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { RegistrationTeam } from '../../../shared/api/contracts/index.ts'
import { formatRegistrationTeamStatus } from '../../game-registration/model/registration-team-status.ts'

export function TeamSummary({ team }: { team: RegistrationTeam }) {
  const { t } = useTranslation()
  const memberNames = team.members.map((member) => member.player.displayName)
  const pendingInvitations = team.pendingInvitations ?? []
  const teamDisplayName =
    team.name?.trim() || t('gameApplication.teamFallbackName', { slot: team.teamSlotIndex })

  return (
    <Box sx={{ minWidth: 0 }}>
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="subtitle2">{teamDisplayName}</Typography>
          <Chip
            size="small"
            variant="outlined"
            label={t('gameApplication.teamSlotChip', { slot: team.teamSlotIndex })}
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
        </Stack>

        <Typography variant="body2" color="text.secondary">
          {pendingInvitations.length > 0
            ? t('gameApplication.teamMembersWithPendingCount', {
                count: team.members.length,
                pending: pendingInvitations.length,
              })
            : t('gameApplication.teamMembersCount', { count: team.members.length })}
        </Typography>

        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          {memberNames.length > 0 || pendingInvitations.length > 0 ? (
            <>
              {memberNames.map((memberName) => (
                <Chip key={memberName} size="small" label={memberName} />
              ))}
              {pendingInvitations.map((invitation) => (
                <Chip
                  key={invitation.invitationId}
                  size="small"
                  color="warning"
                  variant="outlined"
                  label={t('gameApplication.pendingInvitedPlayerChip', {
                    player: invitation.player.displayName,
                  })}
                />
              ))}
            </>
          ) : (
            <Chip size="small" variant="outlined" label={t('gameApplication.emptyTeamMembers')} />
          )}
        </Stack>
      </Stack>
    </Box>
  )
}
