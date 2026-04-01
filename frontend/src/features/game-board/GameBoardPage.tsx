import { Box, Chip, Paper, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { PageStatePanel } from '../../shared/ui/PageStatePanel.tsx'
import { GameBoardGrid } from './ui/GameBoardGrid.tsx'
import { useGameBoardPage } from './use-game-board-page.ts'

export function GameBoardPage() {
  const { t } = useTranslation()
  const { data, isError, isLoading } = useGameBoardPage()

  if (isLoading) {
    return (
      <PageStatePanel
        title={t('gameBoard.title')}
        message={t('gameBoard.loading')}
        showSpinner
      />
    )
  }

  if (isError) {
    return (
      <PageStatePanel
        title={t('gameBoard.title')}
        message={t('gameBoard.errorLoading')}
        tone="error"
      />
    )
  }

  if (data === null) {
    return <PageStatePanel title={t('gameBoard.title')} message={t('gameBoard.empty')} />
  }

  if (!data) {
    return (
      <PageStatePanel
        title={t('gameBoard.title')}
        message={t('gameBoard.errorLoading')}
        tone="error"
      />
    )
  }

  const snapshot = data

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
          {snapshot.title || t('gameBoard.title')}
        </Typography>
        {snapshot.description ? (
          <Typography variant="body2" color="text.secondary" gutterBottom>
            {snapshot.description}
          </Typography>
        ) : null}
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
          <Chip
            size="small"
            color={snapshot.status === 'active' ? 'success' : 'default'}
            label={t(
              snapshot.status === 'active' ? 'gameBoard.statusActive' : 'gameBoard.statusFinished',
            )}
          />
        </Stack>
        <GameBoardGrid snapshot={snapshot} />
      </Paper>
    </Box>
  )
}
