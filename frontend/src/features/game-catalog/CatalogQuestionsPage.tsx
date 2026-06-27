import { Alert, Box, Chip, Stack, Typography } from '@mui/material'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { ImportGameQuestionSkippedItem } from '../../shared/api/contracts/index.ts'
import {
  AppDialog,
  AppButton,
  AsyncSection,
  ConfirmDialog,
  FormTextField,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from './model/catalog-error.ts'
import {
  downloadQuestionImportFailureReport,
  formatSkippedQuestionWarning,
} from './model/question-import-report.ts'
import { CollapsibleToolGroup } from './ui/CollapsibleToolGroup.tsx'
import { QuestionCategoryDialog } from './ui/QuestionCategoryDialog.tsx'
import { QuestionFormDialog } from './ui/QuestionFormDialog.tsx'
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
  const [listError, setListError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [importReport, setImportReport] = useState<ImportReportState | null>(null)
  const [isCategoryBlockedDialogOpen, setIsCategoryBlockedDialogOpen] = useState(false)
  const importInputRef = useRef<HTMLInputElement | null>(null)

  const handleConfirmDelete = async () => {
    setListError(null)
    setSuccessMessage(null)
    setImportReport(null)
    try {
      await confirmDelete()
    } catch (error) {
      cancelDelete()
      setListError(resolveCatalogErrorMessage(error, t))
    }
  }

  const handleConfirmDeleteCategory = async () => {
    setListError(null)
    setSuccessMessage(null)
    setImportReport(null)
    try {
      await confirmDeleteCategory()
    } catch (error) {
      cancelDeleteCategory()
      setListError(resolveCatalogErrorMessage(error, t))
    }
  }

  const categories = categoriesQuery.data ?? []
  const canAddQuestion = categories.length > 0
  const isSelectedCategoryProtected = selectedCategory?.isProtected ?? false

  const handleRenameCategoryClick = () => {
    if (!selectedCategory) {
      return
    }

    openEditCategory(selectedCategory)
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
    setListError(null)
    setSuccessMessage(null)
    setImportReport(null)
    try {
      const templateLocale =
        (i18n.language ?? '').split('-')[0]?.toLowerCase() === 'ru' ? 'ru' : 'en'
      const content = await downloadTemplate(templateLocale)
      const blob = new Blob([content], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = 'question-import-template.jsonc'
      document.body.append(anchor)
      anchor.click()
      anchor.remove()
      URL.revokeObjectURL(url)
    } catch (error) {
      setListError(resolveCatalogErrorMessage(error, t))
    }
  }

  const clearImportReport = () => {
    setImportReport(null)
  }

  const handleUploadButtonClick = () => {
    importInputRef.current?.click()
  }

  const handleImportFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) {
      return
    }

    setListError(null)
    setSuccessMessage(null)
    setImportReport(null)
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
      setListError(errorMessage)
      setImportReport({
        fileName: file.name,
        importedCount: 0,
        skippedQuestions: [],
        errorMessage,
      })
    }
  }

  return (
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
      }}
    >
      {listError ? (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setListError(null)}>
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
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccessMessage(null)}>
          {successMessage}
        </Alert>
      ) : null}

      {importReport && importReport.skippedQuestions.length > 0 ? (
        <Alert severity="warning" sx={{ mb: 2 }} onClose={clearImportReport}>
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
                  {formatSkippedQuestionWarning(warning)}
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
          gridTemplateColumns: {
            xs: '1fr',
            lg: 'minmax(0, 1fr) minmax(320px, 360px)',
          },
        }}
      >
        <Box sx={{ minWidth: 0 }}>
          <SectionCard sx={{ height: '100%' }}>
            <SectionHeader
              title={t('gameCatalog.questions.title')}
              description={
                selectedCategory
                  ? `${t('gameCatalog.questions.description')} ${selectedCategory.name}.`
                  : t('gameCatalog.questions.description')
              }
            />

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mt: 1.5 }}>
              <FormTextField
                value={search}
                label={t('gameCatalog.questions.searchLabel')}
                onChange={(event) => setSearch(event.target.value)}
              />
            </Stack>

            <AsyncSection
              isLoading={catalogQuery.isLoading}
              isError={catalogQuery.isError}
              isEmpty={(catalogQuery.data?.length ?? 0) === 0}
              loadingMessage={t('gameCatalog.questions.loading')}
              errorMessage={t('gameCatalog.questions.error')}
              emptyMessage={t('gameCatalog.questions.empty')}
            >
              <Stack spacing={1} sx={{ mt: 1.5 }}>
                {(catalogQuery.data ?? []).map((question) => (
                  <Box
                    key={question.questionId}
                    sx={{
                      border: (theme) => `1px solid ${theme.palette.divider}`,
                      borderRadius: 1,
                      p: 1.25,
                      display: 'flex',
                      gap: 1,
                      alignItems: 'flex-start',
                      justifyContent: 'space-between',
                    }}
                  >
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {question.text}
                      </Typography>
                      <Stack
                        direction="row"
                        spacing={0.75}
                        sx={{ mt: 1, flexWrap: 'wrap', rowGap: 0.75 }}
                      >
                        <Chip
                          color="info"
                          label={t('gameCatalog.questions.categoryMeta', {
                            category: question.categoryName,
                          })}
                        />
                        <Chip
                          color="warning"
                          label={t('gameCatalog.questions.rewardMeta', {
                            reward: question.reward,
                          })}
                        />
                        <Chip
                          color="success"
                          label={t('gameCatalog.questions.answerMeta', {
                            answer: question.answer,
                          })}
                        />
                        <Chip
                          label={t('gameCatalog.questions.askedMeta', {
                            asked: question.askedTotalCount,
                          })}
                        />
                        {question.isEnabled ? null : (
                          <Chip color="error" label={t('gameCatalog.questions.disabledBadge')} />
                        )}
                      </Stack>
                    </Box>
                    <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
                      <AppButton size="small" tone="secondary" onClick={() => openEdit(question)}>
                        {t('gameCatalog.actions.edit')}
                      </AppButton>
                      <AppButton size="small" tone="danger" onClick={() => requestDelete(question)}>
                        {t('gameCatalog.actions.delete')}
                      </AppButton>
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </AsyncSection>
          </SectionCard>
        </Box>

        <Box sx={{ minWidth: 0 }}>
          <SectionCard sx={{ height: '100%' }}>
            <SectionHeader
              title={t('gameCatalog.questions.menuTitle')}
              description={t('gameCatalog.questions.menuDescription')}
            />

            <Stack spacing={1.5} sx={{ mt: 1.5 }}>
              <Stack spacing={1}>
                <AppButton fullWidth onClick={openCreate} disabled={!canAddQuestion}>
                  {t('gameCatalog.questions.add')}
                </AppButton>

                <CollapsibleToolGroup
                  panelId="catalog-question-import-panel"
                  title={t('gameCatalog.questions.importGroupTitle')}
                  description={t('gameCatalog.questions.importGroupDescription')}
                  expandLabel={t('gameCatalog.questions.importGroupExpand')}
                  collapseLabel={t('gameCatalog.questions.importGroupCollapse')}
                >
                  <AppButton
                    fullWidth
                    tone="secondary"
                    onClick={handleDownloadTemplate}
                    disabled={isDownloadingTemplate}
                  >
                    {t('gameCatalog.questions.downloadTemplate')}
                  </AppButton>
                  <AppButton
                    fullWidth
                    tone="secondary"
                    onClick={handleUploadButtonClick}
                    disabled={isImportingQuestions}
                  >
                    {t('gameCatalog.questions.importJson')}
                  </AppButton>
                </CollapsibleToolGroup>

                <CollapsibleToolGroup
                  panelId="catalog-question-category-panel"
                  title={t('gameCatalog.questions.categoryGroupTitle')}
                  description={t('gameCatalog.questions.categoryGroupDescription')}
                  expandLabel={t('gameCatalog.questions.categoryGroupExpand')}
                  collapseLabel={t('gameCatalog.questions.categoryGroupCollapse')}
                >
                  <AppButton fullWidth tone="secondary" onClick={openCreateCategory}>
                    {t('gameCatalog.questions.addCategory')}
                  </AppButton>
                  <AppButton
                    fullWidth
                    tone="secondary"
                    onClick={handleRenameCategoryClick}
                    disabled={
                      selectedCategory === null || isSelectedCategoryProtected || isSavingCategory
                    }
                  >
                    {t('gameCatalog.questions.renameCategory')}
                  </AppButton>
                  <AppButton
                    fullWidth
                    tone="dangerSecondary"
                    onClick={handleDeleteCategoryClick}
                    disabled={
                      selectedCategory === null || isSelectedCategoryProtected || isDeletingCategory
                    }
                  >
                    {t('gameCatalog.questions.deleteCategory')}
                  </AppButton>
                  {!canAddQuestion ? (
                    <Alert severity="warning">{t('gameCatalog.questions.noCategories')}</Alert>
                  ) : null}
                </CollapsibleToolGroup>
              </Stack>

              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('gameCatalog.questions.categoriesTitle')}
                </Typography>
                <Stack spacing={1}>
                  <Box
                    onClick={() => setSelectedCategoryId(null)}
                    sx={{
                      border: (theme) => `1px solid ${theme.palette.divider}`,
                      borderColor: selectedCategoryId === null ? 'primary.main' : 'divider',
                      bgcolor: selectedCategoryId === null ? 'action.selected' : 'transparent',
                      borderRadius: 1,
                      p: 1.25,
                      cursor: 'pointer',
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>
                      {t('gameCatalog.questions.allCategories')}
                    </Typography>
                  </Box>

                  <AsyncSection
                    isLoading={categoriesQuery.isLoading}
                    isError={categoriesQuery.isError}
                    isEmpty={categories.length === 0}
                    loadingMessage={t('gameCatalog.questions.loadingCategories')}
                    errorMessage={t('gameCatalog.questions.errorCategories')}
                    emptyMessage={t('gameCatalog.questions.emptyCategories')}
                  >
                    <Stack spacing={1}>
                      {categories.map((category) => (
                        <Box
                          key={category.id}
                          onClick={() => setSelectedCategoryId(category.id)}
                          sx={{
                            border: (theme) => `1px solid ${theme.palette.divider}`,
                            borderColor:
                              selectedCategoryId === category.id ? 'primary.main' : 'divider',
                            bgcolor:
                              selectedCategoryId === category.id
                                ? 'action.selected'
                                : 'transparent',
                            borderRadius: 1,
                            p: 1.25,
                            cursor: 'pointer',
                          }}
                        >
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {category.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {t('gameCatalog.questions.categoryCount', {
                              count: category.questionCount,
                            })}
                          </Typography>
                        </Box>
                      ))}
                    </Stack>
                  </AsyncSection>
                </Stack>
              </Box>
            </Stack>
          </SectionCard>
        </Box>
      </Box>

      <QuestionFormDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'create'}
        initial={dialog?.mode === 'edit' ? dialog.question : undefined}
        categories={categoriesQuery.data ?? []}
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
        cancelLabel={t('gameCatalog.actions.cancel')}
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
        cancelLabel={t('gameCatalog.actions.cancel')}
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
            {t('gameCatalog.actions.cancel')}
          </AppButton>
        }
      />
    </PageShell>
  )
}
