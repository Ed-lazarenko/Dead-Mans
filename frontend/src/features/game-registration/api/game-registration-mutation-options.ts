import { mutationOptions, type QueryClient } from '@tanstack/react-query'
import { currentGameTeamQueueQueryOptions } from '../../game-board/index.ts'
import {
  acceptGameRegistrationInvitation,
  assignGameRegistrationPlayerToTeam,
  cancelGameRegistrationTeamInvitation,
  cancelPlayerGameRegistrationInvitation,
  confirmGameRegistrationTeam,
  createAdminGameRegistrationInvitation,
  createPlayerGameRegistrationInvitation,
  createAdminGameRegistrationTeam,
  createGameRegistrationTeam,
  declineGameRegistrationInvitation,
  disbandConfirmedGameRegistrationTeam,
  joinGameRegistrationTeam,
  leaveGameRegistrationTeam,
  moveGameRegistrationTeamToSlot,
  requestMyGameRegistrationTeamDisband,
  rejectGameRegistrationTeam,
  removeGameRegistrationPlayerFromTeam,
  startGameFromRegistration,
  updateAdminGameRegistrationTeamName,
  updateMyGameRegistrationTeamName,
} from './game-registration-api.ts'
import { gameRegistrationQueryKeys } from './game-registration-queries.ts'

export type GameRegistrationMutationErrorHandler = (error: Error) => void

function registrationMutationHandlers(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return {
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: gameRegistrationQueryKeys.all,
        }),
        queryClient.invalidateQueries({
          queryKey: currentGameTeamQueueQueryOptions.queryKey,
        }),
      ])
    },
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

export function updateMyGameRegistrationTeamNameMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: updateMyGameRegistrationTeamName,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function updateAdminGameRegistrationTeamNameMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: updateAdminGameRegistrationTeamName,
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

export function requestMyGameRegistrationTeamDisbandMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: requestMyGameRegistrationTeamDisband,
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

export function createAdminGameRegistrationInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: createAdminGameRegistrationInvitation,
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

export function disbandConfirmedGameRegistrationTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: disbandConfirmedGameRegistrationTeam,
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

export function removeGameRegistrationPlayerFromTeamMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: removeGameRegistrationPlayerFromTeam,
    ...registrationMutationHandlers(queryClient, onError),
  })
}

export function cancelGameRegistrationTeamInvitationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: cancelGameRegistrationTeamInvitation,
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

export function startGameFromRegistrationMutationOptions(
  queryClient: QueryClient,
  onError: GameRegistrationMutationErrorHandler,
) {
  return mutationOptions({
    mutationFn: startGameFromRegistration,
    ...registrationMutationHandlers(queryClient, onError),
  })
}
