import type { ParseKeys } from 'i18next'
import { ButtonBase, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import {
  adminModifiersRoute,
  adminQuestionsRoute,
  catalogModifiersRoute,
  catalogQuestionsRoute,
  gameSetupRoute,
  teamRegistrationsRoute,
  type PanelRouteDefinition,
} from '../routes/app-routes.ts'

const adminSections: ReadonlyArray<{
  labelKey: Extract<ParseKeys, `navigation.sections.${string}`>
  routes: readonly PanelRouteDefinition[]
}> = [
  {
    labelKey: 'navigation.sections.currentGame',
    routes: [gameSetupRoute, adminModifiersRoute, adminQuestionsRoute, teamRegistrationsRoute],
  },
  {
    labelKey: 'navigation.sections.catalog',
    routes: [catalogModifiersRoute, catalogQuestionsRoute],
  },
]

interface PanelAdminNavigationProps {
  activeRouteId: string | undefined
  layout: 'inline' | 'stacked'
}

/**
 * Admin-facing primary navigation. Rendered in place of PanelPrimaryNavigation
 * when the active route belongs to the 'admin' group. Admin destinations are
 * split into two labelled sections: the current game's configuration and the
 * global catalog used by any game.
 */
export function PanelAdminNavigation({ activeRouteId, layout }: PanelAdminNavigationProps) {
  const { t } = useTranslation()
  const isStacked = layout === 'stacked'

  return (
    <Stack
      component="nav"
      aria-label={t('navigation.adminNavigation')}
      direction={isStacked ? 'column' : 'row'}
      spacing={isStacked ? 1.5 : 2}
      sx={
        isStacked
          ? { display: { xs: 'flex', sm: 'none' }, pb: 1 }
          : { display: { xs: 'none', sm: 'flex' }, alignItems: 'center' }
      }
    >
      {adminSections.map((section) => (
        <Stack
          key={section.labelKey}
          direction={isStacked ? 'column' : 'row'}
          spacing={isStacked ? 0.25 : 0.5}
          sx={isStacked ? {} : { alignItems: 'center' }}
        >
          <Typography
            component="span"
            variant="overline"
            sx={{
              color: 'text.disabled',
              letterSpacing: '0.08em',
              px: { xs: 0, sm: 1 },
              whiteSpace: 'nowrap',
            }}
          >
            {t(section.labelKey)}
          </Typography>
          <Stack
            direction="row"
            spacing={isStacked ? 0 : 0.5}
            sx={
              isStacked ? { display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' } : {}
            }
          >
            {section.routes.map((route) => (
              <AdminNavigationLink
                key={route.id}
                to={route.fullPath}
                label={t(route.labelKey)}
                isActive={activeRouteId === route.id}
                fullWidth={isStacked}
              />
            ))}
          </Stack>
        </Stack>
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
