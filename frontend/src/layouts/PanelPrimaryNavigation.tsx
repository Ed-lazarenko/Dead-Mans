import { useId, useState, type MouseEvent } from 'react'
import type { ParseKeys } from 'i18next'
import { ButtonBase, Menu, MenuItem, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import {
  gameApplicationRoute,
  gameBoardRoute,
  gameHistoryRoute,
  gameLeaderboardRoute,
  gameModifiersRoute,
  gameQuizRoute,
  type PanelRouteDefinition,
} from '../routes/app-routes.ts'

const primaryMenus: ReadonlyArray<{
  id: 'current-game' | 'history'
  labelKey: Extract<ParseKeys, `navigation.menus.${string}`>
  routes: readonly PanelRouteDefinition[]
}> = [
  {
    id: 'current-game',
    labelKey: 'navigation.menus.currentGame',
    routes: [
      gameBoardRoute,
      gameLeaderboardRoute,
      gameApplicationRoute,
      gameModifiersRoute,
      gameQuizRoute,
    ],
  },
  {
    id: 'history',
    labelKey: 'navigation.menus.history',
    routes: [gameHistoryRoute],
  },
]

interface PanelPrimaryNavigationProps {
  activeRouteId: string | undefined
  layout: 'inline' | 'stacked'
  showGameApplication: boolean
}

export function PanelPrimaryNavigation({
  activeRouteId,
  layout,
  showGameApplication,
}: PanelPrimaryNavigationProps) {
  const { t } = useTranslation()
  const isStacked = layout === 'stacked'
  const navigationId = useId()
  const [openMenuId, setOpenMenuId] = useState<(typeof primaryMenus)[number]['id'] | null>(null)
  const [anchorByMenuId, setAnchorByMenuId] = useState<
    Partial<Record<(typeof primaryMenus)[number]['id'], HTMLElement | null>>
  >({})

  const handleMenuOpen =
    (menuId: (typeof primaryMenus)[number]['id']) => (event: MouseEvent<HTMLElement>) => {
      setAnchorByMenuId((current) => ({
        ...current,
        [menuId]: event.currentTarget,
      }))
      setOpenMenuId(menuId)
    }

  const handleMenuClose = () => {
    setOpenMenuId(null)
  }

  const visibleMenus = primaryMenus.map((menu) => ({
    ...menu,
    routes: showGameApplication
      ? menu.routes
      : menu.routes.filter((route) => route.id !== gameApplicationRoute.id),
  }))

  return (
    <Stack
      component="nav"
      aria-label={t('navigation.primary')}
      direction={isStacked ? 'column' : 'row'}
      spacing={isStacked ? 1.5 : 2}
      sx={
        isStacked
          ? { display: { xs: 'flex', sm: 'none' }, pb: 1 }
          : { display: { xs: 'none', sm: 'flex' }, alignItems: 'center' }
      }
    >
      {visibleMenus.map((menu) => (
        <PrimaryNavigationMenu
          key={menu.id}
          triggerId={`${navigationId}-${menu.id}`}
          label={t(menu.labelKey)}
          routes={menu.routes}
          activeRouteId={activeRouteId}
          anchorEl={anchorByMenuId[menu.id] ?? null}
          isOpen={openMenuId === menu.id}
          onOpen={handleMenuOpen(menu.id)}
          onClose={handleMenuClose}
          fullWidth={isStacked}
        />
      ))}
    </Stack>
  )
}

interface PrimaryNavigationMenuProps {
  triggerId: string
  label: string
  routes: readonly PanelRouteDefinition[]
  activeRouteId: string | undefined
  anchorEl: HTMLElement | null
  isOpen: boolean
  onOpen: (event: MouseEvent<HTMLElement>) => void
  onClose: () => void
  fullWidth?: boolean
}

function PrimaryNavigationMenu({
  triggerId,
  label,
  routes,
  activeRouteId,
  anchorEl,
  isOpen,
  onOpen,
  onClose,
  fullWidth = false,
}: PrimaryNavigationMenuProps) {
  const { t } = useTranslation()
  const isActive = routes.some((route) => route.id === activeRouteId)

  return (
    <>
      <ButtonBase
        id={triggerId}
        aria-controls={isOpen ? `${triggerId}-menu` : undefined}
        aria-expanded={isOpen ? 'true' : undefined}
        aria-haspopup="menu"
        aria-current={isActive ? 'page' : undefined}
        onClick={onOpen}
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
          justifyContent: fullWidth ? 'space-between' : 'center',
          gap: 1,
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
        <Typography component="span" variant="button" aria-hidden>
          ▾
        </Typography>
      </ButtonBase>

      <Menu
        id={`${triggerId}-menu`}
        anchorEl={anchorEl}
        open={isOpen}
        onClose={onClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{ paper: { sx: { mt: 1, minWidth: 240 } } }}
      >
        {routes.map((route) => (
          <MenuItem
            key={route.id}
            component={RouterLink}
            to={route.fullPath}
            selected={activeRouteId === route.id}
            onClick={onClose}
          >
            {t(route.labelKey)}
          </MenuItem>
        ))}
      </Menu>
    </>
  )
}
