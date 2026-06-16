import { Box, Checkbox, Chip, FormControlLabel, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import {
  AppButton,
  AsyncSection,
  FormTextField,
  SectionCard,
  SectionHeader,
} from '../../../shared/ui/index.ts'
import { useGameSetupQuestionsCatalog } from '../use-game-setup-questions-catalog.ts'

function toCategoryTitle(category: string) {
  return category
    .split('_')
    .filter((part) => part.length > 0)
    .map((part) => `${part[0]!.toUpperCase()}${part.slice(1)}`)
    .join(' ')
}

export function GameSetupQuestionsSection() {
  const { t } = useTranslation()
  const {
    search,
    setSearch,
    activeCategory,
    setActiveCategory,
    catalogQuery,
    toggleQuestionMutation,
    toggleCategoryMutation,
    categories,
    filteredQuestions,
  } = useGameSetupQuestionsCatalog()

  return (
    <SectionCard>
      <SectionHeader
        title={t('gameSetup.questions.title')}
        description={t('gameSetup.questions.description')}
      />

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mt: 1.5 }}>
        <FormTextField
          value={search}
          label={t('gameSetup.questions.searchLabel')}
          onChange={(event) => setSearch(event.target.value)}
        />
      </Stack>

      <Stack direction="row" spacing={1} sx={{ mt: 1.5, flexWrap: 'wrap', rowGap: 1 }}>
        <Chip
          label={t('gameSetup.questions.categoryAll')}
          color={activeCategory === null ? 'primary' : 'default'}
          onClick={() => setActiveCategory(null)}
        />
        {categories.map((category) => (
          <Chip
            key={category}
            label={toCategoryTitle(category)}
            color={activeCategory === category ? 'primary' : 'default'}
            onClick={() => setActiveCategory(category)}
          />
        ))}
      </Stack>

      {activeCategory ? (
        <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
          <AppButton
            size="small"
            tone="secondary"
            disabled={toggleCategoryMutation.isPending}
            onClick={() =>
              toggleCategoryMutation.mutate({ category: activeCategory, isEnabled: true })
            }
          >
            {t('gameSetup.questions.enableCategory')}
          </AppButton>
          <AppButton
            size="small"
            tone="warningGhost"
            disabled={toggleCategoryMutation.isPending}
            onClick={() =>
              toggleCategoryMutation.mutate({ category: activeCategory, isEnabled: false })
            }
          >
            {t('gameSetup.questions.disableCategory')}
          </AppButton>
        </Stack>
      ) : null}

      <AsyncSection
        isLoading={catalogQuery.isLoading}
        isError={catalogQuery.isError}
        isEmpty={filteredQuestions.length === 0}
        loadingMessage={t('gameSetup.questions.loading')}
        errorMessage={t('gameSetup.questions.error')}
        emptyMessage={t('gameSetup.questions.empty')}
      >
        <Stack spacing={0.5} sx={{ mt: 1.5 }}>
          {filteredQuestions.map((question) => (
            <Box
              key={question.questionId}
              sx={{
                border: (theme) => `1px solid ${theme.palette.divider}`,
                borderRadius: 1,
                p: 1,
              }}
            >
              <FormControlLabel
                control={
                  <Checkbox
                    checked={question.isEnabled}
                    onChange={(event) =>
                      toggleQuestionMutation.mutate({
                        questionId: question.questionId,
                        isEnabled: event.target.checked,
                      })
                    }
                    disabled={toggleQuestionMutation.isPending}
                  />
                }
                label={
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    {question.text}
                  </Typography>
                }
                sx={{ alignItems: 'flex-start', m: 0 }}
              />
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ display: 'block', ml: 4.5 }}
              >
                {t('gameSetup.questions.meta', {
                  category: question.category,
                  reward: question.reward,
                  asked: question.askedTotalCount,
                  correct: question.correctTotalCount,
                })}
              </Typography>
            </Box>
          ))}
        </Stack>
      </AsyncSection>
    </SectionCard>
  )
}
