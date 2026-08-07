import { Box, Checkbox, Chip, FormControlLabel, FormGroup, Stack, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  gameModifierCatalogQueryOptions,
  modifierCategoryCodes,
} from '../../game-modifiers/index.ts'
import { deriveModifierRoundSummaryMeta } from '../../game-modifiers/model/modifier-round-summary.ts'
import { matchesModifierSearch } from '../../game-modifiers/model/modifier-search.ts'
import type { GameSetupDraftState } from '../model/game-setup-draft.ts'
import {
  AsyncSection,
  FormTextField,
  SectionCard,
  SectionHeader,
} from '../../../shared/ui/index.ts'

interface GameSetupModifiersSectionProps {
  draft: GameSetupDraftState
  onToggle: (modifierId: string, enabled: boolean) => void
  actions?: ReactNode
}

export function GameSetupModifiersSection({
  draft,
  onToggle,
  actions,
}: GameSetupModifiersSectionProps) {
  const { t } = useTranslation()
  const catalogQuery = useQuery(gameModifierCatalogQueryOptions)
  const [search, setSearch] = useState('')
  const categoryLabels = {
    preparation: t('gameModifiers.categories.preparation'),
    round: t('gameModifiers.categories.round'),
    result: t('gameModifiers.categories.result'),
  } as const
  const filteredModifiers = useMemo(
    () =>
      (catalogQuery.data ?? []).filter((modifier) =>
        matchesModifierSearch(modifier, search, [
          t(`gameCatalog.modifiers.mechanics.${modifier.mechanicType}`),
          t(
            `gameCatalog.modifiers.roundSummaryType.${deriveModifierRoundSummaryMeta(modifier).type}`,
          ),
          t(`gameModifiers.categories.${modifier.category}`),
          modifier.requiresHostControl ? t('gameCatalog.modifiers.hostControlBadge') : '',
        ]),
      ),
    [catalogQuery.data, search, t],
  )
  const groupedModifiers = useMemo(
    () =>
      modifierCategoryCodes
        .map((category) => ({
          category,
          items: filteredModifiers.filter((modifier) => modifier.category === category),
        }))
        .filter((group) => group.items.length > 0),
    [filteredModifiers],
  )

  return (
    <SectionCard>
      <SectionHeader
        title={t('gameSetup.modifiers.title')}
        description={t('gameSetup.modifiers.description')}
        actions={actions}
      />
      <AsyncSection
        isLoading={catalogQuery.isLoading}
        isError={catalogQuery.isError}
        isEmpty={!catalogQuery.isLoading && !catalogQuery.isError && groupedModifiers.length === 0}
        loadingMessage={t('gameSetup.modifiers.loading')}
        errorMessage={t('gameSetup.modifiers.error')}
        emptyMessage={
          search.trim().length > 0
            ? t('gameSetup.modifiers.emptySearch')
            : t('gameSetup.modifiers.empty')
        }
      >
        <Stack spacing={1.5} sx={{ mt: 1 }}>
          <FormTextField
            value={search}
            label={t('gameSetup.modifiers.searchLabel')}
            onChange={(event) => setSearch(event.target.value)}
          />

          {groupedModifiers.map((group) => (
            <Box key={group.category}>
              <Typography variant="subtitle2" sx={{ mb: 0.75 }}>
                {categoryLabels[group.category]}
              </Typography>

              <FormGroup>
                {group.items.map((modifier) => {
                  const checked = draft.enabledModifierIds.includes(modifier.id)
                  const roundSummaryMeta = deriveModifierRoundSummaryMeta(modifier)

                  return (
                    <Box
                      key={modifier.id}
                      sx={(theme) => ({
                        py: 0.85,
                        px: 1,
                        borderRadius: 1.25,
                        border: `1px solid ${theme.palette.divider}`,
                        backgroundColor: checked ? 'action.selected' : 'transparent',
                        '& + &': {
                          mt: 0.75,
                        },
                      })}
                    >
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={checked}
                            onChange={(event) => onToggle(modifier.id, event.target.checked)}
                          />
                        }
                        label={`${modifier.name} (${modifier.activationCost})`}
                      />

                      <Stack
                        direction="row"
                        spacing={0.75}
                        flexWrap="wrap"
                        useFlexGap
                        sx={{ ml: 4.5, mt: 0.35 }}
                      >
                        <Chip
                          size="small"
                          variant="outlined"
                          label={t(`gameCatalog.modifiers.mechanics.${modifier.mechanicType}`)}
                        />
                        <Chip
                          size="small"
                          color={roundSummaryMeta.includeInRoundSummary ? 'secondary' : 'default'}
                          variant="outlined"
                          label={t(
                            `gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`,
                          )}
                        />
                        {modifier.requiresHostControl ? (
                          <Chip
                            size="small"
                            color="error"
                            variant="outlined"
                            label={t('gameCatalog.modifiers.hostControlBadge')}
                          />
                        ) : null}
                      </Stack>

                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ ml: 4.5, mt: 0.6, display: 'block', whiteSpace: 'pre-line' }}
                      >
                        {modifier.description}
                      </Typography>
                    </Box>
                  )
                })}
              </FormGroup>
            </Box>
          ))}
        </Stack>
      </AsyncSection>
    </SectionCard>
  )
}
