import { Chip, Stack } from '@mui/material'
import { useTranslation } from 'react-i18next'
import {
  AppToast,
  ConfirmDialog,
  PageShell,
  PageStatePanel,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { ActiveCardRunPanel } from '../game-card-runs/ui/ActiveCardRunPanel.tsx'
import { useGameCardRunPanel } from '../game-card-runs/use-game-card-run-panel.ts'
import { GameBoardGrid } from './ui/GameBoardGrid.tsx'
import { useGameBoardPage } from './use-game-board-page.ts'
import { useOpenGameBoardCell } from './use-open-game-board-cell.ts'

export function GameBoardPage() {
  const { t } = useTranslation()
  const { data, activeRun, isError, isLoading } = useGameBoardPage()
  const openCellOptions =
    data?.cells
      .filter((cell) => cell.state === 'open')
      .map((cell) => ({
        id: cell.id,
        label: `${cell.row}:${cell.col} - ${cell.title ?? t('gameBoard.cellLabel')} (${cell.cost})`,
      })) ?? []
  const {
    canManageCardRuns,
    eligibleTeamsQuery,
    eligibleTeamOptions,
    resolvedSelectedCellId,
    resolvedSelectedTeamId,
    finalStatus,
    finalScoreInput,
    notes,
    setSelectedCellId,
    setSelectedTeamId,
    setFinalStatus,
    setFinalScoreInput,
    setNotes,
    startRun,
    finalizeRun,
    isStarting,
    isFinalizing,
    toastMessage: runtimeToastMessage,
    dismissToast: dismissRuntimeToast,
  } = useGameCardRunPanel(openCellOptions)
  const {
    pendingCell,
    toastMessage,
    canOpenCells,
    isSubmitting,
    requestOpenCell,
    confirmOpenCell,
    dismissPendingCell,
    dismissToast,
  } = useOpenGameBoardCell()

  if (isLoading) {
    return (
      <PageStatePanel title={t('gameBoard.title')} message={t('gameBoard.loading')} showSpinner />
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
    <PageShell variant="centered" sx={{ width: '100%', px: 0 }}>
      <SectionCard
        sx={{
          width: '100%',
          maxWidth: 1180,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <SectionHeader
          title={snapshot.title || t('gameBoard.title')}
          description={snapshot.description}
          actions={
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
              <Chip
                size="small"
                color={
                  snapshot.status === 'active'
                    ? 'success'
                    : snapshot.status === 'ready'
                      ? 'info'
                      : 'default'
                }
                label={t(
                  snapshot.status === 'active'
                    ? 'gameBoard.statusActive'
                    : snapshot.status === 'ready'
                      ? 'gameBoard.statusReady'
                      : 'gameBoard.statusFinished',
                )}
              />
              {activeRun ? (
                <Chip
                  size="small"
                  color="warning"
                  variant="outlined"
                  label={t('gameBoard.activeRunLabel', {
                    teamSlot: activeRun.teamSlotIndex,
                    score: activeRun.baseScore,
                  })}
                />
              ) : null}
            </Stack>
          }
        />
        <GameBoardGrid
          snapshot={snapshot}
          canOpenCells={canOpenCells}
          onCellRequestOpen={requestOpenCell}
        />
        <ActiveCardRunPanel
          openCellOptions={openCellOptions.map((option) => ({
            value: option.id,
            label: option.label,
          }))}
          eligibleTeamOptions={eligibleTeamOptions}
          activeRun={activeRun}
          canManageCardRuns={canManageCardRuns}
          isLoadingTeams={eligibleTeamsQuery.isLoading}
          isStarting={isStarting}
          isFinalizing={isFinalizing}
          selectedCellId={resolvedSelectedCellId}
          selectedTeamId={resolvedSelectedTeamId}
          finalStatus={finalStatus}
          finalScoreInput={finalScoreInput}
          notes={notes}
          onSelectedCellChange={setSelectedCellId}
          onSelectedTeamChange={setSelectedTeamId}
          onFinalStatusChange={setFinalStatus}
          onFinalScoreInputChange={setFinalScoreInput}
          onNotesChange={setNotes}
          onStartRun={startRun}
          onFinalizeRun={finalizeRun}
          labels={{
            title: t('gameBoard.runPanelTitle'),
            idleDescription: t('gameBoard.runPanelIdleDescription'),
            activeDescription: t('gameBoard.runPanelActiveDescription'),
            openCell: t('gameBoard.runPanelOpenCell'),
            team: t('gameBoard.runPanelTeam'),
            start: t('gameBoard.runPanelStart'),
            status: t('gameBoard.runPanelStatus'),
            finalScore: t('gameBoard.runPanelFinalScore'),
            notes: t('gameBoard.runPanelNotes'),
            complete: t('gameBoard.runPanelComplete'),
            noOpenCells: t('gameBoard.runPanelNoOpenCells'),
            noTeams: t('gameBoard.runPanelNoTeams'),
          }}
        />
      </SectionCard>

      <ConfirmDialog
        open={pendingCell !== null}
        onClose={dismissPendingCell}
        onConfirm={confirmOpenCell}
        isBusy={isSubmitting}
        title={t('gameBoard.openConfirmTitle')}
        description={t('gameBoard.openConfirmDescription', {
          cost: pendingCell?.cost ?? 0,
          row: pendingCell?.row ?? '-',
          col: pendingCell?.col ?? '-',
        })}
        cancelLabel={t('gameBoard.openCancel')}
        confirmLabel={t('gameBoard.openConfirm')}
      />

      <AppToast
        message={toastMessage}
        onClose={dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
      <AppToast
        message={runtimeToastMessage}
        onClose={dismissRuntimeToast}
        severity="info"
        autoHideDuration={3000}
      />
    </PageShell>
  )
}
