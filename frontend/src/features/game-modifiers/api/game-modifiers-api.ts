import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type {
  AdminActivateGameModifierRequest,
  GameModifierActivation,
  GameModifierAdminPlayer,
} from '../../../shared/api/contracts/index.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameModifiersApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game/modifiers/catalog'
      | '/game/modifiers/state'
      | '/game/modifiers/{modifierId}/activate'
      | '/game/modifiers/admin/players'
      | '/game/modifiers/admin/state/{userId}'
      | '/game/modifiers/admin/activations'
      | '/game/modifiers/admin/activate'
      | '/game/modifiers/admin/activations/{activationId}'
    >
  >()

export function fetchGameModifierCatalog() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/catalog'))
}

export function fetchGameModifierState() {
  return unwrapOpenApiData(gameModifiersApiClient.GET('/game/modifiers/state'))
}

export function activateGameModifier(modifierId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.POST('/game/modifiers/{modifierId}/activate', {
      params: { path: { modifierId } },
    }),
  )
}

export function fetchAdminGameModifierPlayers(): Promise<GameModifierAdminPlayer[]> {
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

export function cancelGameModifierActivation(activationId: string) {
  return unwrapOpenApiData(
    gameModifiersApiClient.DELETE('/game/modifiers/admin/activations/{activationId}', {
      params: { path: { activationId } },
    }),
  )
}
