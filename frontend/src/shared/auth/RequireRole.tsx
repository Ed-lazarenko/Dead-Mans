import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { Paper, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { UserRole } from './authContext.ts'
import { useAuth } from './useAuth.ts'

interface RequireRoleProps {
  allowedRoles: UserRole[]
  children: ReactNode
}

export function RequireRole({ allowedRoles, children }: RequireRoleProps) {
  const { user, isAuthenticated } = useAuth()
  const role: UserRole = user?.role ?? 'guest'
  const { t } = useTranslation()

  if (!isAuthenticated) {
    return <Navigate to="/" replace />
  }

  if (!allowedRoles.includes(role)) {
    return (
      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          {t('authErrors.noPermissionTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('authErrors.noPermissionDescription')}
        </Typography>
      </Paper>
    )
  }

  return <>{children}</>
}


