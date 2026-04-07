import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Paper,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell } from '../../shared/api/contracts/index.ts'
import { queryKeys } from '../../shared/api/query-keys.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { PageStatePanel } from '../../shared/ui/PageStatePanel.tsx'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { openGameBoardCell } from './api/game-board-data-access.ts'
import { GameBoardGrid } from './ui/GameBoardGrid.tsx'
import { useGameBoardPage } from './use-game-board-page.ts'

export function GameBoardPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [pendingCell, setPendingCell] = useState<GameBoardCell | null>(null)
  const [toastMessage, setToastMessage] = useState<string | null>(null)
  const { data, isError, isLoading } = useGameBoardPage()
  const isAdmin = useMemo(() => user?.roles.includes('admin') === true, [user?.roles])
  const openCellMutation = useMutation({
    mutationFn: (cellId: string) => openGameBoardCell(cellId),
    onSuccess: async () => {
      setToastMessage(t('gameBoard.openSuccess'))
      await queryClient.invalidateQueries({ queryKey: queryKeys.gameBoard.currentSnapshot() })
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        if (error.status === 403) {
          setToastMessage(t('gameBoard.openForbidden'))
          return
        }
        if (error.status === 404) {
          setToastMessage(t('gameBoard.openNotFound'))
          return
        }
      }

      setToastMessage(t('gameBoard.openFailed'))
    },
    onSettled: () => {
      setPendingCell(null)
    },
  })

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
        <GameBoardGrid
          snapshot={snapshot}
          canOpenCells={isAdmin && !openCellMutation.isPending}
          onCellRequestOpen={(cell) => {
            if (!isAdmin || openCellMutation.isPending) {
              return
            }
            setPendingCell(cell)
          }}
        />
      </Paper>

      <Dialog
        open={pendingCell !== null}
        onClose={() => setPendingCell(null)}
        aria-labelledby="open-cell-dialog-title"
      >
        <DialogTitle id="open-cell-dialog-title">{t('gameBoard.openConfirmTitle')}</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            {t('gameBoard.openConfirmDescription', {
              cost: pendingCell?.cost ?? 0,
              row: pendingCell?.row ?? '-',
              col: pendingCell?.col ?? '-',
            })}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPendingCell(null)} disabled={openCellMutation.isPending}>
            {t('gameBoard.openCancel')}
          </Button>
          <Button
            variant="contained"
            onClick={() => {
              if (!pendingCell) {
                return
              }
              openCellMutation.mutate(pendingCell.id)
            }}
            disabled={openCellMutation.isPending}
          >
            {t('gameBoard.openConfirm')}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={toastMessage !== null}
        autoHideDuration={3000}
        onClose={() => setToastMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={() => setToastMessage(null)} severity="info" variant="filled">
          {toastMessage}
        </Alert>
      </Snackbar>
    </Box>
  )
}
