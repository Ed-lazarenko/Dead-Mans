import {
  createApiClient,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../../../shared/api/client/openApiClient.ts'
import type {
  AdminActivateGameModifierRequest,
  CancelGameModifierActivationRequest,
  EmergencyDisableGameModifierRequest,
  GameModifierActivation,
  GameModifierAdminPlayersResult,
} from '../../../shared/api/contracts/index.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameModifiersApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/modifiers/catalog'
      | '/game/modifiers/state'
      | '/game/modifiers/{modifierId}/activate'
      | '/game/modifiers/{modifierId}/emergency-disable'
      | '/game/modifiers/activations/{activationId}/self-cancel'
      | '/game/modifiers/admin/players'
      | '/game/modifiers/admin/state/{userId}'
      | '/game/modifiers/admin/activations'
      | '/game/modifiers/admin/activate'
      | '/game/modifiers/admin/activations/{activationId}/cancel'
    >
  >()

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/catalog'))
}

export function fetchGameModifierState() {
  return unwrapOpenApiDataOrNullOnNoContent(gameModifiersApiClient.GET('/game/modifiers/state'))
}

export function activateGameModifier(modifierId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/{modifierId}/activate', {
      params: { path: { modifierId } },
    }),
  )
}

export function emergencyDisableGameModifier(modifierId: string, reason: string) {
  const body: EmergencyDisableGameModifierRequest = { reason }

  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/{modifierId}/emergency-disable', {
      params: { path: { modifierId } },
      body,
    }),
  )
}

export function selfCancelGameModifierActivation(
  activationId: string,
  expectedRoundVersion: number,
) {
  const body: CancelGameModifierActivationRequest = { expectedRoundVersion }

  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/activations/{activationId}/self-cancel', {
      params: { path: { activationId } },
      body,
    }),
  )
}

export function fetchAdminGameModifierPlayers(): Promise<GameModifierAdminPlayersResult> {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/admin/players'))
}

export function fetchAdminGameModifierState(userId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.GET('/game/modifiers/admin/state/{userId}', {
      params: { path: { userId } },
    }),
  )
}

export function fetchAdminActiveGameModifierActivations(): Promise<GameModifierActivation[]> {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/admin/activations'))
}

export function adminActivateGameModifier(modifierId: string, targetUserId: string) {
  const body: AdminActivateGameModifierRequest = { modifierId, targetUserId }

  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/admin/activate', {
      body,
    }),
  )
}

export function cancelGameModifierActivation(
  activationId: string,
  expectedRoundVersion: number,
  reason: string,
) {
  const body: CancelGameModifierActivationRequest = { expectedRoundVersion, reason }

  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/admin/activations/{activationId}/cancel', {
      params: { path: { activationId } },
      body,
    }),
  )
}
