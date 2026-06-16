import { Alert, Box, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { PageShell, PageStatePanel, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import { CreateGameSetupDialog } from './ui/CreateGameSetupDialog.tsx'
import { GameSetupBoardNotices } from './ui/GameSetupBoardNotices.tsx'
import { GameSetupEmptyState } from './ui/GameSetupEmptyState.tsx'
import { GameSetupGrid } from './ui/GameSetupGrid.tsx'
import { GameSetupSettingsSidebar } from './ui/GameSetupSettingsSidebar.tsx'
import { GameSetupSyncActions } from './ui/GameSetupSyncActions.tsx'
import { useGameSetupPage } from './use-game-setup-page.ts'

export function GameSetupPage() {
  const { t } = useTranslation()
  const {
    snapshot,
    draft,
    isLoading,
    isError,
    isEmpty,
    isDirty,
    syncStatus,
    remoteChangeNotice,
    draftRemovedNotice,
    saveErrorMessage,
    resetErrorMessage,
    updateDraft,
    applyLayoutChange,
    saveDraft,
    reloadFromServer,
    createDraft,
    deleteDraft,
    isCreating,
    isResetting,
    isSaving,
    cellMediaDisplayByCellId,
    isCellMediaBusy,
    cellMediaErrorKey,
    uploadCellMedia,
    deleteCellMedia,
    dismissCellMediaError,
    dismissRemoteChangeNotice,
    dismissDraftRemovedNotice,
  } = useGameSetupPage()

  if (isLoading) {
    return (
      <PageStatePanel title={t('gameSetup.title')} message={t('gameSetup.loading')} showSpinner />
    )
  }

  if (isError) {
    return (
      <PageStatePanel
        title={t('gameSetup.title')}
        message={t('gameSetup.errorLoading')}
        tone="error"
      />
    )
  }

  if (!snapshot || !draft) {
    return (
      <>
        <GameSetupEmptyState
          draftRemovedNotice={draftRemovedNotice}
          onDismissDraftRemovedNotice={dismissDraftRemovedNotice}
        />
        <CreateGameSetupDialog
          open={isEmpty}
          isSubmitting={isCreating}
          onCreate={async (title) => {
            await createDraft({ title })
          }}
        />
      </>
    )
  }

  return (
    <PageShell variant="split">
      <GameSetupSettingsSidebar
        draft={draft}
        onDraftChange={updateDraft}
        onLayoutChange={applyLayoutChange}
        isResetting={isResetting}
        onReset={deleteDraft}
      />

      <SectionCard
        sx={{
          flex: 1,
          minWidth: 0,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <SectionHeader
          title={<Typography variant="h5">{t('gameSetup.boardTitle')}</Typography>}
          description={t('gameSetup.boardDescription')}
          actions={
            <GameSetupSyncActions
              syncStatus={syncStatus}
              isDirty={isDirty}
              isSaving={isSaving}
              onSave={() => void saveDraft()}
            />
          }
        />

        <Alert severity="info" sx={{ mt: 2 }}>
          {t('gameSetup.persistenceHint')}
        </Alert>

        <GameSetupBoardNotices
          remoteChangeNotice={remoteChangeNotice}
          onDismissRemoteChange={dismissRemoteChangeNotice}
          onReloadFromServer={() => void reloadFromServer()}
          saveErrorMessage={saveErrorMessage}
          resetErrorMessage={resetErrorMessage}
          cellMediaErrorKey={cellMediaErrorKey}
          onDismissCellMediaError={dismissCellMediaError}
        />

        <Box sx={{ mt: 3, flex: 1, minHeight: 0 }}>
          <GameSetupGrid
            snapshot={snapshot}
            draft={draft}
            onDraftChange={updateDraft}
            cellMediaDisplayByCellId={cellMediaDisplayByCellId}
            isCellMediaBusy={isCellMediaBusy}
            onUploadCellMedia={(cellId, file) => void uploadCellMedia(cellId, file)}
            onDeleteCellMedia={deleteCellMedia}
          />
        </Box>
      </SectionCard>
    </PageShell>
  )
}
