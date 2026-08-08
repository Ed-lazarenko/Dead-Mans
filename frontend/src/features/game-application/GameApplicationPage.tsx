import { Box, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { gameBoardRoute } from '../../routes/app-routes.ts'
import {
  AppLinkButton,
  AppToast,
  PageShell,
  PageStatePanel,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { CreateTeamSection } from './ui/CreateTeamSection.tsx'
import { MyTeamSection } from './ui/MyTeamSection.tsx'
import { OpenTeamsSection } from './ui/OpenTeamsSection.tsx'
import { PendingInvitationsSection } from './ui/PendingInvitationsSection.tsx'
import { useGameApplicationPage } from './use-game-application-page.ts'
import { SectionCard } from '../../shared/ui/index.ts'

export function GameApplicationPage() {
  const { t } = useTranslation()
  const {
    snapshotQuery,
    createTeam,
    joinTeam,
    leaveTeam,
    createPlayerInvitation,
    cancelPlayerInvitation,
    requestTeamDisband,
    updateTeamName,
    acceptInvitation,
    declineInvitation,
    toastMessage,
    dismissToast,
  } = useGameApplicationPage()

  if (snapshotQuery.isLoading) {
    return (
      <PageStatePanel
        title={t('gameApplication.title')}
        message={t('gameApplication.loading')}
        showSpinner
      />
    )
  }

  if (snapshotQuery.isError) {
    return (
      <PageStatePanel
        title={t('gameApplication.title')}
        message={t('gameApplication.errorLoading')}
        tone="error"
      />
    )
  }

  if (snapshotQuery.data == null) {
    return (
      <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
        <PageStatePanel title={t('gameApplication.title')} message={t('gameApplication.notOpen')} />
      </PageShell>
    )
  }

  const snapshot = snapshotQuery.data
  const joinableTeamsCount = snapshot.teams.filter(
    (team) => team.status === 'forming' && team.recruitmentOpen,
  ).length

  return (
    <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
      <SectionHeader
        title={t('gameApplication.title')}
        description={t('gameApplication.description')}
      />

      <Stack spacing={2.5}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.overviewTeamLabel')}
              </Typography>
              <Typography variant="subtitle2">
                {snapshot.myTeam
                  ? t('gameApplication.overviewTeamReady')
                  : t('gameApplication.overviewTeamMissing')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {snapshot.myTeam
                  ? t('gameApplication.overviewTeamReadyDescription')
                  : t('gameApplication.overviewTeamMissingDescription')}
              </Typography>
            </Stack>
          </SectionCard>

          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.overviewInvitationsLabel')}
              </Typography>
              <Typography variant="subtitle2">
                {t('gameApplication.overviewInvitationsValue', {
                  count: snapshot.myPendingInvitations.length,
                })}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.overviewInvitationsDescription')}
              </Typography>
            </Stack>
          </SectionCard>

          <SectionCard inset sx={{ flex: 1 }}>
            <Stack spacing={0.75}>
              <Typography variant="overline" color="text.secondary">
                {t('gameApplication.overviewTeamsLabel')}
              </Typography>
              <Typography variant="subtitle2">
                {t('gameApplication.overviewTeamsValue', {
                  total: snapshot.teams.length,
                  open: joinableTeamsCount,
                })}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.overviewTeamsDescription')}
              </Typography>
            </Stack>
          </SectionCard>
        </Stack>

        <PendingInvitationsSection
          invitations={snapshot.myPendingInvitations}
          onAccept={(invitationId) => acceptInvitation.mutate(invitationId)}
          onDecline={(invitationId) => declineInvitation.mutate(invitationId)}
          pendingAcceptId={acceptInvitation.isPending ? acceptInvitation.variables : undefined}
          pendingDeclineId={declineInvitation.isPending ? declineInvitation.variables : undefined}
        />

        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} alignItems="stretch">
          <Box sx={{ flex: 1, minWidth: 0 }}>
            {snapshot.myTeam ? (
              <MyTeamSection
                team={snapshot.myTeam}
                canInvitePlayers={snapshot.canInvitePlayersToMyTeam}
                invitablePlayers={snapshot.invitablePlayers}
                outgoingInvitations={snapshot.myOutgoingInvitations}
                onInvitePlayer={(userId) => createPlayerInvitation.mutate(userId)}
                isInvitingPlayer={createPlayerInvitation.isPending}
                onCancelInvitation={(invitationId) => cancelPlayerInvitation.mutate(invitationId)}
                isCancellingInvitation={cancelPlayerInvitation.isPending}
                onLeave={() => leaveTeam.mutate()}
                isLeaving={leaveTeam.isPending}
                onRequestDisband={() => requestTeamDisband.mutate()}
                isRequestingDisband={requestTeamDisband.isPending}
                onUpdateName={(name) => updateTeamName.mutate(name)}
                isUpdatingName={updateTeamName.isPending}
              />
            ) : (
              <CreateTeamSection
                onCreate={(recruitmentOpen, name) => createTeam.mutate({ recruitmentOpen, name })}
                isCreating={createTeam.isPending}
              />
            )}
          </Box>

          <Box sx={{ flex: 1, minWidth: 0 }}>
            <OpenTeamsSection
              teams={snapshot.teams}
              canJoinTeams={snapshot.myTeam === null}
              onJoin={(teamId) => joinTeam.mutate(teamId)}
              joiningTeamId={joinTeam.isPending ? joinTeam.variables : undefined}
            />
          </Box>
        </Stack>
      </Stack>

      <AppLinkButton to={gameBoardRoute.fullPath} sx={{ mt: 2 }} tone="ghost">
        {t('gameApplication.backToBoard')}
      </AppLinkButton>

      <AppToast
        message={toastMessage}
        onClose={dismissToast}
        severity="error"
        autoHideDuration={5000}
      />
    </PageShell>
  )
}
