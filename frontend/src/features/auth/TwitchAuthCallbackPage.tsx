import { useMemo } from 'react'
import { Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useTwitchAuthCallback } from './useTwitchAuthCallback.ts'

export function TwitchAuthCallbackPage() {
  const { t } = useTranslation()
  const { callbackReason, isSuccess, navigateToLogin, sessionRestoreFailed } = useTwitchAuthCallback()
  const callbackReasonMessage = useMemo(() => {
    return t(`auth.callbackReasons.${callbackReason ?? 'unknown'}`, {
      defaultValue: t('auth.callbackReasons.unknown'),
    })
  }, [callbackReason, t])

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
            <Button variant="contained" onClick={navigateToLogin}>
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
            <Button variant="contained" onClick={navigateToLogin}>
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
