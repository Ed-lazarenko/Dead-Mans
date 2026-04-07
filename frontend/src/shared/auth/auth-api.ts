import type { AuthUser } from './auth-context.ts'
import type { AuthSession } from '../api/contracts/index.ts'
import { getBackendOrigin } from '../api/config.ts'
import { httpClient } from '../api/client/httpClient.ts'

export async function fetchAuthMe(): Promise<AuthUser> {
  const data = await httpClient.get<AuthSession>('/auth/me', {
    baseUrl: getBackendOrigin(),
    cache: 'no-store',
    credentials: 'include',
  })
  return {
    id: data.userId,
    displayName: data.displayName,
    roles: data.roles,
  }
}
