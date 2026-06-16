import { ButtonBase, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import {
  adminModifiersRoute,
  adminQuestionsRoute,
  gameSetupRoute,
  teamRegistrationsRoute,
} from '../routes/app-routes.ts'

const adminRoutes = [
  gameSetupRoute,
  adminModifiersRoute,
  adminQuestionsRoute,
  teamRegistrationsRoute,
]

interface PanelAdminNavigationProps {
  activeRouteId: string | undefined
  layout: 'inline' | 'stacked'
}

/**
 * Admin-facing primary navigation. Rendered in place of PanelPrimaryNavigation
 * when the active route belongs to the 'admin' group. Mirrors the same
 * inline/stacked dual-render pattern as the player nav.
 */
export function PanelAdminNavigation({ activeRouteId, layout }: PanelAdminNavigationProps) {
  const { t } = useTranslation()
  const isStacked = layout === 'stacked'

  return (
    <Stack
      component="nav"
      aria-label={t('navigation.adminNavigation')}
      direction="row"
      spacing={isStacked ? 0 : 0.5}
      sx={
        isStacked
          ? {
              display: { xs: 'grid', sm: 'none' },
              gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
              pb: 1,
            }
          : { display: { xs: 'none', sm: 'flex' } }
      }
    >
      {adminRoutes.map((route) => (
        <AdminNavigationLink
          key={route.id}
          to={route.fullPath}
          label={t(route.labelKey)}
          isActive={activeRouteId === route.id}
          fullWidth={isStacked}
        />
      ))}
    </Stack>
  )
}

interface AdminNavigationLinkProps {
  to: string
  label: string
  isActive: boolean
  fullWidth?: boolean
}

function AdminNavigationLink({ to, label, isActive, fullWidth = false }: AdminNavigationLinkProps) {
  return (
    <ButtonBase
      component={RouterLink}
      to={to}
      aria-current={isActive ? 'page' : undefined}
      sx={(theme) => ({
        position: 'relative',
        width: fullWidth ? '100%' : 'auto',
        minHeight: 42,
        px: { xs: 1, sm: 2 },
        borderRadius: 1,
        color: isActive ? 'warning.light' : 'text.secondary',
        fontFamily: theme.typography.button.fontFamily,
        fontWeight: 700,
        letterSpacing: '0.05em',
        textTransform: 'uppercase',
        '&::after': {
          content: '""',
          position: 'absolute',
          right: 10,
          bottom: 2,
          left: 10,
          height: 2,
          backgroundColor: isActive ? 'warning.main' : 'transparent',
          transition: 'background-color 0.15s ease',
        },
        '&:hover': {
          color: 'text.primary',
          backgroundColor: alpha(theme.palette.warning.main, 0.08),
        },
      })}
    >
      <Typography component="span" variant="button" noWrap>
        {label}
      </Typography>
    </ButtonBase>
  )
}
