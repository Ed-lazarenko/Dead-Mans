import { createApiClient, unwrapOpenApiData } from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const client =
  createApiClient<
    Pick<
      paths,
      | '/game/modifiers/history'
      | '/game/modifiers/{modifierId}/versions'
      | '/game/modifiers/{modifierId}/versions/{revision}'
      | '/game/modifiers/{modifierId}/versions/{revision}/games'
    >
  >()

export function fetchModifierHistory(
  search: string,
  status: 'active' | 'archived' | 'all',
  cursor?: string,
) {
  return unwrapOpenApiData(
    client.GET('/game/modifiers/history', {
      params: { query: { search: search || undefined, status, cursor, limit: 20 } },
    }),
  )
}

export function fetchModifierVersions(modifierId: string, cursor?: string) {
  return unwrapOpenApiData(
    client.GET('/game/modifiers/{modifierId}/versions', {
      params: { path: { modifierId }, query: { cursor, limit: 20 } },
    }),
  )
}

export function fetchModifierVersion(modifierId: string, revision: number) {
  return unwrapOpenApiData(
    client.GET('/game/modifiers/{modifierId}/versions/{revision}', {
      params: { path: { modifierId, revision } },
    }),
  )
}

export function fetchModifierVersionGames(modifierId: string, revision: number, cursor?: string) {
  return unwrapOpenApiData(
    client.GET('/game/modifiers/{modifierId}/versions/{revision}/games', {
      params: { path: { modifierId, revision }, query: { cursor, limit: 20 } },
    }),
  )
}
