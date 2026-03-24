import {
  Box,
  Container,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Typography,
} from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useState } from 'react'
import { getRoutesForRole } from '../routes/appRoutes.ts'
import { useAuth } from '../shared/auth/useAuth.ts'
import { LanguageSwitcher } from '../shared/i18n/LanguageSwitcher.tsx'

export function MainLayout() {
  const { t } = useTranslation()
  const location = useLocation()
  const { authStatus, user } = useAuth()
  const [isNavOpen, setIsNavOpen] = useState(false)
  const role = user?.role ?? 'guest'

  const visibleRoutes = authStatus === 'checking' ? [] : getRoutesForRole(role)

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <IconButton
        color="inherit"
        sx={{
          position: 'fixed',
          top: 16,
          right: 16,
          zIndex: (theme) => theme.zIndex.drawer + 1,
          bgcolor: 'rgba(15, 23, 42, 0.8)',
          '&:hover': {
            bgcolor: 'rgba(30, 41, 59, 0.9)',
          },
        }}
        onClick={() => setIsNavOpen(true)}
        aria-label={t('layout.openNavigation')}
      >
        <MenuIcon />
      </IconButton>

      <Drawer
        anchor="right"
        open={isNavOpen}
        onClose={() => setIsNavOpen(false)}
      >
        <Box
          sx={{
            width: 300,
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
          }}
          role="presentation"
          onClick={() => setIsNavOpen(false)}
          onKeyDown={(event) => {
            if (event.key === 'Escape') {
              setIsNavOpen(false)
            }
          }}
        >
          <Box
            sx={{
              px: 2,
              py: 1.8,
            }}
          >
            <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: '0.06em' }}>
              {t('appTitle')}
            </Typography>
          </Box>
          <Divider />
          <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
            <List>
              {visibleRoutes.map((route) => (
                <ListItem key={route.id} disablePadding>
                  <ListItemButton
                    component={Link}
                    to={route.fullPath}
                    selected={location.pathname.startsWith(route.fullPath)}
                  >
                    <ListItemText primary={t(route.labelKey)} />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Box>
          <Divider />
          <Box
            sx={{
              px: 2,
              py: 1.2,
              display: 'flex',
              justifyContent: 'flex-end',
            }}
          >
            <LanguageSwitcher />
          </Box>
        </Box>
      </Drawer>

      <Container
        sx={{
          flexGrow: 1,
          py: 3,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Outlet />
      </Container>
    </Box>
  )
}


