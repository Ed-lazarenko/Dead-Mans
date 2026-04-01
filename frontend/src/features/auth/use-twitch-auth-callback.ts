import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { defaultRoute } from '../../routes/app-routes.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'

export function useTwitchAuthCallback() {
  const location = useLocation()
  const navigate = useNavigate()
  const { refreshSession } = useAuth()
  const [sessionRestoreFailed, setSessionRestoreFailed] = useState(false)

  const callbackParams = useMemo(() => new URLSearchParams(location.search), [location.search])
  const callbackStatus = callbackParams.get('status')
  const callbackReason = callbackParams.get('reason')
  const isSuccess = callbackStatus === 'authenticated'

  useEffect(() => {
    if (!isSuccess) return

    let isCancelled = false

    void (async () => {
      const isSessionReady = await refreshSession()
      if (isCancelled) return

      if (isSessionReady) {
        navigate(defaultRoute.fullPath, { replace: true })
        return
      }

      setSessionRestoreFailed(true)
    })()

    return () => {
      isCancelled = true
    }
  }, [isSuccess, navigate, refreshSession])

  return {
    callbackReason,
    isSuccess,
    sessionRestoreFailed,
    navigateToLogin: () => navigate('/', { replace: true }),
  }
}
