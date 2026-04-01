import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useAuth } from './use-auth.ts'

interface RequireAuthProps {
  children: ReactNode
}

export function RequireAuth({ children }: RequireAuthProps) {
  const { isAuthenticated, authStatus } = useAuth()
  const { t } = useTranslation()

  if (authStatus === 'checking') {
    return (
      <Paper sx={{ p: 4 }}>
        <Stack spacing={2} alignItems="center">
          <CircularProgress size={28} />
          <Typography>{t('auth.checkingSession')}</Typography>
        </Stack>
      </Paper>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
