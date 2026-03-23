import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../../shared/auth/useAuth.ts'
import { defaultRoute } from '../../routes/appRoutes.ts'

export function TwitchAuthCallbackPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const { refreshSession } = useAuth()
  const [sessionRestoreFailed, setSessionRestoreFailed] = useState(false)

  const callbackStatus = useMemo(() => {
    const query = new URLSearchParams(location.search)
    return query.get('status')
  }, [location.search])
  const callbackReason = useMemo(() => {
    const query = new URLSearchParams(location.search)
    return query.get('reason')
  }, [location.search])
  const isSuccess = callbackStatus === 'authenticated'
  const callbackReasonMessage = useMemo(() => {
    return t(`auth.callbackReasons.${callbackReason ?? 'unknown'}`, {
      defaultValue: t('auth.callbackReasons.unknown'),
    })
  }, [callbackReason, t])

  useEffect(() => {
    if (!isSuccess) return

    let isMounted = true

    void (async () => {
      const isSessionReady = await refreshSession()

      if (!isMounted) return

      if (isSessionReady) {
        navigate(defaultRoute.fullPath, { replace: true })
        return
      }

      setSessionRestoreFailed(true)
    })()

    return () => {
      isMounted = false
    }
  }, [refreshSession, navigate, isSuccess])

  if (!isSuccess) {
    return (
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Paper sx={{ p: 4, minWidth: 320 }}>
          <Stack spacing={2}>
            <Typography variant="h6">{t('auth.callbackFailedTitle')}</Typography>
            <Typography color="text.secondary">{callbackReasonMessage}</Typography>
            <Button variant="contained" onClick={() => navigate('/', { replace: true })}>
              {t('auth.backToLogin')}
            </Button>
          </Stack>
        </Paper>
      </Box>
    )
  }

  if (sessionRestoreFailed) {
    return (
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Paper sx={{ p: 4, minWidth: 320 }}>
          <Stack spacing={2}>
            <Typography variant="h6">{t('auth.callbackFailedTitle')}</Typography>
            <Typography color="text.secondary">{t('auth.sessionRestoreFailed')}</Typography>
            <Button variant="contained" onClick={() => navigate('/', { replace: true })}>
              {t('auth.backToLogin')}
            </Button>
          </Stack>
        </Paper>
      </Box>
    )
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Paper sx={{ p: 4, minWidth: 320, textAlign: 'center' }}>
        <CircularProgress size={26} />
        <Typography sx={{ mt: 2 }}>{t('auth.processing')}</Typography>
      </Paper>
    </Box>
  )
}
