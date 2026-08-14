import { Box, Stack, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { panelRootPath } from '../../routes/app-routes.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { LanguageSwitcher } from '../../shared/i18n/LanguageSwitcher.tsx'
import { huntAuthCardSx, huntBrassTitleSx, huntOverlineSx } from '../../shared/theme/surface-sx.ts'
import { uiTokens } from '../../shared/theme/tokens.ts'
import { AppButton, AuthScreenShell, CenteredProgress, SectionCard } from '../../shared/ui/index.ts'

export function AuthLandingPage() {
  const { t } = useTranslation()
  const { authStatus, isAuthenticated, startTwitchLogin } = useAuth()

  if (authStatus === 'checking') {
    return <CenteredProgress minHeight="100vh" message={t('auth.checkingSession')} />
  }

  if (isAuthenticated) {
    return <Navigate to={panelRootPath} replace />
  }

  return (
    <AuthScreenShell>
      <Box
        sx={{
          position: 'absolute',
          top: 16,
          right: 16,
          zIndex: 2,
        }}
      >
        <LanguageSwitcher />
      </Box>
      <SectionCard sx={[(theme) => huntAuthCardSx(theme), { px: { xs: 4, sm: 6 }, py: 5 }]}>
        <Stack spacing={3}>
          <Box>
            <Typography variant="overline" sx={huntOverlineSx}>
              {t('auth.subtitle')}
            </Typography>
            <Typography
              variant="h3"
              sx={{
                ...huntBrassTitleSx,
                mt: 1,
                fontSize: { xs: '1.75rem', sm: '2.2rem' },
              }}
            >
              {t('appTitle')}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
              {t('auth.description')}
            </Typography>
          </Box>

          <AppButton
            size="large"
            onClick={startTwitchLogin}
            sx={{
              mt: 1,
              px: 4,
              py: 1.2,
              backgroundColor: uiTokens.brand.twitch,
              border: 'none',
              backgroundImage: 'none',
              color: 'common.white',
              '&:hover': {
                backgroundColor: uiTokens.brand.twitchHover,
                backgroundImage: 'none',
              },
            }}
          >
            {t('auth.button')}
          </AppButton>
        </Stack>
      </SectionCard>
    </AuthScreenShell>
  )
}
