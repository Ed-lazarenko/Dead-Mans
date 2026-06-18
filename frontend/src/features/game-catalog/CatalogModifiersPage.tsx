import { Alert, Box, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  AppButton,
  AsyncSection,
  ConfirmDialog,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from './model/catalog-error.ts'
import { ModifierFormDialog } from './ui/ModifierFormDialog.tsx'
import { useCatalogModifiers } from './use-catalog-modifiers.ts'

export function CatalogModifiersPage() {
  const { t } = useTranslation()
  const {
    catalogQuery,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitModifier,
    isSaving,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting,
  } = useCatalogModifiers()
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
          title={t('gameCatalog.modifiers.title')}
          description={t('gameCatalog.modifiers.description')}
          actions={
            <AppButton tone="primary" onClick={openCreate}>
              {t('gameCatalog.modifiers.add')}
            </AppButton>
          }
        />

        {listError ? (
          <Alert severity="error" sx={{ mt: 2 }} onClose={() => setListError(null)}>
            {listError}
          </Alert>
        ) : null}

        <AsyncSection
          isLoading={catalogQuery.isLoading}
          isError={catalogQuery.isError}
          isEmpty={(catalogQuery.data?.length ?? 0) === 0}
          loadingMessage={t('gameCatalog.modifiers.loading')}
          errorMessage={t('gameCatalog.modifiers.error')}
          emptyMessage={t('gameCatalog.modifiers.empty')}
        >
          <Stack spacing={1} sx={{ mt: 1.5 }}>
            {(catalogQuery.data ?? []).map((modifier) => (
              <Box
                key={modifier.code}
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
                    {modifier.iconEmoji ? `${modifier.iconEmoji} ` : ''}
                    {modifier.name}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    {t('gameCatalog.modifiers.meta', {
                      code: modifier.code,
                      kind: modifier.kind,
                      category: modifier.category,
                      cost: modifier.activationCost,
                    })}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    {modifier.description}
                  </Typography>
                </Box>
                <Stack direction="row" spacing={1} sx={{ flexShrink: 0 }}>
                  <AppButton size="small" tone="secondary" onClick={() => openEdit(modifier)}>
                    {t('gameCatalog.actions.edit')}
                  </AppButton>
                  <AppButton size="small" tone="danger" onClick={() => requestDelete(modifier)}>
                    {t('gameCatalog.actions.delete')}
                  </AppButton>
                </Stack>
              </Box>
            ))}
          </Stack>
        </AsyncSection>
      </SectionCard>

      <ModifierFormDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'create'}
        initial={dialog?.mode === 'edit' ? dialog.modifier : undefined}
        isBusy={isSaving}
        onClose={closeDialog}
        onSubmit={submitModifier}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        title={t('gameCatalog.modifiers.deleteTitle')}
        description={t('gameCatalog.modifiers.deleteConfirm', { name: deleteTarget?.name ?? '' })}
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
