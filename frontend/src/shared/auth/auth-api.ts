import type { AuthUser } from './auth-context.ts'
import {
  backendApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiDataOrNullOnNoContent,
} from '../api/client/openApiClient.ts'
import { parseApiResponse } from '../api/parse-api-response.ts'
import { authSessionSchema } from './auth-session-schema.ts'

export async function fetchAuthMe(): Promise<AuthUser | null> {
  const payload = await unwrapOpenApiDataOrNullOnNoContent(
    backendApiClient.GET('/auth/me', {
      cache: 'no-store',
    }),
  )
  if (payload == null) {
    return null
  }

  const data = parseApiResponse(authSessionSchema, payload, 'AuthSession')

  return {
    id: data.userId,
    displayName: data.displayName,
    roles: data.roles,
  }
}

export async function logoutAuthSession(): Promise<void> {
  await ensureOpenApiSuccess(backendApiClient.POST('/auth/logout'))
}
