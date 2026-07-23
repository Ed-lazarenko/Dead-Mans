import { Alert, Box, Chip, Stack, Typography } from '@mui/material'
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
import { modifierCategoryCodes, modifierRoundSummaryTypes } from '../game-modifiers/index.ts'
import { deriveModifierRoundSummaryMeta } from '../game-modifiers/model/modifier-round-summary.ts'
import { useCatalogFeedback } from './use-catalog-feedback.ts'
import { ModifierFormDialog } from './ui/ModifierFormDialog.tsx'
import { useCatalogModifiers } from './use-catalog-modifiers.ts'

export function CatalogModifiersPage() {
  const { t } = useTranslation()
  const categoryLabels = {
    preparation: t('gameCatalog.modifiers.categories.preparation'),
    round: t('gameCatalog.modifiers.categories.round'),
    result: t('gameCatalog.modifiers.categories.result'),
  } as const
  const roundSummaryLabels = {
    passive: t('gameCatalog.modifiers.roundSummaryType.passive'),
    auto_result: t('gameCatalog.modifiers.roundSummaryType.auto_result'),
    toggle_bonus: t('gameCatalog.modifiers.roundSummaryType.toggle_bonus'),
    counted_bonus: t('gameCatalog.modifiers.roundSummaryType.counted_bonus'),
    kill_multiplier: t('gameCatalog.modifiers.roundSummaryType.kill_multiplier'),
    manual_points: t('gameCatalog.modifiers.roundSummaryType.manual_points'),
  } as const
  const {
    search,
    setSearch,
    selectedCategory,
    setSelectedCategory,
    categoryCounts,
    selectedRoundSummaryType,
    setSelectedRoundSummaryType,
    roundSummaryCounts,
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
  const { listError, clearListError, resetFeedback, showResolvedError } = useCatalogFeedback(t)

  const hasCatalogItems = (catalogQuery.data?.length ?? 0) > 0
  const isSearchActive = search.trim().length > 0
  const isCategoryActive = selectedCategory !== null
  const isRoundSummaryActive = selectedRoundSummaryType !== null
  const isListEmpty = filteredModifiers.length === 0
  const emptyMessage =
    (isSearchActive || isCategoryActive || isRoundSummaryActive) && hasCatalogItems
      ? isCategoryActive && !isSearchActive
        ? t('gameCatalog.modifiers.emptyCategory')
        : t('gameCatalog.modifiers.emptySearch')
      : t('gameCatalog.modifiers.empty')

  const handleConfirmDelete = async () => {
    resetFeedback()
    try {
      await confirmDelete()
    } catch (error) {
      cancelDelete()
      showResolvedError(error)
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
        <Alert severity="error" sx={{ mb: 2 }} onClose={clearListError}>
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
              description={
                selectedCategory
                  ? `${t('gameCatalog.modifiers.description')} ${categoryLabels[selectedCategory]}.`
                  : t('gameCatalog.modifiers.description')
              }
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
                {filteredModifiers.map((modifier) => {
                  const roundSummaryMeta = deriveModifierRoundSummaryMeta(modifier)

                  return (
                  <Box
                    key={modifier.id}
                    sx={(theme) => ({
                      border: `1px solid ${theme.palette.divider}`,
                      borderRadius: 1,
                      p: 1.25,
                      display: 'flex',
                      gap: 1,
                      alignItems: 'flex-start',
                      justifyContent: 'space-between',
                    })}
                  >
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {modifier.iconEmoji ? `${modifier.iconEmoji} ` : ''}
                        {modifier.name}
                      </Typography>
                      <Stack
                        direction="row"
                        spacing={0.75}
                        sx={{ mt: 1, flexWrap: 'wrap', rowGap: 0.75 }}
                      >
                        <Chip
                          color="warning"
                          label={`${t('gameCatalog.modifiers.fields.activationCost')}: ${modifier.activationCost}`}
                        />
                        <Chip
                          color="info"
                          label={`${t('gameCatalog.modifiers.fields.category')}: ${categoryLabels[modifier.category]}`}
                        />
                        <Chip
                          color="success"
                          label={`${t('gameCatalog.modifiers.fields.activationLimitCount')}: ${
                            modifier.defaultLimitPerGame == null
                              ? t('gameCatalog.modifiers.preview.unlimited')
                              : modifier.defaultLimitPerGame
                          }`}
                        />
                        <Chip
                          label={t(`gameCatalog.modifiers.mechanics.${modifier.mechanicType}`)}
                        />
                        <Chip
                          color={roundSummaryMeta.includeInRoundSummary ? 'secondary' : 'default'}
                          label={t(
                            `gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`,
                          )}
                        />
                        {modifier.requiresHostControl ? (
                          <Chip color="error" label={t('gameCatalog.modifiers.hostControlBadge')} />
                        ) : null}
                      </Stack>
                      <Typography
                        variant="body2"
                        color="text.primary"
                        sx={{ mt: 1, display: 'block', whiteSpace: 'pre-line' }}
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
                  )
                })}
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

              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('gameCatalog.modifiers.categoriesTitle')}
                </Typography>
                <Stack spacing={1}>
                  <Box
                    onClick={() => setSelectedCategory(null)}
                    sx={{
                      border: (theme) => `1px solid ${theme.palette.divider}`,
                      borderColor: selectedCategory === null ? 'primary.main' : 'divider',
                      bgcolor: selectedCategory === null ? 'action.selected' : 'transparent',
                      borderRadius: 1,
                      p: 1.25,
                      cursor: 'pointer',
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>
                      {t('gameCatalog.modifiers.allCategories')}
                    </Typography>
                  </Box>

                  {modifierCategoryCodes.map((category) => (
                    <Box
                      key={category}
                      onClick={() => setSelectedCategory(category)}
                      sx={{
                        border: (theme) => `1px solid ${theme.palette.divider}`,
                        borderColor: selectedCategory === category ? 'primary.main' : 'divider',
                        bgcolor: selectedCategory === category ? 'action.selected' : 'transparent',
                        borderRadius: 1,
                        p: 1.25,
                        cursor: 'pointer',
                      }}
                    >
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {categoryLabels[category]}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {t('gameCatalog.modifiers.categoryCount', {
                          count: categoryCounts[category],
                        })}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              </Box>

              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  {t('gameCatalog.modifiers.roundSummaryTitle')}
                </Typography>
                <Stack spacing={1}>
                  <Box
                    onClick={() => setSelectedRoundSummaryType(null)}
                    sx={{
                      border: (theme) => `1px solid ${theme.palette.divider}`,
                      borderColor: selectedRoundSummaryType === null ? 'primary.main' : 'divider',
                      bgcolor: selectedRoundSummaryType === null ? 'action.selected' : 'transparent',
                      borderRadius: 1,
                      p: 1.25,
                      cursor: 'pointer',
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 700 }}>
                      {t('gameCatalog.modifiers.allRoundSummaries')}
                    </Typography>
                  </Box>

                  {modifierRoundSummaryTypes.map((roundSummaryType) => (
                    <Box
                      key={roundSummaryType}
                      onClick={() => setSelectedRoundSummaryType(roundSummaryType)}
                      sx={{
                        border: (theme) => `1px solid ${theme.palette.divider}`,
                        borderColor:
                          selectedRoundSummaryType === roundSummaryType ? 'primary.main' : 'divider',
                        bgcolor:
                          selectedRoundSummaryType === roundSummaryType
                            ? 'action.selected'
                            : 'transparent',
                        borderRadius: 1,
                        p: 1.25,
                        cursor: 'pointer',
                      }}
                    >
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {roundSummaryLabels[roundSummaryType]}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {t('gameCatalog.modifiers.roundSummaryCount', {
                          count: roundSummaryCounts[roundSummaryType],
                        })}
                      </Typography>
                    </Box>
                  ))}
                </Stack>
              </Box>
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
