import { Box, Paper, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useLoadoutPage } from './useLoadoutPage.ts'
import { LoadoutBoardGrid } from './ui/LoadoutBoardGrid.tsx'
import { LoadoutFullscreenDialog } from './ui/LoadoutFullscreenDialog.tsx'

export function LoadoutPage() {
  const { t } = useTranslation()
  const {
    data,
    isLoading,
    isCellOpened,
    handleCellClick,
    fullscreenCell,
    closeFullscreen,
  } = useLoadoutPage()

  if (isLoading || !data) {
    return (
      <Paper sx={{ p: 3 }}>
        <Typography>{t('pages.loadout')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('loadout.loading')}
        </Typography>
      </Paper>
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
