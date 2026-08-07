import { useId, useState, type MouseEvent } from 'react'
import type { ParseKeys } from 'i18next'
import { ButtonBase, Menu, MenuItem, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import { gameSetupDraftQueryOptions } from '../features/game-setup/index.ts'
import {
  adminModifiersRoute,
  adminQuestionsRoute,
  catalogModifiersRoute,
  catalogQuestionsRoute,
  gameSetupRoute,
  type PanelRouteDefinition,
} from '../routes/app-routes.ts'

const adminMenus: ReadonlyArray<{
  id: 'game-setup' | 'global-settings'
  labelKey: Extract<ParseKeys, `navigation.menus.${string}`>
  routes: readonly PanelRouteDefinition[]
}> = [
  {
    id: 'game-setup',
    labelKey: 'navigation.menus.gameSetup',
    routes: [gameSetupRoute, adminModifiersRoute, adminQuestionsRoute],
  },
  {
    id: 'global-settings',
    labelKey: 'navigation.menus.globalSettings',
    routes: [catalogModifiersRoute, catalogQuestionsRoute],
  },
]

interface PanelAdminNavigationProps {
  activeRouteId: string | undefined
  layout: 'inline' | 'stacked'
}

export function PanelAdminNavigation({ activeRouteId, layout }: PanelAdminNavigationProps) {
  const { t } = useTranslation()
  const isStacked = layout === 'stacked'
  const navigationId = useId()
  const [openMenuId, setOpenMenuId] = useState<(typeof adminMenus)[number]['id'] | null>(null)
  const [anchorByMenuId, setAnchorByMenuId] = useState<
    Partial<Record<(typeof adminMenus)[number]['id'], HTMLElement | null>>
  >({})
  const { data: draftState } = useQuery({
    ...gameSetupDraftQueryOptions,
    staleTime: 60_000,
  })
  const hasDraftGame = draftState?.snapshot != null

  const handleMenuOpen =
    (menuId: (typeof adminMenus)[number]['id']) => (event: MouseEvent<HTMLElement>) => {
      setAnchorByMenuId((current) => ({
        ...current,
        [menuId]: event.currentTarget,
      }))
      setOpenMenuId(menuId)
    }

  const handleMenuClose = () => {
    setOpenMenuId(null)
  }

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
      {adminMenus.map((menu) => (
        <AdminNavigationMenu
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
          disableCurrentGameChildren={menu.id === 'game-setup' && !hasDraftGame}
        />
      ))}
    </Stack>
  )
}

interface AdminNavigationMenuProps {
  triggerId: string
  label: string
  routes: readonly PanelRouteDefinition[]
  activeRouteId: string | undefined
  anchorEl: HTMLElement | null
  isOpen: boolean
  onOpen: (event: MouseEvent<HTMLElement>) => void
  onClose: () => void
  fullWidth?: boolean
  disableCurrentGameChildren?: boolean
}

function AdminNavigationMenu({
  triggerId,
  label,
  routes,
  activeRouteId,
  anchorEl,
  isOpen,
  onOpen,
  onClose,
  fullWidth = false,
  disableCurrentGameChildren = false,
}: AdminNavigationMenuProps) {
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
          color: isActive ? 'warning.light' : 'text.secondary',
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
        slotProps={{ paper: { sx: { mt: 1, minWidth: 260 } } }}
      >
        {routes.map((route, index) => {
          const isRouteDisabled = disableCurrentGameChildren && index > 0
          const routeLabel = t(route.labelKey)

          if (isRouteDisabled) {
            return (
              <MenuItem key={route.id} disabled>
                {routeLabel}
              </MenuItem>
            )
          }

          return (
            <MenuItem
              key={route.id}
              component={RouterLink}
              to={route.fullPath}
              selected={activeRouteId === route.id}
              onClick={onClose}
            >
              {routeLabel}
            </MenuItem>
          )
        })}
      </Menu>
    </>
  )
}
