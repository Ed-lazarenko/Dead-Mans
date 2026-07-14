import {
  apiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOn404,
} from '../../../shared/api/client/openApiClient.ts'

export function fetchGameRegistrationSnapshot() {
  return unwrapOpenApiDataOrNullOn404(apiClient.GET('/game/registration'))
}

export function fetchGameRegistrationAdminTeams() {
  return unwrapOpenApiDataOrNullOn404(apiClient.GET('/game/registration/teams'))
}

export function fetchGameRegistrationAdminSnapshot() {
  return unwrapOpenApiDataOrNullOn404(apiClient.GET('/game/registration/admin'))
}

export function createGameRegistrationTeam(recruitmentOpen: boolean) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/teams', {
      body: { recruitmentOpen },
    }),
  )
}

export function createAdminGameRegistrationTeam(input: {
  recruitmentOpen: boolean
  slotId?: string
}) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/admin/teams', {
      body: {
        recruitmentOpen: input.recruitmentOpen,
        ...(input.slotId ? { slotId: input.slotId } : {}),
      },
    }),
  )
}

export function joinGameRegistrationTeam(teamId: string) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/teams/{teamId}/join', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function leaveGameRegistrationTeam() {
  return ensureOpenApiSuccess(apiClient.POST('/game/registration/teams/leave'))
}

export function acceptGameRegistrationInvitation(invitationId: string) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/invitations/{invitationId}/accept', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function declineGameRegistrationInvitation(invitationId: string) {
  return ensureOpenApiSuccess(
    apiClient.POST('/game/registration/invitations/{invitationId}/decline', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function createPlayerGameRegistrationInvitation(invitedUserId: string) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/my-team/invitations', {
      body: {
        invitedUserId,
      },
    }),
  )
}

export function cancelPlayerGameRegistrationInvitation(invitationId: string) {
  return ensureOpenApiSuccess(
    apiClient.POST('/game/registration/my-team/invitations/{invitationId}/cancel', {
      params: {
        path: { invitationId },
      },
    }),
  )
}

export function confirmGameRegistrationTeam(teamId: string) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/teams/{teamId}/confirm', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function rejectGameRegistrationTeam(teamId: string) {
  return ensureOpenApiSuccess(
    apiClient.POST('/game/registration/teams/{teamId}/reject', {
      params: {
        path: { teamId },
      },
    }),
  )
}

export function assignGameRegistrationPlayerToTeam(input: { teamId: string; userId: string }) {
  return unwrapOpenApiData(
    apiClient.POST('/game/registration/admin/teams/{teamId}/assign', {
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
    apiClient.POST('/game/registration/admin/teams/{teamId}/move', {
      params: {
        path: { teamId: input.teamId },
      },
      body: {
        targetSlotId: input.targetSlotId,
      },
    }),
  )
}
