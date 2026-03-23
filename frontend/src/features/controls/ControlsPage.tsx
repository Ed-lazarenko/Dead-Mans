import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useControlsPage } from './useControlsPage.ts'

export function ControlsPage() {
  const { t } = useTranslation()
  const {
    data,
    isLoading,
    isBusy,
    startMutation,
    pauseMutation,
    resumeMutation,
    nextRoundMutation,
    resetMutation,
    closeAllLoadoutCards,
  } = useControlsPage()

  if (isLoading || !data) {
    return (
      <Paper sx={{ p: 3 }}>
        <Typography>{t('pages.controls')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('controls.loading')}
        </Typography>
      </Paper>
    )
  }

  return (
    <Paper sx={{ p: 3, display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h5">{t('nav.controls')}</Typography>

      <Box>
        <Typography variant="body2" color="text.secondary">
          {t('controls.currentState')}
        </Typography>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mt: 1 }}>
          <Chip label={t('controls.phase', { phase: data.phase })} color="info" />
          <Chip
            label={t('controls.round', { current: data.currentRound, total: data.totalRounds })}
            color="primary"
          />
          {data.lastActionAt && (
            <Typography variant="caption" color="text.secondary">
              {t('controls.lastAction', {
                time: new Date(data.lastActionAt).toLocaleTimeString(),
              })}
            </Typography>
          )}
        </Stack>
      </Box>

      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {t('controls.quickActions')}
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap">
          <Button
            variant="contained"
            onClick={() => startMutation.mutate()}
            disabled={isBusy || data.phase === 'running'}
          >
            {t('controls.start')}
          </Button>
          <Button
            variant="outlined"
            onClick={() => pauseMutation.mutate()}
            disabled={isBusy || data.phase !== 'running'}
          >
            {t('controls.pause')}
          </Button>
          <Button
            variant="outlined"
            onClick={() => resumeMutation.mutate()}
            disabled={isBusy || data.phase !== 'paused'}
          >
            {t('controls.resume')}
          </Button>
          <Button
            variant="outlined"
            onClick={() => nextRoundMutation.mutate()}
            disabled={isBusy || data.phase === 'idle'}
          >
            {t('controls.nextRound')}
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => resetMutation.mutate()}
            disabled={isBusy}
          >
            {t('controls.resetAll')}
          </Button>
          <Button
            variant="outlined"
            color="warning"
            onClick={closeAllLoadoutCards}
          >
            {t('controls.closeAllLoadoutCards')}
          </Button>
        </Stack>
      </Box>

      <Typography variant="caption" color="text.secondary">
        {t('controls.mockNotice')}
      </Typography>
    </Paper>
  )
}

