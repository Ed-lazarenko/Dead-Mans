import { Box, Container, Typography } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '../shared/i18n/LanguageSwitcher.tsx'

export function MainLayout() {
  const { t } = useTranslation()

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Box
        sx={{
          px: 3,
          py: 2,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}
      >
        <Typography variant="h6" sx={{ fontWeight: 700, letterSpacing: '0.06em' }}>
          {t('appTitle')}
        </Typography>
        <LanguageSwitcher />
      </Box>

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


