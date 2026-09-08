import { Alert, Box, Stack, Typography } from '@mui/material'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { ImportGameQuestionSkippedItem } from '../../shared/api/contracts/index.ts'
import { downloadTextFile } from '../../shared/lib/download-file.ts'
import { AppButton, AppDialog, ConfirmDialog, PageShell } from '../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from './model/catalog-error.ts'
import {
  downloadQuestionImportFailureReport,
  formatSkippedQuestionWarning,
} from './model/question-import-report.ts'
import { QuestionCatalogList } from './ui/QuestionCatalogList.tsx'
import { QuestionCatalogMenu } from './ui/QuestionCatalogMenu.tsx'
import { QuestionCategoryDialog } from './ui/QuestionCategoryDialog.tsx'
import { QuestionFormDialog } from './ui/QuestionFormDialog.tsx'
import { useCatalogFeedback } from './use-catalog-feedback.ts'
import { useCatalogQuestions } from './use-catalog-questions.ts'

interface ImportReportState {
  fileName: string
  importedCount: number
  skippedQuestions: ImportGameQuestionSkippedItem[]
  errorMessage: string | null
}

export function CatalogQuestionsPage() {
  const { t, i18n } = useTranslation()
  const {
    search,
    setSearch,
    selectedCategoryId,
    setSelectedCategoryId,
    selectedCategory,
    catalogQuery,
    categoriesQuery,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitQuestion,
    categoryDialog,
    openCreateCategory,
    openEditCategory,
    closeCreateCategory,
    submitCategory,
    isSaving,
    isSavingCategory,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting,
    deleteCategoryTarget,
    requestDeleteCategory,
    cancelDeleteCategory,
    confirmDeleteCategory,
    isDeletingCategory,
    importQuestions,
    isImportingQuestions,
    downloadTemplate,
    isDownloadingTemplate,
  } = useCatalogQuestions()
  const {
    listError,
    successMessage,
    setSuccessMessage,
    clearListError,
    clearSuccessMessage,
    resetFeedback,
    showResolvedError,
  } = useCatalogFeedback(t)
  const [importReport, setImportReport] = useState<ImportReportState | null>(null)
  const [isCategoryBlockedDialogOpen, setIsCategoryBlockedDialogOpen] = useState(false)
  const importInputRef = useRef<HTMLInputElement | null>(null)

  const resetPageFeedback = () => {
    resetFeedback()
    setImportReport(null)
  }

  const handleConfirmDelete = async () => {
    resetPageFeedback()
    try {
      await confirmDelete()
    } catch (error) {
      cancelDelete()
      showResolvedError(error)
    }
  }

  const handleConfirmDeleteCategory = async () => {
    resetPageFeedback()
    try {
      await confirmDeleteCategory()
    } catch (error) {
      cancelDeleteCategory()
      showResolvedError(error)
    }
  }

  const categories = categoriesQuery.data ?? []
  const canAddQuestion = categories.length > 0
  const isSelectedCategoryProtected = selectedCategory?.isProtected ?? false

  const handleRenameCategoryClick = () => {
    if (selectedCategory) {
      openEditCategory(selectedCategory)
    }
  }

  const handleDeleteCategoryClick = () => {
    if (!selectedCategory) {
      return
    }

    if (selectedCategory.questionCount > 0) {
      setIsCategoryBlockedDialogOpen(true)
      return
    }

    requestDeleteCategory(selectedCategory)
  }

  const handleDownloadTemplate = async () => {
    resetPageFeedback()
    try {
      const templateLocale =
        (i18n.language ?? '').split('-')[0]?.toLowerCase() === 'ru' ? 'ru' : 'en'
      const content = await downloadTemplate(templateLocale)
      downloadTextFile(content, 'question-import-template.jsonc', 'application/json')
    } catch (error) {
      showResolvedError(error)
    }
  }

  const handleImportFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    resetPageFeedback()
    try {
      const result = await importQuestions(file)
      const skippedQuestions = result.skippedQuestions ?? []
      setSuccessMessage(
        skippedQuestions.length === 0
          ? t('gameCatalog.questions.importSuccess', { count: result.importedCount })
          : t('gameCatalog.questions.importPartial', {
              count: result.importedCount,
              skipped: skippedQuestions.length,
            }),
      )
      setImportReport(
        skippedQuestions.length > 0
          ? {
              fileName: file.name,
              importedCount: result.importedCount,
              skippedQuestions,
              errorMessage: null,
            }
          : null,
      )
    } catch (error) {
      const errorMessage = resolveCatalogErrorMessage(error, t)
      showResolvedError(error)
      setImportReport({
        fileName: file.name,
        importedCount: 0,
        skippedQuestions: [],
        errorMessage,
      })
    }
  }

  return (
    <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
      {listError ? (
        <Alert severity="error" sx={{ mb: 2 }} onClose={clearListError}>
          <Stack spacing={1}>
            <Typography variant="body2">{listError}</Typography>
            {importReport?.errorMessage ? (
              <>
                <Typography variant="body2" color="text.secondary">
                  {t('gameCatalog.questions.importErrorDescription')}
                </Typography>
                <AppButton
                  size="small"
                  tone="secondary"
                  sx={{ alignSelf: 'flex-start' }}
                  onClick={() => downloadQuestionImportFailureReport(importReport)}
                >
                  {t('gameCatalog.questions.downloadImportReport')}
                </AppButton>
              </>
            ) : null}
          </Stack>
        </Alert>
      ) : null}

      {successMessage ? (
        <Alert severity="success" sx={{ mb: 2 }} onClose={clearSuccessMessage}>
          {successMessage}
        </Alert>
      ) : null}

      {importReport && importReport.skippedQuestions.length > 0 ? (
        <Alert severity="warning" sx={{ mb: 2 }} onClose={() => setImportReport(null)}>
          <Stack spacing={1}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{ alignItems: { xs: 'flex-start', sm: 'center' } }}
            >
              <Typography variant="body2" sx={{ fontWeight: 700 }}>
                {t('gameCatalog.questions.importSkippedTitle')}
              </Typography>
              <AppButton
                size="small"
                tone="secondary"
                onClick={() => downloadQuestionImportFailureReport(importReport)}
              >
                {t('gameCatalog.questions.downloadImportReport')}
              </AppButton>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {t('gameCatalog.questions.importSkippedDescription')}
            </Typography>
            <Stack spacing={0.5}>
              {importReport.skippedQuestions.map((warning) => (
                <Typography
                  key={`${warning.rowNumber}:${warning.questionText ?? ''}:${warning.reason}`}
                  variant="body2"
                >
                  {formatSkippedQuestionWarning(warning, t)}
                </Typography>
              ))}
            </Stack>
          </Stack>
        </Alert>
      ) : null}

      <input
        ref={importInputRef}
        type="file"
        accept=".json,.jsonc,application/json"
        hidden
        onChange={(event) => void handleImportFileSelected(event)}
      />

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          alignItems: 'stretch',
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) minmax(320px, 360px)' },
        }}
      >
        <Box sx={{ minWidth: 0 }}>
          <QuestionCatalogList
            search={search}
            selectedCategory={selectedCategory}
            questions={catalogQuery.data ?? []}
            isLoading={catalogQuery.isLoading}
            isError={catalogQuery.isError}
            onSearchChange={setSearch}
            onEdit={openEdit}
            onDelete={requestDelete}
          />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <QuestionCatalogMenu
            categories={categories}
            selectedCategoryId={selectedCategoryId}
            canAddQuestion={canAddQuestion}
            canRenameCategory={
              selectedCategory !== null && !isSelectedCategoryProtected && !isSavingCategory
            }
            canDeleteCategory={
              selectedCategory !== null && !isSelectedCategoryProtected && !isDeletingCategory
            }
            isCategoriesLoading={categoriesQuery.isLoading}
            isCategoriesError={categoriesQuery.isError}
            isImportingQuestions={isImportingQuestions}
            isDownloadingTemplate={isDownloadingTemplate}
            onSelectCategory={setSelectedCategoryId}
            onCreateQuestion={openCreate}
            onDownloadTemplate={() => void handleDownloadTemplate()}
            onUploadQuestions={() => importInputRef.current?.click()}
            onCreateCategory={openCreateCategory}
            onRenameCategory={handleRenameCategoryClick}
            onDeleteCategory={handleDeleteCategoryClick}
          />
        </Box>
      </Box>

      <QuestionFormDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'create'}
        initial={dialog?.mode === 'edit' ? dialog.question : undefined}
        categories={categories}
        isBusy={isSaving}
        onClose={closeDialog}
        onSubmit={submitQuestion}
      />
      <QuestionCategoryDialog
        open={categoryDialog !== null}
        mode={categoryDialog?.mode ?? 'create'}
        initialName={categoryDialog?.mode === 'edit' ? categoryDialog.category.name : ''}
        isBusy={isSavingCategory}
        onClose={closeCreateCategory}
        onSubmit={submitCategory}
      />
      <ConfirmDialog
        open={deleteTarget !== null}
        title={t('gameCatalog.questions.deleteTitle')}
        description={t('gameCatalog.questions.deleteConfirm')}
        confirmLabel={t('gameCatalog.actions.delete')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        isBusy={isDeleting}
        onClose={cancelDelete}
        onConfirm={() => void handleConfirmDelete()}
      />
      <ConfirmDialog
        open={deleteCategoryTarget !== null}
        title={t('gameCatalog.questions.deleteCategoryTitle')}
        description={t('gameCatalog.questions.deleteCategoryConfirm', {
          name: deleteCategoryTarget?.name ?? '',
        })}
        confirmLabel={t('gameCatalog.actions.delete')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        isBusy={isDeletingCategory}
        onClose={cancelDeleteCategory}
        onConfirm={() => void handleConfirmDeleteCategory()}
      />
      <AppDialog
        open={isCategoryBlockedDialogOpen}
        onClose={() => setIsCategoryBlockedDialogOpen(false)}
        title={t('gameCatalog.questions.deleteCategoryTitle')}
        description={t('gameCatalog.errors.categoryNotEmpty')}
        actions={
          <AppButton tone="primary" onClick={() => setIsCategoryBlockedDialogOpen(false)}>
            {t('common.actions.cancel')}
          </AppButton>
        }
      />
    </PageShell>
  )
}
