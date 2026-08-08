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
      | '/game/registration/my-team/name'
      | '/game/registration/admin'
      | '/game/registration/teams/{teamId}/join'
      | '/game/registration/teams/leave'
      | '/game/registration/my-team/disband-request'
      | '/game/registration/invitations/{invitationId}/accept'
      | '/game/registration/invitations/{invitationId}/decline'
      | '/game/registration/my-team/invitations'
      | '/game/registration/my-team/invitations/{invitationId}/cancel'
      | '/game/registration/teams/{teamId}/confirm'
      | '/game/registration/teams/{teamId}/reject'
      | '/game/registration/teams/{teamId}/disband'
      | '/game/registration/invitations'
      | '/game/registration/admin/teams'
      | '/game/registration/admin/teams/{teamId}/name'
      | '/game/registration/admin/teams/{teamId}/assign'
      | '/game/registration/admin/teams/{teamId}/members/{userId}/remove'
      | '/game/registration/admin/teams/{teamId}/invitations/{invitationId}/cancel'
      | '/game/registration/admin/teams/{teamId}/move'
      | '/game/lifecycle/start'
    >
  >()

export function fetchGameRegistrationSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameRegistrationApiClient.GET('/game/registration'))
}

export function fetchGameRegistrationAdminSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameRegistrationApiClient.GET('/game/registration/admin'))
}

export function createGameRegistrationTeam(input: { recruitmentOpen: boolean; name?: string }) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/teams', {
      body: {
        recruitmentOpen: input.recruitmentOpen,
        ...(input.name ? { name: input.name } : {}),
      },
    }),
  )
}

export function createAdminGameRegistrationTeam(input: {
  recruitmentOpen: boolean
  teamSlotId?: string
  name?: string
}) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/admin/teams', {
      body: {
        recruitmentOpen: input.recruitmentOpen,
        ...(input.teamSlotId ? { teamSlotId: input.teamSlotId } : {}),
        ...(input.name ? { name: input.name } : {}),
      },
    }),
  )
}

export function updateMyGameRegistrationTeamName(name?: string) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.PATCH('/game/registration/my-team/name', {
      body: {
        name: name || null,
      },
    }),
  )
}

export function updateAdminGameRegistrationTeamName(input: { teamId: string; name?: string }) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.PATCH('/game/registration/admin/teams/{teamId}/name', {
      params: {
        path: { teamId: input.teamId },
      },
      body: {
        name: input.name || null,
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

export function requestMyGameRegistrationTeamDisband() {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/my-team/disband-request'),
  )
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

export function createAdminGameRegistrationInvitation(input: {
  teamSlotId: string
  invitedUserId: string
  teamId?: string
}) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/invitations', {
      body: {
        teamSlotId: input.teamSlotId,
        invitedUserId: input.invitedUserId,
        ...(input.teamId ? { teamId: input.teamId } : {}),
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

export function disbandConfirmedGameRegistrationTeam(teamId: string) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST('/game/registration/teams/{teamId}/disband', {
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

export function removeGameRegistrationPlayerFromTeam(input: { teamId: string; userId: string }) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST(
      '/game/registration/admin/teams/{teamId}/members/{userId}/remove',
      {
        params: {
          path: {
            teamId: input.teamId,
            userId: input.userId,
          },
        },
      },
    ),
  )
}

export function cancelGameRegistrationTeamInvitation(input: {
  teamId: string
  invitationId: string
}) {
  return ensureOpenApiSuccess(
    gameRegistrationApiClient.POST(
      '/game/registration/admin/teams/{teamId}/invitations/{invitationId}/cancel',
      {
        params: {
          path: {
            teamId: input.teamId,
            invitationId: input.invitationId,
          },
        },
      },
    ),
  )
}

export function moveGameRegistrationTeamToSlot(input: {
  teamId: string
  targetTeamSlotId: string
}) {
  return unwrapOpenApiData(
    gameRegistrationApiClient.POST('/game/registration/admin/teams/{teamId}/move', {
      params: {
        path: { teamId: input.teamId },
      },
      body: {
        targetTeamSlotId: input.targetTeamSlotId,
      },
    }),
  )
}

export function startGameFromRegistration() {
  return unwrapOpenApiData(gameRegistrationApiClient.POST('/game/lifecycle/start'))
}
