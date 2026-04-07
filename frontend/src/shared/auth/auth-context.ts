import { createContext } from 'react'
import type { AuthRole } from '../api/contracts/index.ts'

export type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

export interface AuthUser {
  id: string
  displayName: string
  roles: AuthRole[]
}

export interface AuthContextValue {
  user: AuthUser | null
  authStatus: AuthStatus
  isAuthenticated: boolean
  startTwitchLogin: () => void
  refreshSession: () => Promise<boolean>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
