import { Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { RegistrationInvitation } from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'

interface PendingInvitationsSectionProps {
  invitations: RegistrationInvitation[]
  onAccept: (invitationId: string) => void
  onDecline: (invitationId: string) => void
  pendingAcceptId: string | undefined
  pendingDeclineId: string | undefined
}

export function PendingInvitationsSection({
  invitations,
  onAccept,
  onDecline,
  pendingAcceptId,
  pendingDeclineId,
}: PendingInvitationsSectionProps) {
  const { t } = useTranslation()

  if (invitations.length === 0) {
    return null
  }

  return (
    <SectionCard>
      <Stack spacing={2}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle1">{t('gameApplication.invitationsTitle')}</Typography>
            <Chip
              size="small"
              color="warning"
              label={t('gameApplication.invitationsChip', { count: invitations.length })}
            />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {t('gameApplication.invitationsDescription')}
          </Typography>
        </Stack>

        <Stack spacing={1}>
          {invitations.map((invitation) => (
            <SectionCard
              key={invitation.invitationId}
              inset
              sx={{
                display: 'flex',
                flexDirection: { xs: 'column', sm: 'row' },
                gap: 1.5,
                alignItems: { xs: 'stretch', sm: 'center' },
                justifyContent: 'space-between',
              }}
            >
              <Stack spacing={0.5}>
                <Typography variant="subtitle2">
                  {t('gameApplication.invitationSlot', { slot: invitation.teamSlotIndex })}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.invitationDescription')}
                </Typography>
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                <AppButton
                  size="small"
                  disabled={pendingAcceptId === invitation.invitationId}
                  onClick={() => onAccept(invitation.invitationId)}
                >
                  {t('gameApplication.acceptInvitation')}
                </AppButton>
                <AppButton
                  size="small"
                  tone="ghost"
                  disabled={pendingDeclineId === invitation.invitationId}
                  onClick={() => onDecline(invitation.invitationId)}
                >
                  {t('gameApplication.declineInvitation')}
                </AppButton>
              </Stack>
            </SectionCard>
          ))}
        </Stack>
      </Stack>
    </SectionCard>
  )
}
