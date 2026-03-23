import { createContext } from 'react'

export type UserRole = 'admin' | 'moderator' | 'viewer' | 'guest'
export type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

export interface AuthUser {
  id: string
  displayName: string
  role: UserRole
}

export interface AuthContextValue {
  user: AuthUser | null
  authStatus: AuthStatus
  isAuthenticated: boolean
  startTwitchLogin: () => void
  refreshSession: () => Promise<boolean>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
