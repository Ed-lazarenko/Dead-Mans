import {
  createApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOn404,
} from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameRegistrationApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/registration'
      | '/game/registration/teams'
      | '/game/registration/admin'
      | '/game/registration/teams/{teamId}/join'
      | '/game/registration/teams/leave'
      | '/game/registration/invitations/{invitationId}/accept'
      | '/game/registration/invitations/{invitationId}/decline'
      | '/game/registration/my-team/invitations'
      | '/game/registration/my-team/invitations/{invitationId}/cancel'
      | '/game/registration/teams/{teamId}/confirm'
      | '/game/registration/teams/{teamId}/reject'
      | '/game/registration/admin/teams'
      | '/game/registration/admin/teams/{teamId}/assign'
      | '/game/registration/admin/teams/{teamId}/move'
    >
  >()

export function fetchGameRegistrationSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameRegistrationApiClient.GET('/game/registration'))
}

export function fetchGameRegistrationAdminSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameRegistrationApiClient.GET('/game/registration/admin'))
}

export function createGameRegistrationTeam(recruitmentOpen: boolean) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/teams', {
      body: { recruitmentOpen },
    }),
  )
}

export function createAdminGameRegistrationTeam(input: {
  recruitmentOpen: boolean
  slotId?: string
}) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/admin/teams', {
      body: {
        recruitmentOpen: input.recruitmentOpen,
        ...(input.slotId ? { slotId: input.slotId } : {}),
      },
    }),
  )
}

export function joinGameRegistrationTeam(teamId: string) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/teams/{teamId}/join', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function leaveGameRegistrationTeam() {
  return ensureOpenApiSuccess(gameRegistrationApiClient.POST('/game/registration/teams/leave'))
}

export function acceptGameRegistrationInvitation(invitationId: string) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/invitations/{invitationId}/accept', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function declineGameRegistrationInvitation(invitationId: string) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST('/game/registration/invitations/{invitationId}/decline', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function createPlayerGameRegistrationInvitation(invitedUserId: string) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/my-team/invitations', {
      body: {
        invitedUserId,
      },
    }),
  )
}

export function cancelPlayerGameRegistrationInvitation(invitationId: string) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST('/game/registration/my-team/invitations/{invitationId}/cancel', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function confirmGameRegistrationTeam(teamId: string) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/teams/{teamId}/confirm', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function rejectGameRegistrationTeam(teamId: string) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST('/game/registration/teams/{teamId}/reject', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function assignGameRegistrationPlayerToTeam(input: { teamId: string; userId: string }) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/admin/teams/{teamId}/assign', {
      params: {
        path: { teamId: input.teamId },
      },
      body: {
        userId: input.userId,
      },
    }),
  )
}

export function moveGameRegistrationTeamToSlot(input: { teamId: string; targetSlotId: string }) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/admin/teams/{teamId}/move', {
      params: {
        path: { teamId: input.teamId },
      },
      body: {
        targetSlotId: input.targetSlotId,
      },
    }),
  )
}
