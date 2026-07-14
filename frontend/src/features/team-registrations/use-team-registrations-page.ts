import { useQuery } from '@tanstack/react-query'
import {
  gameRegistrationAdminSnapshotQueryOptions,
  useAssignGameRegistrationPlayerToTeamMutation,
  useConfirmGameRegistrationTeamMutation,
  useCreateAdminGameRegistrationTeamMutation,
  useGameRegistrationToast,
  useMoveGameRegistrationTeamToSlotMutation,
  useRejectGameRegistrationTeamMutation,
} from '../game-registration/index.ts'

export function useTeamRegistrationsPage() {
  const { toastMessage, onMutationError, dismissToast } = useGameRegistrationToast()
  const createAdminTeam = useCreateAdminGameRegistrationTeamMutation(onMutationError)
  const assignPlayerToTeam = useAssignGameRegistrationPlayerToTeamMutation(onMutationError)
  const moveTeamToSlot = useMoveGameRegistrationTeamToSlotMutation(onMutationError)
  const confirmTeam = useConfirmGameRegistrationTeamMutation(onMutationError)
  const rejectTeam = useRejectGameRegistrationTeamMutation(onMutationError)
  const adminSnapshotQuery = useQuery(gameRegistrationAdminSnapshotQueryOptions)

  return {
    adminSnapshotQuery,
    createAdminTeam,
    assignPlayerToTeam,
    moveTeamToSlot,
    confirmTeam,
    rejectTeam,
    toastMessage,
    dismissToast,
  }
}
