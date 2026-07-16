import { useState, type MouseEvent } from 'react'
import { Badge, Box, ButtonBase, Container, Menu, MenuItem, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import {
  gameApplicationRoute,
  gameBoardRoute,
  getPanelRouteByPath,
  teamRegistrationsRoute,
} from '../routes/app-routes.ts'
import { useAuth } from '../shared/auth/use-auth.ts'
import { huntBrassTitleSx } from '../shared/theme/surface-sx.ts'
import {
  gameRegistrationAdminSnapshotQueryOptions,
  gameRegistrationSnapshotQueryOptions,
} from '../features/game-registration/index.ts'
import { PanelAdminNavigation } from './PanelAdminNavigation.tsx'
import { PanelPrimaryNavigation } from './PanelPrimaryNavigation.tsx'
import { PanelProfileMenu } from './PanelProfileMenu.tsx'

export function PanelNavigation() {
  const { t } = useTranslation()
  const location = useLocation()
  const { user, logout } = useAuth()
  const activeRoute = getPanelRouteByPath(location.pathname)
  const isAdminRoute = activeRoute?.group === 'admin'
  const canSeeStaffNotifications =
    user?.roles.includes('admin') === true || user?.roles.includes('moderator') === true
  const snapshotQuery = useQuery(gameRegistrationSnapshotQueryOptions)
  const adminSnapshotQuery = useQuery({
    ...gameRegistrationAdminSnapshotQueryOptions,
    enabled: canSeeStaffNotifications,
  })
  const [notificationAnchor, setNotificationAnchor] = useState<HTMLElement | null>(null)

  if (!user) {
    return null
  }

  const pendingInvitationsCount = snapshotQuery.data?.myPendingInvitations.length ?? 0
  const pendingInvitations = snapshotQuery.data?.myPendingInvitations ?? []
  const disbandRequestTeams =
    adminSnapshotQuery.data?.teams.filter(
      (team) => team.status === 'confirmed' && team.disbandRequestedAtUtc != null,
    ) ?? []
  const importantNotificationsCount = disbandRequestTeams.length
  const totalNotificationsCount = pendingInvitationsCount + importantNotificationsCount

  const openNotificationMenu = (event: MouseEvent<HTMLElement>) => {
    setNotificationAnchor(event.currentTarget)
  }

  const closeNotificationMenu = () => setNotificationAnchor(null)

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

          <Stack direction="row" spacing={1} alignItems="center">
            <ButtonBase
              aria-controls={notificationAnchor ? 'notification-menu' : undefined}
              aria-expanded={notificationAnchor ? 'true' : undefined}
              aria-haspopup="menu"
              aria-label={t('navigation.openNotifications')}
              onClick={openNotificationMenu}
              sx={(theme) => ({
                width: 42,
                height: 42,
                borderRadius: 999,
                border: `1px solid ${alpha(theme.palette.primary.main, 0.28)}`,
                backgroundColor: alpha(theme.palette.common.black, 0.16),
                color: 'primary.main',
                '&:hover': {
                  borderColor: alpha(theme.palette.primary.main, 0.55),
                  backgroundColor: alpha(theme.palette.primary.main, 0.08),
                },
              })}
            >
              <Badge color="warning" badgeContent={totalNotificationsCount} max={9}>
                <Typography component="span" sx={{ fontSize: 18, lineHeight: 1 }}>
                  🔔
                </Typography>
              </Badge>
            </ButtonBase>

            <Menu
              id="notification-menu"
              anchorEl={notificationAnchor}
              open={Boolean(notificationAnchor)}
              onClose={closeNotificationMenu}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
              transformOrigin={{ vertical: 'top', horizontal: 'right' }}
              slotProps={{ paper: { sx: { mt: 1, minWidth: 320, maxWidth: 380 } } }}
            >
              <Box sx={{ px: 2, py: 1.25 }}>
                <Typography variant="subtitle2">{t('navigation.notifications')}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {totalNotificationsCount > 0
                    ? t('navigation.notificationCount', { count: totalNotificationsCount })
                    : t('navigation.notificationsEmpty')}
                </Typography>
              </Box>

              {totalNotificationsCount === 0 ? (
                <MenuItem
                  component={RouterLink}
                  to={gameApplicationRoute.fullPath}
                  onClick={closeNotificationMenu}
                >
                  {t('navigation.openApplicationPage')}
                </MenuItem>
              ) : (
                [
                  ...disbandRequestTeams.map((team) => (
                    <MenuItem
                      key={`disband-${team.teamId}`}
                      component={RouterLink}
                      to={teamRegistrationsRoute.fullPath}
                      onClick={closeNotificationMenu}
                      sx={{ whiteSpace: 'normal', alignItems: 'flex-start', py: 1.25 }}
                    >
                      <Stack spacing={0.5}>
                        <Typography variant="body2" fontWeight={700}>
                          {t('navigation.disbandRequestItemTitle', {
                            player: team.disbandRequestedByDisplayName ?? t('navigation.someone'),
                          })}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {t('navigation.disbandRequestItemDescription', {
                            slot: team.slotIndex,
                          })}
                        </Typography>
                      </Stack>
                    </MenuItem>
                  )),
                  ...pendingInvitations.map((invitation) => (
                    <MenuItem
                      key={`invitation-${invitation.invitationId}`}
                      component={RouterLink}
                      to={gameApplicationRoute.fullPath}
                      onClick={closeNotificationMenu}
                      sx={{ whiteSpace: 'normal', alignItems: 'flex-start', py: 1.25 }}
                    >
                      <Stack spacing={0.5}>
                        <Typography variant="body2" fontWeight={700}>
                          {t('navigation.invitationItemTitle', {
                            player: invitation.invitedByDisplayName ?? t('navigation.someone'),
                          })}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {t('navigation.invitationItemDescription', {
                            slot: invitation.slotIndex,
                          })}
                        </Typography>
                      </Stack>
                    </MenuItem>
                  )),
                ]
              )}
            </Menu>

            <PanelProfileMenu user={user} activeRouteId={activeRoute?.id} onLogout={logout} />
          </Stack>
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
