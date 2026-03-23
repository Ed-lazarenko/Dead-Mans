import type { ReactNode } from 'react'
import { useState } from 'react'
import { AuthContext, type AuthContextValue, type AuthUser } from './authContext.ts'

// TODO: позже заменить на реальную авторизацию через Twitch OAuth2 и backend.
const mockUser: AuthUser = {
  id: 'demo-streamer',
  displayName: 'DemoStreamer',
  role: 'streamer',
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)

  const value: AuthContextValue = {
    user,
    isAuthenticated: user != null,
    loginAsDemoStreamer: () => setUser(mockUser),
    logout: () => setUser(null),
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}


