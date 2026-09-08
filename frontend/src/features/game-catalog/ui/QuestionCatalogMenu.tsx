import { Alert, Box, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { GameQuestionCategoryItem } from '../../../shared/api/contracts/index.ts'
import { AppButton, AsyncSection, SectionCard, SectionHeader } from '../../../shared/ui/index.ts'
import { CollapsibleToolGroup } from './CollapsibleToolGroup.tsx'

interface QuestionCatalogMenuProps {
  categories: readonly GameQuestionCategoryItem[]
  selectedCategoryId: string | null
  canAddQuestion: boolean
  canRenameCategory: boolean
  canDeleteCategory: boolean
  isCategoriesLoading: boolean
  isCategoriesError: boolean
  isImportingQuestions: boolean
  isDownloadingTemplate: boolean
  onSelectCategory: (categoryId: string | null) => void
  onCreateQuestion: () => void
  onDownloadTemplate: () => void
  onUploadQuestions: () => void
  onCreateCategory: () => void
  onRenameCategory: () => void
  onDeleteCategory: () => void
}

export function QuestionCatalogMenu({
  categories,
  selectedCategoryId,
  canAddQuestion,
  canRenameCategory,
  canDeleteCategory,
  isCategoriesLoading,
  isCategoriesError,
  isImportingQuestions,
  isDownloadingTemplate,
  onSelectCategory,
  onCreateQuestion,
  onDownloadTemplate,
  onUploadQuestions,
  onCreateCategory,
  onRenameCategory,
  onDeleteCategory,
}: QuestionCatalogMenuProps) {
  const { t } = useTranslation()

  return (
    <SectionCard sx={{ height: '100%' }}>
      <SectionHeader
        title={t('gameCatalog.questions.menuTitle')}
        description={t('gameCatalog.questions.menuDescription')}
      />

      <Stack spacing={1.5} sx={{ mt: 1.5 }}>
        <Stack spacing={1}>
          <AppButton fullWidth onClick={onCreateQuestion} disabled={!canAddQuestion}>
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
              onClick={onDownloadTemplate}
              disabled={isDownloadingTemplate}
            >
              {t('gameCatalog.questions.downloadTemplate')}
            </AppButton>
            <AppButton
              fullWidth
              tone="secondary"
              onClick={onUploadQuestions}
              disabled={isImportingQuestions}
            >
              {t('gameCatalog.questions.importJson')}
            </AppButton>
          </CollapsibleToolGroup>

          <CollapsibleToolGroup
            panelId="catalog-question-category-panel"
            title={t('common.entities.categories')}
            description={t('gameCatalog.questions.categoryGroupDescription')}
            expandLabel={t('gameCatalog.questions.categoryGroupExpand')}
            collapseLabel={t('gameCatalog.questions.categoryGroupCollapse')}
          >
            <AppButton fullWidth tone="secondary" onClick={onCreateCategory}>
              {t('gameCatalog.questions.addCategory')}
            </AppButton>
            <AppButton
              fullWidth
              tone="secondary"
              onClick={onRenameCategory}
              disabled={!canRenameCategory}
            >
              {t('gameCatalog.questions.renameCategory')}
            </AppButton>
            <AppButton
              fullWidth
              tone="dangerSecondary"
              onClick={onDeleteCategory}
              disabled={!canDeleteCategory}
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
            {t('common.entities.categories')}
          </Typography>
          <Stack spacing={1}>
            <CategoryOption
              isSelected={selectedCategoryId === null}
              onClick={() => onSelectCategory(null)}
              name={t('common.filters.allCategories')}
            />

            <AsyncSection
              isLoading={isCategoriesLoading}
              isError={isCategoriesError}
              isEmpty={categories.length === 0}
              loadingMessage={t('gameCatalog.questions.loadingCategories')}
              errorMessage={t('gameCatalog.questions.errorCategories')}
              emptyMessage={t('gameCatalog.questions.emptyCategories')}
            >
              <Stack spacing={1}>
                {categories.map((category) => (
                  <CategoryOption
                    key={category.id}
                    isSelected={selectedCategoryId === category.id}
                    onClick={() => onSelectCategory(category.id)}
                    name={category.name}
                    count={category.questionCount}
                  />
                ))}
              </Stack>
            </AsyncSection>
          </Stack>
        </Box>
      </Stack>
    </SectionCard>
  )
}

function CategoryOption({
  isSelected,
  onClick,
  name,
  count,
}: {
  isSelected: boolean
  onClick: () => void
  name: string
  count?: number
}) {
  const { t } = useTranslation()

  return (
    <Box
      onClick={onClick}
      sx={{
        border: (theme) => `1px solid ${theme.palette.divider}`,
        borderColor: isSelected ? 'primary.main' : 'divider',
        bgcolor: isSelected ? 'action.selected' : 'transparent',
        borderRadius: 1,
        p: 1.25,
        cursor: 'pointer',
      }}
    >
      <Typography variant="body2" sx={{ fontWeight: 700 }}>
        {name}
      </Typography>
      {count === undefined ? null : (
        <Typography variant="caption" color="text.secondary">
          {t('gameCatalog.questions.categoryCount', { count })}
        </Typography>
      )}
    </Box>
  )
}
