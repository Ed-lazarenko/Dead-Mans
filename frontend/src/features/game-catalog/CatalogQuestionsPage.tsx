import { Alert, Box, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  AppButton,
  AsyncSection,
  ConfirmDialog,
  FormTextField,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from './model/catalog-error.ts'
import { QuestionCategoryDialog } from './ui/QuestionCategoryDialog.tsx'
import { QuestionFormDialog } from './ui/QuestionFormDialog.tsx'
import { useCatalogQuestions } from './use-catalog-questions.ts'

export function CatalogQuestionsPage() {
  const { t } = useTranslation()
  const {
    search,
    setSearch,
    catalogQuery,
    categoriesQuery,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitQuestion,
    isCategoryDialogOpen,
    openCreateCategory,
    closeCreateCategory,
    submitCategory,
    isSaving,
    isSavingCategory,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting,
  } = useCatalogQuestions()
  const [listError, setListError] = useState<string | null>(null)

  const handleConfirmDelete = async () => {
    setListError(null)
    try {
      await confirmDelete()
    } catch (error) {
      cancelDelete()
      setListError(resolveCatalogErrorMessage(error, t))
    }
  }

  return (
    <PageShell>
      <SectionCard>
        <SectionHeader
          title={t('gameCatalog.questions.title')}
          description={t('gameCatalog.questions.description')}
          actions={
            <Stack direction="row" spacing={1}>
              <AppButton tone="ghost" onClick={openCreateCategory}>
                {t('gameCatalog.questions.addCategory')}
              </AppButton>
              <AppButton tone="primary" onClick={openCreate}>
                {t('gameCatalog.questions.add')}
              </AppButton>
            </Stack>
          }
        />

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mt: 1.5 }}>
          <FormTextField
            value={search}
            label={t('gameCatalog.questions.searchLabel')}
            onChange={(event) => setSearch(event.target.value)}
          />
        </Stack>

        {listError ? (
          <Alert severity="error" sx={{ mt: 2 }} onClose={() => setListError(null)}>
            {listError}
          </Alert>
        ) : null}

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
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    {t('gameCatalog.questions.meta', {
                      category: question.categoryName,
                      reward: question.reward,
                      answer: question.answer,
                    })}
                    {question.isEnabled ? '' : ` · ${t('gameCatalog.questions.disabledBadge')}`}
                  </Typography>
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
        open={isCategoryDialogOpen}
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
    </PageShell>
  )
}
