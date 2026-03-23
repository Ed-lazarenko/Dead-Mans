import { createContext } from 'react'

export type UserRole = 'streamer' | 'moderator' | 'viewer' | 'guest'

export interface AuthUser {
  id: string
  displayName: string
  role: UserRole
}

export interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  loginAsDemoStreamer: () => void
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
