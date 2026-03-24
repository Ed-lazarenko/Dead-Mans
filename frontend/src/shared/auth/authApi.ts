import type { AuthUser, UserRole } from './authContext.ts'
import type { AuthRole, AuthSession } from '../api/contracts/index.ts'
import { getBackendOrigin } from '../api/config.ts'
import { httpClient } from '../api/client/httpClient.ts'

function mapRole(roles: AuthRole[]): UserRole {
  if (roles.includes('admin')) return 'admin'
  if (roles.includes('moderator')) return 'moderator'
  if (roles.includes('viewer')) return 'viewer'
  throw new Error('Authenticated user has no supported effective role.')
}

export async function fetchAuthMe(): Promise<AuthUser> {
  const data = await httpClient.get<AuthSession>('/auth/me', {
    baseUrl: getBackendOrigin(),
    cache: 'no-store',
    credentials: 'include',
  })
  return {
    id: data.userId,
    displayName: data.displayName,
    role: mapRole(data.roles),
  }
}

export async function postAuthLogout(): Promise<void> {
  await httpClient.post('/auth/logout', undefined, {
    baseUrl: getBackendOrigin(),
    cache: 'no-store',
    credentials: 'include',
  })
}
