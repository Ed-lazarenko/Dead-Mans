import type { ReactNode } from 'react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { panelRootPath } from '../../routes/appRoutes.ts'
import { AuthContext, type AuthContextValue, type AuthUser } from './authContext.ts'
import { fetchAuthMe, postAuthLogout } from './authApi.ts'

function shouldHydrateSessionOnBoot() {
  return window.location.pathname.startsWith(panelRootPath)
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
    const backendOrigin = import.meta.env.VITE_BACKEND_ORIGIN ?? 'http://localhost:5285'
    window.location.href = `${backendOrigin}/auth/twitch/login`
  }, [])

  const logout = useCallback(async () => {
    await postAuthLogout()
    setUser(null)
    setAuthStatus('anonymous')
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
      logout,
    }),
    [authStatus, logout, refreshSession, startTwitchLogin, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}


