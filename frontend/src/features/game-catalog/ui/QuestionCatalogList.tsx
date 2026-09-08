import { Box, Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type {
  GameQuestionCatalogItem,
  GameQuestionCategoryItem,
} from '../../../shared/api/contracts/index.ts'
import {
  AppButton,
  AsyncSection,
  FormTextField,
  SectionCard,
  SectionHeader,
} from '../../../shared/ui/index.ts'

interface QuestionCatalogListProps {
  search: string
  selectedCategory: GameQuestionCategoryItem | null
  questions: readonly GameQuestionCatalogItem[]
  isLoading: boolean
  isError: boolean
  onSearchChange: (search: string) => void
  onEdit: (question: GameQuestionCatalogItem) => void
  onDelete: (question: GameQuestionCatalogItem) => void
}

export function QuestionCatalogList({
  search,
  selectedCategory,
  questions,
  isLoading,
  isError,
  onSearchChange,
  onEdit,
  onDelete,
}: QuestionCatalogListProps) {
  const { t } = useTranslation()

  return (
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
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </Stack>

      <AsyncSection
        isLoading={isLoading}
        isError={isError}
        isEmpty={questions.length === 0}
        loadingMessage={t('gameCatalog.questions.loading')}
        errorMessage={t('gameCatalog.questions.error')}
        emptyMessage={t('gameCatalog.questions.empty')}
      >
        <Stack spacing={1} sx={{ mt: 1.5 }}>
          {questions.map((question) => (
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
                    label={t('gameCatalog.questions.rewardMeta', { reward: question.reward })}
                  />
                  <Chip
                    color="success"
                    label={t('gameCatalog.questions.answerMeta', { answer: question.answer })}
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
                <AppButton size="small" tone="secondary" onClick={() => onEdit(question)}>
                  {t('gameCatalog.actions.edit')}
                </AppButton>
                <AppButton size="small" tone="danger" onClick={() => onDelete(question)}>
                  {t('gameCatalog.actions.delete')}
                </AppButton>
              </Stack>
            </Box>
          ))}
        </Stack>
      </AsyncSection>
    </SectionCard>
  )
}
