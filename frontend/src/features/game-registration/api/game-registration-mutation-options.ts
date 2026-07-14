import { mutationOptions, type QueryClient } from '@tanstack/react-query'
import {
  acceptGameRegistrationInvitation,
  assignGameRegistrationPlayerToTeam,
  cancelPlayerGameRegistrationInvitation,
  confirmGameRegistrationTeam,
  createPlayerGameRegistrationInvitation,
  createAdminGameRegistrationTeam,
  createGameRegistrationTeam,
  declineGameRegistrationInvitation,
  joinGameRegistrationTeam,
  leaveGameRegistrationTeam,
  moveGameRegistrationTeamToSlot,
  rejectGameRegistrationTeam,
} from './game-registration-api.ts'
import { gameRegistrationQueryKeys } from './game-registration-queries.ts'

export type GameRegistrationMutationErrorHandler = (error: Error) => void

function registrationMutationHandlers(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return {
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: gameRegistrationQueryKeys.all,
      }),
    onError: (error: Error) => onError(error),
  }
}

export function createGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: createGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function joinGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: joinGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function createAdminGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: createAdminGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function leaveGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: leaveGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function acceptGameRegistrationInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: acceptGameRegistrationInvitation,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function declineGameRegistrationInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: declineGameRegistrationInvitation,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function createPlayerGameRegistrationInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: createPlayerGameRegistrationInvitation,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function cancelPlayerGameRegistrationInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: cancelPlayerGameRegistrationInvitation,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function confirmGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: confirmGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function rejectGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: rejectGameRegistrationTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function assignGameRegistrationPlayerToTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: assignGameRegistrationPlayerToTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function moveGameRegistrationTeamToSlotMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: moveGameRegistrationTeamToSlot,
    ...registrationMutationHandlers(queryClient, onError),
  })
}
