import { useQuery } from '@tanstack/react-query'
import {
  gameRegistrationSnapshotQueryOptions,
  useAcceptGameRegistrationInvitationMutation,
  useCancelPlayerGameRegistrationInvitationMutation,
  useCreatePlayerGameRegistrationInvitationMutation,
  useCreateGameRegistrationTeamMutation,
  useDeclineGameRegistrationInvitationMutation,
  useGameRegistrationToast,
  useJoinGameRegistrationTeamMutation,
  useLeaveGameRegistrationTeamMutation,
  useRequestMyGameRegistrationTeamDisbandMutation,
} from '../game-registration/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'

export function useGameApplicationPage() {
  const { toastMessage, onMutationError, dismissToast } = useGameRegistrationToast()
  const createTeam = useCreateGameRegistrationTeamMutation(onMutationError)
  const joinTeam = useJoinGameRegistrationTeamMutation(onMutationError)
  const leaveTeam = useLeaveGameRegistrationTeamMutation(onMutationError)
  const acceptInvitation = useAcceptGameRegistrationInvitationMutation(onMutationError)
  const declineInvitation = useDeclineGameRegistrationInvitationMutation(onMutationError)
  const createPlayerInvitation = useCreatePlayerGameRegistrationInvitationMutation(onMutationError)
  const cancelPlayerInvitation = useCancelPlayerGameRegistrationInvitationMutation(onMutationError)
  const requestTeamDisband = useRequestMyGameRegistrationTeamDisbandMutation(onMutationError)
  const gameBoardQuery = useQuery(currentGameBoardQueryOptions)
  const isRegistrationOpen = gameBoardQuery.data?.status === 'ready'
  const registrationSnapshotQuery = useQuery({
    ...gameRegistrationSnapshotQueryOptions,
    enabled: isRegistrationOpen,
  })
  const snapshotQuery = {
    data: isRegistrationOpen ? registrationSnapshotQuery.data : null,
    isLoading:
      gameBoardQuery.isLoading || (isRegistrationOpen && registrationSnapshotQuery.isLoading),
    isError: gameBoardQuery.isError || registrationSnapshotQuery.isError,
  }

  return {
    snapshotQuery,
    createTeam,
    joinTeam,
    leaveTeam,
    acceptInvitation,
    declineInvitation,
    createPlayerInvitation,
    cancelPlayerInvitation,
    requestTeamDisband,
    toastMessage,
    dismissToast,
  }
}
