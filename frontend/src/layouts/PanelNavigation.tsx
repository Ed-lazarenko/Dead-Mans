import { Box, Container, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { gameBoardRoute, getPanelRouteByPath } from '../routes/app-routes.ts'
import { useAuth } from '../shared/auth/use-auth.ts'
import { huntBrassTitleSx } from '../shared/theme/surface-sx.ts'
import { PanelAdminNavigation } from './PanelAdminNavigation.tsx'
import { PanelPrimaryNavigation } from './PanelPrimaryNavigation.tsx'
import { PanelProfileMenu } from './PanelProfileMenu.tsx'

export function PanelNavigation() {
  const { t } = useTranslation()
  const location = useLocation()
  const { user, logout } = useAuth()
  const activeRoute = getPanelRouteByPath(location.pathname)
  const isAdminRoute = activeRoute?.group === 'admin'

  if (!user) {
    return null
  }

  return (
    <Box
      component="header"
      sx={(theme) => ({
        position: 'sticky',
        top: 0,
        zIndex: theme.zIndex.appBar,
        borderBottom: `1px solid ${alpha(theme.palette.primary.main, 0.28)}`,
        backgroundImage: theme.custom.gradients.panelAccentSoft,
        boxShadow: `0 10px 28px ${alpha(theme.palette.common.black, 0.35)}`,
        backdropFilter: 'blur(12px)',
      })}
    >
      <Container maxWidth="xl">
        <Stack
          direction="row"
          alignItems="center"
          justifyContent="space-between"
          spacing={2}
          sx={{ minHeight: 64 }}
        >
          <Typography
            component={RouterLink}
            to={gameBoardRoute.fullPath}
            variant="h6"
            sx={{
              ...huntBrassTitleSx,
              color: 'primary.main',
              textDecoration: 'none',
              whiteSpace: 'nowrap',
            }}
          >
            {t('appTitle')}
          </Typography>

          {isAdminRoute ? (
            <PanelAdminNavigation activeRouteId={activeRoute?.id} layout="inline" />
          ) : (
            <PanelPrimaryNavigation activeRouteId={activeRoute?.id} layout="inline" />
          )}

          <PanelProfileMenu user={user} activeRouteId={activeRoute?.id} onLogout={logout} />
        </Stack>

        {isAdminRoute ? (
          <PanelAdminNavigation activeRouteId={activeRoute?.id} layout="stacked" />
        ) : (
          <PanelPrimaryNavigation activeRouteId={activeRoute?.id} layout="stacked" />
        )}
      </Container>
    </Box>
  )
}
