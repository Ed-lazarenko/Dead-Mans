import { createContext } from 'react'

export type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

export interface AuthUser {
  id: string
  displayName: string
}

export interface AuthContextValue {
  user: AuthUser | null
  authStatus: AuthStatus
  isAuthenticated: boolean
  startTwitchLogin: () => void
  refreshSession: () => Promise<boolean>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
