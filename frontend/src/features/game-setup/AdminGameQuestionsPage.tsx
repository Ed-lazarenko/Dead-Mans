import { useTranslation } from 'react-i18next'
import { PageShell, PageStatePanel } from '../../shared/ui/index.ts'
import { GameSetupBoardNotices } from './ui/GameSetupBoardNotices.tsx'
import { GameSetupEmptyState } from './ui/GameSetupEmptyState.tsx'
import { GameSetupQuestionsSection } from './ui/GameSetupQuestionsSection.tsx'
import { GameSetupSyncActions } from './ui/GameSetupSyncActions.tsx'
import { useGameSetupPage } from './use-game-setup-page.ts'

export function AdminGameQuestionsPage() {
  const { t } = useTranslation()
  const {
    draft,
    isLoading,
    isError,
    syncStatus,
    isDirty,
    isSaving,
    saveErrorMessage,
    resetErrorMessage,
    cellMediaErrorKey,
    remoteChangeNotice,
    draftRemovedNotice,
    saveDraft,
    reloadFromServer,
    toggleQuestion,
    setQuestionsEnabled,
    dismissRemoteChangeNotice,
    dismissDraftRemovedNotice,
    dismissCellMediaError,
  } = useGameSetupPage()

  if (isLoading) {
    return (
      <PageStatePanel
        title={t('navigation.items.adminQuestions.label')}
        message={t('gameSetup.loading')}
        showSpinner
      />
    )
  }

  if (isError) {
    return (
      <PageStatePanel
        title={t('navigation.items.adminQuestions.label')}
        message={t('gameSetup.errorLoading')}
        tone="error"
      />
    )
  }

  if (!draft) {
    return (
      <GameSetupEmptyState
        draftRemovedNotice={draftRemovedNotice}
        onDismissDraftRemovedNotice={dismissDraftRemovedNotice}
      />
    )
  }

  return (
    <PageShell>
      <GameSetupQuestionsSection
        draft={draft}
        onToggle={toggleQuestion}
        onBulkSetEnabled={setQuestionsEnabled}
        actions={
          <GameSetupSyncActions
            syncStatus={syncStatus}
            isDirty={isDirty}
            isSaving={isSaving}
            onSave={() => void saveDraft()}
          />
        }
      />

      <GameSetupBoardNotices
        remoteChangeNotice={remoteChangeNotice}
        onDismissRemoteChange={dismissRemoteChangeNotice}
        onReloadFromServer={() => void reloadFromServer()}
        saveErrorMessage={saveErrorMessage}
        resetErrorMessage={resetErrorMessage}
        cellMediaErrorKey={cellMediaErrorKey}
        onDismissCellMediaError={dismissCellMediaError}
      />
    </PageShell>
  )
}
