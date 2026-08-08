import { AppToast, PageShell, PageStatePanel, SectionHeader } from '../../shared/ui/index.ts'
import { useTranslation } from 'react-i18next'
import { AdminRegistrationPanel } from '../game-registration/index.ts'
import { useTeamRegistrationsPage } from './use-team-registrations-page.ts'

export function TeamRegistrationsPage() {
  const { t } = useTranslation()
  const {
    adminSnapshotQuery,
    createAdminTeam,
    createAdminInvitation,
    assignPlayerToTeam,
    removePlayerFromTeam,
    cancelTeamInvitation,
    moveTeamToSlot,
    confirmTeam,
    rejectTeam,
    disbandTeam,
    teamPlayedState,
    updateTeamName,
    toastMessage,
    dismissToast,
  } = useTeamRegistrationsPage()

  if (adminSnapshotQuery.isLoading) {
    return (
      <PageStatePanel
        title={t('teamRegistrations.title')}
        message={t('teamRegistrations.loading')}
        showSpinner
      />
    )
  }

  if (adminSnapshotQuery.isError) {
    return (
      <PageStatePanel
        title={t('teamRegistrations.title')}
        message={t('teamRegistrations.errorLoading')}
        tone="error"
      />
    )
  }

  if (adminSnapshotQuery.data == null) {
    return (
      <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
        <PageStatePanel
          title={t('teamRegistrations.title')}
          message={t('teamRegistrations.notOpen')}
        />
      </PageShell>
    )
  }

  return (
    <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
      <SectionHeader
        title={t('teamRegistrations.title')}
        description={t('teamRegistrations.description')}
      />

      <AdminRegistrationPanel
        snapshot={adminSnapshotQuery.data}
        isCreatingTeam={createAdminTeam.isPending}
        isCreatingInvitation={(teamId) =>
          createAdminInvitation.isPending && createAdminInvitation.variables?.teamId === teamId
        }
        isAssigningPlayer={assignPlayerToTeam.isPending}
        isRemovingPlayer={(teamId, userId) =>
          removePlayerFromTeam.isPending &&
          removePlayerFromTeam.variables?.teamId === teamId &&
          removePlayerFromTeam.variables.userId === userId
        }
        isCancellingTeamInvitation={(teamId, invitationId) =>
          cancelTeamInvitation.isPending &&
          cancelTeamInvitation.variables?.teamId === teamId &&
          cancelTeamInvitation.variables.invitationId === invitationId
        }
        isMovingTeam={moveTeamToSlot.isPending}
        isConfirmingTeam={(teamId) => confirmTeam.isPending && confirmTeam.variables === teamId}
        isRejectingTeam={(teamId) => rejectTeam.isPending && rejectTeam.variables === teamId}
        isDisbandingTeam={(teamId) => disbandTeam.isPending && disbandTeam.variables === teamId}
        isTogglingPlayedState={(teamId) =>
          teamPlayedState.isUpdatingPlayedState && teamPlayedState.updatingTeamId === teamId
        }
        isUpdatingTeamName={(teamId) =>
          updateTeamName.isPending && updateTeamName.variables?.teamId === teamId
        }
        onCreateTeam={(recruitmentOpen, teamSlotId) =>
          createAdminTeam.mutate({ recruitmentOpen, teamSlotId })
        }
        onCreateInvitation={(teamSlotId, invitedUserId, teamId) =>
          createAdminInvitation.mutate({ teamSlotId, invitedUserId, teamId })
        }
        onAssignPlayer={(teamId, userId) => assignPlayerToTeam.mutate({ teamId, userId })}
        onRemovePlayer={(teamId, userId) => removePlayerFromTeam.mutate({ teamId, userId })}
        onCancelTeamInvitation={(teamId, invitationId) =>
          cancelTeamInvitation.mutate({ teamId, invitationId })
        }
        onMoveTeam={(teamId, targetTeamSlotId) =>
          moveTeamToSlot.mutate({ teamId, targetTeamSlotId })
        }
        onConfirmTeam={(teamId) => confirmTeam.mutate(teamId)}
        onRejectTeam={(teamId) => rejectTeam.mutate(teamId)}
        onDisbandTeam={(teamId) => disbandTeam.mutate(teamId)}
        onTogglePlayedState={(teamId, isPlayed) =>
          teamPlayedState.setTeamPlayedState({ teamId, isPlayed })
        }
        onUpdateTeamName={(teamId, name) => updateTeamName.mutate({ teamId, name })}
      />

      <AppToast
        message={toastMessage}
        onClose={dismissToast}
        severity="error"
        autoHideDuration={5000}
      />
      <AppToast
        message={teamPlayedState.toastMessage}
        onClose={teamPlayedState.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
    </PageShell>
  )
}
