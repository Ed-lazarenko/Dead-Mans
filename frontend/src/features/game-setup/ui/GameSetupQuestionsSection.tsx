import { Box, Checkbox, Chip, FormControlLabel, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  AppButton,
  AsyncSection,
  FormTextField,
  SectionCard,
  SectionHeader,
} from '../../../shared/ui/index.ts'
import type { GameSetupDraftState } from '../model/game-setup-draft.ts'
import { useGameSetupQuestionsCatalog } from '../use-game-setup-questions-catalog.ts'

interface GameSetupQuestionsSectionProps {
  draft: GameSetupDraftState
  onToggle: (questionId: string, enabled: boolean) => void
  onBulkSetEnabled: (questionIds: readonly string[], enabled: boolean) => void
  actions?: ReactNode
}

export function GameSetupQuestionsSection({
  draft,
  onToggle,
  onBulkSetEnabled,
  actions,
}: GameSetupQuestionsSectionProps) {
  const { t } = useTranslation()
  const {
    search,
    setSearch,
    activeCategory,
    setActiveCategory,
    catalogQuery,
    categories,
    filteredQuestions,
  } = useGameSetupQuestionsCatalog()

  const enabledQuestionIds = new Set(draft.enabledQuestionIds)
  const visibleIds = filteredQuestions.map((question) => question.questionId)

  return (
    <SectionCard>
      <SectionHeader
        title={t('gameSetup.questions.title')}
        description={t('gameSetup.questions.enabledDescription')}
        actions={actions}
      />

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>
        {t('gameSetup.questions.enabledCount', { count: draft.enabledQuestionIds.length })}
      </Typography>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ mt: 1.5 }}>
        <FormTextField
          value={search}
          label={t('gameSetup.questions.searchLabel')}
          onChange={(event) => setSearch(event.target.value)}
        />
      </Stack>

      <Stack direction="row" spacing={1} sx={{ mt: 1.5, flexWrap: 'wrap', rowGap: 1 }}>
        <Chip
          label={t('common.filters.allCategories')}
          color={activeCategory === null ? 'primary' : 'default'}
          onClick={() => setActiveCategory(null)}
        />
        {categories.map((category) => (
          <Chip
            key={category}
            label={category}
            color={activeCategory === category ? 'primary' : 'default'}
            onClick={() => setActiveCategory(category)}
          />
        ))}
      </Stack>

      <Stack direction="row" spacing={1} sx={{ mt: 1.5 }}>
        <AppButton size="small" tone="secondary" onClick={() => onBulkSetEnabled(visibleIds, true)}>
          {t('gameSetup.questions.enableVisible')}
        </AppButton>
        <AppButton
          size="small"
          tone="warningGhost"
          onClick={() => onBulkSetEnabled(visibleIds, false)}
        >
          {t('gameSetup.questions.disableVisible')}
        </AppButton>
      </Stack>

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
                    checked={enabledQuestionIds.has(question.questionId)}
                    onChange={(event) => onToggle(question.questionId, event.target.checked)}
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
                  category: question.categoryName,
                  reward: question.reward,
                  asked: question.askedTotalCount,
                  correct: question.correctTotalCount,
                })}
                {question.isEnabled ? '' : ` · ${t('gameSetup.questions.globallyDisabled')}`}
              </Typography>
            </Box>
          ))}
        </Stack>
      </AsyncSection>
    </SectionCard>
  )
}
