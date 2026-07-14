import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  acceptGameRegistrationInvitationMutationOptions,
  assignGameRegistrationPlayerToTeamMutationOptions,
  cancelPlayerGameRegistrationInvitationMutationOptions,
  confirmGameRegistrationTeamMutationOptions,
  createAdminGameRegistrationTeamMutationOptions,
  createPlayerGameRegistrationInvitationMutationOptions,
  createGameRegistrationTeamMutationOptions,
  declineGameRegistrationInvitationMutationOptions,
  type GameRegistrationMutationErrorHandler,
  joinGameRegistrationTeamMutationOptions,
  leaveGameRegistrationTeamMutationOptions,
  moveGameRegistrationTeamToSlotMutationOptions,
  rejectGameRegistrationTeamMutationOptions,
} from './game-registration-mutation-options.ts'

export function useCreateGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(createGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useJoinGameRegistrationTeamMutation(onError: GameRegistrationMutationErrorHandler) {
  const queryClient = useQueryClient()
  return useMutation(joinGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useCreateAdminGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(createAdminGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useLeaveGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(leaveGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useAcceptGameRegistrationInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(acceptGameRegistrationInvitationMutationOptions(queryClient, onError))
}

export function useDeclineGameRegistrationInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(declineGameRegistrationInvitationMutationOptions(queryClient, onError))
}

export function useCreatePlayerGameRegistrationInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(createPlayerGameRegistrationInvitationMutationOptions(queryClient, onError))
}

export function useCancelPlayerGameRegistrationInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(cancelPlayerGameRegistrationInvitationMutationOptions(queryClient, onError))
}

export function useConfirmGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(confirmGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useRejectGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(rejectGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useAssignGameRegistrationPlayerToTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(assignGameRegistrationPlayerToTeamMutationOptions(queryClient, onError))
}

export function useMoveGameRegistrationTeamToSlotMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(moveGameRegistrationTeamToSlotMutationOptions(queryClient, onError))
}
