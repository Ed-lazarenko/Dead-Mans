import { useQuery } from '@tanstack/react-query'
import {
  gameRegistrationAdminSnapshotQueryOptions,
  useAssignGameRegistrationPlayerToTeamMutation,
  useCancelGameRegistrationTeamInvitationMutation,
  useConfirmGameRegistrationTeamMutation,
  useCreateAdminGameRegistrationInvitationMutation,
  useCreateAdminGameRegistrationTeamMutation,
  useDisbandConfirmedGameRegistrationTeamMutation,
  useGameRegistrationToast,
  useMoveGameRegistrationTeamToSlotMutation,
  useRejectGameRegistrationTeamMutation,
  useRemoveGameRegistrationPlayerFromTeamMutation,
} from '../game-registration/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'

export function useTeamRegistrationsPage() {
  const { toastMessage, onMutationError, dismissToast } = useGameRegistrationToast()
  const createAdminTeam = useCreateAdminGameRegistrationTeamMutation(onMutationError)
  const createAdminInvitation = useCreateAdminGameRegistrationInvitationMutation(onMutationError)
  const assignPlayerToTeam = useAssignGameRegistrationPlayerToTeamMutation(onMutationError)
  const removePlayerFromTeam = useRemoveGameRegistrationPlayerFromTeamMutation(onMutationError)
  const cancelTeamInvitation = useCancelGameRegistrationTeamInvitationMutation(onMutationError)
  const moveTeamToSlot = useMoveGameRegistrationTeamToSlotMutation(onMutationError)
  const confirmTeam = useConfirmGameRegistrationTeamMutation(onMutationError)
  const rejectTeam = useRejectGameRegistrationTeamMutation(onMutationError)
  const disbandTeam = useDisbandConfirmedGameRegistrationTeamMutation(onMutationError)
  const gameBoardQuery = useQuery(currentGameBoardQueryOptions)
  const isTeamManagementAvailable =
    gameBoardQuery.data?.status === 'ready' || gameBoardQuery.data?.status === 'active'
  const registrationAdminSnapshotQuery = useQuery({
    ...gameRegistrationAdminSnapshotQueryOptions,
    enabled: isTeamManagementAvailable,
  })
  const adminSnapshotQuery = {
    data: isTeamManagementAvailable ? registrationAdminSnapshotQuery.data : null,
    isLoading:
      gameBoardQuery.isLoading ||
      (isTeamManagementAvailable && registrationAdminSnapshotQuery.isLoading),
    isError: gameBoardQuery.isError || registrationAdminSnapshotQuery.isError,
  }

  return {
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
    toastMessage,
    dismissToast,
  }
}
