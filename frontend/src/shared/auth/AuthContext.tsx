import type { ReactNode } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { panelRootPath } from '../../routes/app-routes.ts'
import { getBackendOrigin } from '../api/config.ts'
import { AuthContext, type AuthContextValue, type AuthUser } from './auth-context.ts'
import { fetchAuthMe } from './auth-api.ts'

function shouldHydrateSessionOnBoot() {
  const { pathname } = window.location
  return pathname === '/' || pathname.startsWith(panelRootPath)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [authStatus, setAuthStatus] = useState<'checking' | 'authenticated' | 'anonymous'>(
    shouldHydrateSessionOnBoot() ? 'checking' : 'anonymous',
  )

  const refreshSession = useCallback(async () => {
    try {
      const payload = await fetchAuthMe()
      setUser(payload)
      setAuthStatus('authenticated')
      return true
    } catch {
      setUser(null)
      setAuthStatus('anonymous')
      return false
    }
  }, [])

  const startTwitchLogin = useCallback(() => {
    window.location.href = `${getBackendOrigin()}/auth/twitch/login`
  }, [])

  useEffect(() => {
    if (!shouldHydrateSessionOnBoot()) return

    let isMounted = true

    void (async () => {
      try {
        const payload = await fetchAuthMe()
        if (!isMounted) return

        setUser(payload)
        setAuthStatus('authenticated')
      } catch {
        if (!isMounted) return

        setUser(null)
        setAuthStatus('anonymous')
      }
    })()

    return () => {
      isMounted = false
    }
  }, [])

  const value: AuthContextValue = useMemo(
    () => ({
      user,
      authStatus,
      isAuthenticated: user != null,
      startTwitchLogin,
      refreshSession,
    }),
    [authStatus, refreshSession, startTwitchLogin, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}


