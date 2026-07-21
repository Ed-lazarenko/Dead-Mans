import {
  createApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
} from '../../../shared/api/client/openApiClient.ts'
import type { GameUserNotification } from '../../../shared/api/contracts/index.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameNotificationsApiClient =
  createApiClient<Pick<paths, '/game/notifications' | '/game/notifications/read'>>()

export function fetchGameNotifications(): Promise<GameUserNotification[]> {
  return unwrapOpenApiData(gameNotificationsApiClient.GET('/game/notifications'))
}

export function markGameNotificationsRead() {
  return ensureOpenApiSuccess(gameNotificationsApiClient.POST('/game/notifications/read'))
}
