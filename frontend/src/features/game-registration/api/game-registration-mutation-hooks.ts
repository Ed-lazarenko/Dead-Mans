import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  acceptGameRegistrationInvitationMutationOptions,
  assignGameRegistrationPlayerToTeamMutationOptions,
  cancelGameRegistrationTeamInvitationMutationOptions,
  cancelPlayerGameRegistrationInvitationMutationOptions,
  confirmGameRegistrationTeamMutationOptions,
  createAdminGameRegistrationInvitationMutationOptions,
  createAdminGameRegistrationTeamMutationOptions,
  createPlayerGameRegistrationInvitationMutationOptions,
  createGameRegistrationTeamMutationOptions,
  declineGameRegistrationInvitationMutationOptions,
  disbandConfirmedGameRegistrationTeamMutationOptions,
  type GameRegistrationMutationErrorHandler,
  joinGameRegistrationTeamMutationOptions,
  leaveGameRegistrationTeamMutationOptions,
  moveGameRegistrationTeamToSlotMutationOptions,
  requestMyGameRegistrationTeamDisbandMutationOptions,
  rejectGameRegistrationTeamMutationOptions,
  removeGameRegistrationPlayerFromTeamMutationOptions,
  startGameFromRegistrationMutationOptions,
  updateAdminGameRegistrationTeamNameMutationOptions,
  updateMyGameRegistrationTeamNameMutationOptions,
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

export function useUpdateMyGameRegistrationTeamNameMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(updateMyGameRegistrationTeamNameMutationOptions(queryClient, onError))
}

export function useUpdateAdminGameRegistrationTeamNameMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(updateAdminGameRegistrationTeamNameMutationOptions(queryClient, onError))
}

export function useLeaveGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(leaveGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useRequestMyGameRegistrationTeamDisbandMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(requestMyGameRegistrationTeamDisbandMutationOptions(queryClient, onError))
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

export function useCreateAdminGameRegistrationInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(createAdminGameRegistrationInvitationMutationOptions(queryClient, onError))
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

export function useDisbandConfirmedGameRegistrationTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(disbandConfirmedGameRegistrationTeamMutationOptions(queryClient, onError))
}

export function useAssignGameRegistrationPlayerToTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(assignGameRegistrationPlayerToTeamMutationOptions(queryClient, onError))
}

export function useRemoveGameRegistrationPlayerFromTeamMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(removeGameRegistrationPlayerFromTeamMutationOptions(queryClient, onError))
}

export function useCancelGameRegistrationTeamInvitationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(cancelGameRegistrationTeamInvitationMutationOptions(queryClient, onError))
}

export function useMoveGameRegistrationTeamToSlotMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(moveGameRegistrationTeamToSlotMutationOptions(queryClient, onError))
}

export function useStartGameFromRegistrationMutation(
  onError: GameRegistrationMutationErrorHandler,
) {
  const queryClient = useQueryClient()
  return useMutation(startGameFromRegistrationMutationOptions(queryClient, onError))
}
