import { ButtonBase, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import {
  gameApplicationRoute,
  gameBoardRoute,
  gameHistoryRoute,
  gameModifiersRoute,
  gameQuizRoute,
} from '../routes/app-routes.ts'

const primaryRoutes = [
  gameHistoryRoute,
  gameBoardRoute,
  gameApplicationRoute,
  gameModifiersRoute,
  gameQuizRoute,
]

interface PanelPrimaryNavigationProps {
  activeRouteId: string | undefined
  layout: 'inline' | 'stacked'
  showGameApplication: boolean
}

/**
 * Player-facing primary navigation. Rendered twice by the panel shell: an
 * `inline` row for >= sm viewports and a `stacked` two-column grid for xs.
 */
export function PanelPrimaryNavigation({
  activeRouteId,
  layout,
  showGameApplication,
}: PanelPrimaryNavigationProps) {
  const { t } = useTranslation()
  const isStacked = layout === 'stacked'
  const visibleRoutes = showGameApplication
    ? primaryRoutes
    : primaryRoutes.filter((route) => route.id !== gameApplicationRoute.id)

  return (
    <Stack
      component="nav"
      aria-label={t('navigation.primary')}
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
      {visibleRoutes.map((route) => (
        <NavigationLink
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

interface NavigationLinkProps {
  to: string
  label: string
  isActive: boolean
  fullWidth?: boolean
}

function NavigationLink({ to, label, isActive, fullWidth = false }: NavigationLinkProps) {
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
        color: isActive ? 'primary.light' : 'text.secondary',
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
          backgroundColor: isActive ? 'primary.main' : 'transparent',
          transition: 'background-color 0.15s ease',
        },
        '&:hover': {
          color: 'text.primary',
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
        },
      })}
    >
      <Typography component="span" variant="button" noWrap>
        {label}
      </Typography>
    </ButtonBase>
  )
}
