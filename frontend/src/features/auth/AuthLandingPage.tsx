import { Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { defaultRoute } from '../../routes/app-routes.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { LanguageSwitcher } from '../../shared/i18n/LanguageSwitcher.tsx'

export function AuthLandingPage() {
  const { t } = useTranslation()
  const { authStatus, isAuthenticated, startTwitchLogin } = useAuth()

  if (authStatus === 'checking') {
    return (
      <Box
        sx={{
          position: 'fixed',
          inset: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Stack spacing={2} alignItems="center">
          <CircularProgress size={28} />
          <Typography>{t('auth.checkingSession')}</Typography>
        </Stack>
      </Box>
    )
  }

  if (isAuthenticated) {
    return <Navigate to={defaultRoute.fullPath} replace />
  }

  return (
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          top: 16,
          right: 16,
        }}
      >
        <LanguageSwitcher />
      </Box>
      <Paper
        elevation={6}
        sx={{
          px: 6,
          py: 5,
          maxWidth: 480,
          textAlign: 'center',
          background:
            'radial-gradient(circle at top, rgba(144,202,249,0.12) 0, transparent 60%), #141829',
        }}
      >
        <Stack spacing={3}>
          <Box>
            <Typography
              variant="h3"
              sx={{
                fontWeight: 700,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
              }}
            >
              {t('auth.title')}
            </Typography>
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{ mt: 1 }}
            >
              {t('auth.subtitle')}
            </Typography>
          </Box>

          <Box>
            <Typography variant="body1" sx={{ mb: 2 }}>
              {t('auth.description')}
            </Typography>
            <Button
              variant="contained"
              size="large"
              onClick={startTwitchLogin}
              sx={{
                mt: 1,
                px: 4,
                py: 1.2,
                fontWeight: 600,
                backgroundColor: '#6441A5',
                '&:hover': {
                  backgroundColor: '#7c4fd9',
                },
              }}
            >
              {t('auth.button')}
            </Button>
          </Box>
        </Stack>
      </Paper>
    </Box>
  )
}


