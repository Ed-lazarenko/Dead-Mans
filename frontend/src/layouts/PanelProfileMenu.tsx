import { useState, type MouseEvent } from 'react'
import { Box, ButtonBase, Divider, Menu, MenuItem, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { gameSetupRoute, teamRegistrationsRoute } from '../routes/app-routes.ts'
import type { AuthContextValue, AuthUser } from '../shared/auth/auth-context.ts'
import { LanguageSwitcher } from '../shared/i18n/LanguageSwitcher.tsx'
import { huntOverlineSx } from '../shared/theme/surface-sx.ts'

interface PanelProfileMenuProps {
  user: AuthUser
  activeRouteId: string | undefined
  onLogout: AuthContextValue['logout']
}

/**
 * Profile trigger plus its dropdown menu. Owns the menu anchor state and the
 * admin-only administration links; the menu itself is portalled, so rendering
 * it next to the trigger does not affect the header layout.
 */
export function PanelProfileMenu({ user, activeRouteId, onLogout }: PanelProfileMenuProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [profileAnchor, setProfileAnchor] = useState<HTMLElement | null>(null)
  const canAdminister = user.roles.includes('admin')

  const closeProfile = () => setProfileAnchor(null)

  const handleProfileOpen = (event: MouseEvent<HTMLElement>) => {
    setProfileAnchor(event.currentTarget)
  }

  const handleLogout = async () => {
    closeProfile()
    await onLogout()
    navigate('/', { replace: true })
  }

  return (
    <>
      <ButtonBase
        aria-controls={profileAnchor ? 'profile-menu' : undefined}
        aria-haspopup="menu"
        aria-expanded={profileAnchor ? 'true' : undefined}
        onClick={handleProfileOpen}
        sx={(theme) => ({
          maxWidth: { xs: 140, sm: 220 },
          px: 1.5,
          py: 1,
          borderRadius: 1,
          border: `1px solid ${alpha(theme.palette.primary.main, 0.28)}`,
          backgroundColor: alpha(theme.palette.common.black, 0.16),
          '&:hover': {
            borderColor: alpha(theme.palette.primary.main, 0.55),
            backgroundColor: alpha(theme.palette.primary.main, 0.08),
          },
        })}
      >
        <Typography variant="body2" fontWeight={700} noWrap>
          {user.displayName}
        </Typography>
        <Box component="span" aria-hidden sx={{ ml: 1, color: 'primary.main' }}>
          ▾
        </Box>
      </ButtonBase>

      <Menu
        id="profile-menu"
        anchorEl={profileAnchor}
        open={Boolean(profileAnchor)}
        onClose={closeProfile}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { mt: 1, minWidth: 260 } } }}
      >
        <Box sx={{ px: 2, py: 1.25 }}>
          <Typography variant="overline" sx={huntOverlineSx}>
            {t('navigation.profile')}
          </Typography>
          <Typography variant="body1" fontWeight={700}>
            {user.displayName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {user.roles.map((role) => t(`navigation.roles.${role}`)).join(', ')}
          </Typography>
        </Box>

        {canAdminister ? <Divider /> : null}
        {canAdminister ? (
          <Typography
            variant="overline"
            sx={{ ...huntOverlineSx, display: 'block', px: 2, pt: 1.5 }}
          >
            {t('navigation.administration')}
          </Typography>
        ) : null}
        {canAdminister ? (
          <MenuItem
            component={RouterLink}
            to={gameSetupRoute.fullPath}
            selected={activeRouteId === gameSetupRoute.id}
            onClick={closeProfile}
          >
            {t('navigation.menus.gameSetup')}
          </MenuItem>
        ) : null}
        {canAdminister ? (
          <MenuItem
            component={RouterLink}
            to={teamRegistrationsRoute.fullPath}
            selected={activeRouteId === teamRegistrationsRoute.id}
            onClick={closeProfile}
          >
            {t(teamRegistrationsRoute.labelKey)}
          </MenuItem>
        ) : null}

        <Divider />
        <Box sx={{ px: 2, py: 1.25 }}>
          <Typography variant="caption" color="text.secondary">
            {t('navigation.language')}
          </Typography>
          <LanguageSwitcher sx={{ mt: 0.75, width: '100%' }} />
        </Box>
        <Divider />
        <MenuItem onClick={() => void handleLogout()}>{t('navigation.logout')}</MenuItem>
      </Menu>
    </>
  )
}
