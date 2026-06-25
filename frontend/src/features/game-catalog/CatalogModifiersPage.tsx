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
import { ModifierFormDialog } from './ui/ModifierFormDialog.tsx'
import { useCatalogModifiers } from './use-catalog-modifiers.ts'

export function CatalogModifiersPage() {
  const { t } = useTranslation()
  const {
    search,
    setSearch,
    catalogQuery,
    filteredModifiers,
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

  const hasCatalogItems = (catalogQuery.data?.length ?? 0) > 0
  const isSearchActive = search.trim().length > 0
  const isListEmpty = filteredModifiers.length === 0
  const emptyMessage =
    isSearchActive && hasCatalogItems
      ? t('gameCatalog.modifiers.emptySearch')
      : t('gameCatalog.modifiers.empty')

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
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
      }}
    >
      {listError ? (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setListError(null)}>
          {listError}
        </Alert>
      ) : null}

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
              title={t('gameCatalog.modifiers.title')}
              description={t('gameCatalog.modifiers.description')}
            />

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mt: 1.5 }}>
              <FormTextField
                value={search}
                label={t('gameCatalog.modifiers.searchLabel')}
                onChange={(event) => setSearch(event.target.value)}
              />
            </Stack>

            <AsyncSection
              isLoading={catalogQuery.isLoading}
              isError={catalogQuery.isError}
              isEmpty={isListEmpty}
              loadingMessage={t('gameCatalog.modifiers.loading')}
              errorMessage={t('gameCatalog.modifiers.error')}
              emptyMessage={emptyMessage}
            >
              <Stack spacing={1} sx={{ mt: 1.5 }}>
                {filteredModifiers.map((modifier) => (
                  <Box
                    key={modifier.id}
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
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block' }}
                      >
                        {t('gameCatalog.modifiers.meta', {
                          cost: modifier.activationCost,
                        })}
                      </Typography>
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block' }}
                      >
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
        </Box>

        <Box sx={{ minWidth: 0 }}>
          <SectionCard sx={{ height: '100%' }}>
            <SectionHeader
              title={t('gameCatalog.modifiers.menuTitle')}
              description={t('gameCatalog.modifiers.menuDescription')}
            />

            <Stack spacing={1.5} sx={{ mt: 1.5 }}>
              <AppButton fullWidth onClick={openCreate}>
                {t('gameCatalog.modifiers.add')}
              </AppButton>
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                {t('gameCatalog.modifiers.menuHint')}
              </Typography>
            </Stack>
          </SectionCard>
        </Box>
      </Box>

      <ModifierFormDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'create'}
        initial={dialog?.mode === 'edit' ? dialog.modifier : undefined}
        modifiers={catalogQuery.data ?? []}
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
