import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierActivation,
  GameModifierAvailability,
  GameModifierDefinition,
} from '../../shared/api/contracts/index.ts'
import { AppButton, AppToast, AsyncSection, PageShell, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import { gameModifierStateQueryOptions } from './api/game-modifier-queries.ts'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

export function GameModifiersPage() {
  const { t } = useTranslation()
  const stateQuery = useQuery(gameModifierStateQueryOptions)
  const activation = useActivateGameModifier()
  const state = stateQuery.data ?? null
  const isEmpty = !stateQuery.isLoading && !stateQuery.isError && state == null

  return (
    <PageShell>
      <SectionHeader
        title={t('gameModifiers.title')}
        actions={
          state ? (
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip
                label={`${t('gameModifiers.myPoints')}: ${t('gameModifiers.myPointsValue', {
                  points: state.availableQuizPoints,
                })}`}
                color="primary"
                variant="outlined"
              />
              <Chip
                label={t(
                  state.isOrderingOpen
                    ? 'gameModifiers.orderingOpen'
                    : 'gameModifiers.orderingClosed',
                )}
                color={state.isOrderingOpen ? 'success' : 'warning'}
                variant="outlined"
              />
            </Stack>
          ) : undefined
        }
      />

      <AsyncSection
        isLoading={stateQuery.isLoading}
        isError={stateQuery.isError}
        isEmpty={isEmpty}
        loadingMessage={t('gameModifiers.loading')}
        errorMessage={t('gameModifiers.errorLoading')}
        emptyMessage={t('gameModifiers.noGame')}
      >
        {state ? (
          <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} sx={{ mt: 1 }}>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
                {t('gameModifiers.activeTitle')}
              </Typography>
              {state.activeModifiers.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameModifiers.activeEmpty')}
                </Typography>
              ) : (
                <Stack spacing={1.25}>
                  {state.activeModifiers.map((modifier) => (
                    <ActiveModifierCard
                      key={`${modifier.modifierId}-${modifier.activatedAtUtc}`}
                      activation={modifier}
                    />
                  ))}
                </Stack>
              )}
            </Box>

            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
                {t('gameModifiers.availableTitle')}
              </Typography>
              {state.availableModifiers.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameModifiers.availableEmpty')}
                </Typography>
              ) : (
                <Stack spacing={1.25}>
                  {state.availableModifiers.map((availability) => (
                    <AvailableModifierCard
                      key={availability.modifier.id}
                      availability={availability}
                      isBusy={activation.isActivating}
                      onActivate={activation.activate}
                    />
                  ))}
                </Stack>
              )}
            </Box>
          </Stack>
        ) : null}
      </AsyncSection>

      <AppToast
        message={activation.toastMessage}
        onClose={activation.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
    </PageShell>
  )
}

function ActiveModifierCard({ activation }: { activation: GameModifierActivation }) {
  const { t } = useTranslation()
  return (
    <SectionCard>
      <Stack spacing={0.75}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="subtitle2" fontWeight={700}>
            {activation.modifierName}
          </Typography>
          <Chip label={t('gameModifiers.activeTag')} color="primary" size="small" />
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {t('gameModifiers.activatedBy', { player: activation.activatedByDisplayName })}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('gameModifiers.activationSpend', { cost: activation.activationCost })}
          {' · '}
          {t('gameModifiers.activatedAt', {
            time: new Date(activation.activatedAtUtc).toLocaleTimeString(),
          })}
        </Typography>
      </Stack>
    </SectionCard>
  )
}

interface AvailableModifierCardProps {
  availability: GameModifierAvailability
  isBusy: boolean
  onActivate: (modifierId: string) => void
}

function AvailableModifierCard({ availability, isBusy, onActivate }: AvailableModifierCardProps) {
  const { t } = useTranslation()
  const definition = availability.modifier
  return (
    <SectionCard
      sx={(theme) => ({
        borderColor: availability.canActivate
          ? alpha(theme.palette.success.main, 0.52)
          : alpha(theme.palette.divider, 0.4),
      })}
    >
      <Stack spacing={1}>
        <ModifierHeader definition={definition} />
        <Typography variant="body2" color="text.secondary">
          {definition.description}
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          <Chip size="small" label={t('gameModifiers.costLabel', { cost: definition.activationCost })} />
          <Chip
            size="small"
            label={
              availability.limit != null
                ? t('gameModifiers.limitProgressLabel', {
                    count: availability.activationsCount,
                    limit: availability.limit,
                  })
                : t('gameModifiers.noLimit')
            }
          />
          {definition.requiresHostControl ? (
            <Chip size="small" label={t('gameModifiers.hostControlTag')} />
          ) : null}
        </Stack>
        {availability.canActivate ? (
          <AppButton
            tone="primary"
            size="small"
            disabled={isBusy}
            onClick={() => onActivate(definition.id)}
          >
            {t('gameModifiers.activateAction')}
          </AppButton>
        ) : availability.blockedReason ? (
          <Typography variant="caption" color="text.secondary">
            {t(`gameModifiers.blockedReasons.${availability.blockedReason}`)}
          </Typography>
        ) : null}
      </Stack>
    </SectionCard>
  )
}

function ModifierHeader({ definition }: { definition: GameModifierDefinition }) {
  const { t } = useTranslation()
  const categoryLabels = {
    preparation: t('gameModifiers.categories.preparation'),
    round: t('gameModifiers.categories.round'),
    result: t('gameModifiers.categories.result'),
  } as const

  return (
    <Stack direction="row" spacing={1.25} alignItems="flex-start">
      {definition.iconEmoji ? (
        <Typography sx={{ fontSize: '1.5rem', lineHeight: 1 }}>{definition.iconEmoji}</Typography>
      ) : null}
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="subtitle2" fontWeight={700}>
          {definition.name}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {categoryLabels[definition.category]}
        </Typography>
      </Box>
    </Stack>
  )
}
