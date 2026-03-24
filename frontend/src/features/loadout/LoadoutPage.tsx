import { Box, Paper, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { PageStatePanel } from '../../shared/ui/PageStatePanel.tsx'
import { useLoadoutPage } from './useLoadoutPage.ts'
import { LoadoutBoardGrid } from './ui/LoadoutBoardGrid.tsx'
import { LoadoutFullscreenDialog } from './ui/LoadoutFullscreenDialog.tsx'

export function LoadoutPage() {
  const { t } = useTranslation()
  const {
    data,
    isError,
    isLoading,
    isCellOpened,
    handleCellClick,
    fullscreenCell,
    closeFullscreen,
  } = useLoadoutPage()

  if (isLoading) {
    return (
      <PageStatePanel
        title={t('nav.loadout')}
        message={t('loadout.loading')}
        showSpinner
      />
    )
  }

  if (isError || !data) {
    return (
      <PageStatePanel
        title={t('nav.loadout')}
        message={t('loadout.errorLoading')}
        tone="error"
      />
    )
  }

  return (
    <Box
      sx={{
        flex: 1,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        px: { xs: 1, sm: 2 },
      }}
    >
      <Paper
        sx={{
          p: { xs: 2, md: 3 },
          width: '100%',
          maxWidth: 960,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <Typography variant="h5" gutterBottom>
          {t('nav.loadout')}
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {t('loadout.hint')}
        </Typography>
        <LoadoutBoardGrid
          board={data}
          isCellOpened={isCellOpened}
          onCellClick={handleCellClick}
        />
        <LoadoutFullscreenDialog cell={fullscreenCell} onClose={closeFullscreen} />
      </Paper>
    </Box>
  )
}
