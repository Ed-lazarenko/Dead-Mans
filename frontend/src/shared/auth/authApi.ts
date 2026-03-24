import type { AuthUser, UserRole } from './authContext.ts'
import type { AuthRole, AuthSession } from '../api/contracts/index.ts'

function getBackendOrigin() {
  return import.meta.env.VITE_BACKEND_ORIGIN ?? 'http://localhost:5285'
}

function mapRole(roles: AuthRole[]): UserRole {
  if (roles.includes('admin')) return 'admin'
  if (roles.includes('moderator')) return 'moderator'
  if (roles.includes('viewer')) return 'viewer'
  throw new Error('Authenticated user has no supported effective role.')
}

export async function fetchAuthMe(): Promise<AuthUser> {
  const response = await fetch(`${getBackendOrigin()}/auth/me`, {
    cache: 'no-store',
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error(`auth/me failed with status ${response.status}`)
  }

  const data = (await response.json()) as AuthSession
  return {
    id: data.userId,
    displayName: data.displayName,
    role: mapRole(data.roles),
  }
}

export async function postAuthLogout(): Promise<void> {
  const response = await fetch(`${getBackendOrigin()}/auth/logout`, {
    method: 'POST',
    cache: 'no-store',
    credentials: 'include',
  })

  if (!response.ok && response.status !== 204) {
    throw new Error(`auth/logout failed with status ${response.status}`)
  }
}
