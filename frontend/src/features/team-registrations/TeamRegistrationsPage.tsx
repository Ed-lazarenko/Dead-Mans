import { AppToast, PageShell, PageStatePanel, SectionHeader } from '../../shared/ui/index.ts'
import { useTranslation } from 'react-i18next'
import { AdminRegistrationPanel } from '../game-registration/index.ts'
import { useTeamRegistrationsPage } from './use-team-registrations-page.ts'

export function TeamRegistrationsPage() {
  const { t } = useTranslation()
  const {
    adminSnapshotQuery,
    createAdminTeam,
    assignPlayerToTeam,
    moveTeamToSlot,
    confirmTeam,
    rejectTeam,
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
        isAssigningPlayer={assignPlayerToTeam.isPending}
        isMovingTeam={moveTeamToSlot.isPending}
        isConfirmingTeam={(teamId) => confirmTeam.isPending && confirmTeam.variables === teamId}
        isRejectingTeam={(teamId) => rejectTeam.isPending && rejectTeam.variables === teamId}
        onCreateTeam={(recruitmentOpen, slotId) =>
          createAdminTeam.mutate({ recruitmentOpen, slotId })
        }
        onAssignPlayer={(teamId, userId) => assignPlayerToTeam.mutate({ teamId, userId })}
        onMoveTeam={(teamId, targetSlotId) => moveTeamToSlot.mutate({ teamId, targetSlotId })}
        onConfirmTeam={(teamId) => confirmTeam.mutate(teamId)}
        onRejectTeam={(teamId) => rejectTeam.mutate(teamId)}
      />

      <AppToast
        message={toastMessage}
        onClose={dismissToast}
        severity="error"
        autoHideDuration={5000}
      />
    </PageShell>
  )
}
