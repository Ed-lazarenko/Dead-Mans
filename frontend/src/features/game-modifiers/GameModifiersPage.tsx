import { Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierAvailability,
  GameModifierDefinition,
  GameModifierState,
} from '../../shared/api/contracts/index.ts'
import {
  AppButton,
  AppToast,
  AsyncSection,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { gameModifierStateQueryOptions } from './api/game-modifier-queries.ts'
import {
  groupActiveGameModifiers,
  groupAvailableGameModifiers,
} from './model/game-modifier-groups.ts'
import { AdminModifierPanel } from './AdminModifierPanel.tsx'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

const CATEGORY_ORDER = ['preparation', 'round', 'result'] as const

export function GameModifiersPage() {
  const { t } = useTranslation()
  const stateQuery = useQuery(gameModifierStateQueryOptions)
  const activation = useActivateGameModifier()
  const state: GameModifierState | null = stateQuery.data ?? null
  const isEmpty = !stateQuery.isLoading && !stateQuery.isError && state == null

  const activeGroups = state ? groupActiveGameModifiers(state.activeModifiers) : []
  const availableGroups = state
    ? groupAvailableGameModifiers(state.availableModifiers).sort(
        (left, right) =>
          CATEGORY_ORDER.indexOf(left.category) - CATEGORY_ORDER.indexOf(right.category),
      )
    : []
  const availableDefinitionsById = new Map(
    state?.availableModifiers.map((item) => [item.modifier.id, item.modifier]) ?? [],
  )

  return (
    <PageShell sx={{ width: '100%', maxWidth: 1440, mx: 'auto' }}>
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
          <Box
            sx={{
              mt: 1,
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', lg: '320px minmax(0, 1fr)' },
              gap: 2,
              alignItems: 'start',
            }}
          >
            <Stack spacing={2}>
              <SectionCard sx={{ p: 1.5 }}>
                <Stack spacing={1.25}>
                  <Typography variant="subtitle2">{t('gameModifiers.summaryTitle')}</Typography>
                  <Box
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
                      gap: 1,
                    }}
                  >
                    <SummaryMetric
                      label={t('gameModifiers.summaryAvailablePoints')}
                      value={t('gameModifiers.myPointsValue', {
                        points: state.availableQuizPoints,
                      })}
                    />
                    <SummaryMetric
                      label={t('gameModifiers.summarySpentPoints')}
                      value={t('gameModifiers.myPointsValue', {
                        points: state.spentQuizPoints,
                      })}
                    />
                    <SummaryMetric
                      label={t('gameModifiers.summaryEarnedPoints')}
                      value={t('gameModifiers.myPointsValue', {
                        points: state.earnedQuizPoints,
                      })}
                    />
                    <SummaryMetric
                      label={t('gameModifiers.summaryEnabledCount')}
                      value={String(state.availableModifiers.length)}
                    />
                  </Box>
                </Stack>
              </SectionCard>

              <SectionCard sx={{ p: 1.5 }}>
                <Stack spacing={1.25}>
                  <Stack direction="row" justifyContent="space-between" spacing={1}>
                    <Typography variant="subtitle2">{t('gameModifiers.activeTitle')}</Typography>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={t('gameModifiers.summaryActiveCount', {
                        count: activeGroups.length,
                      })}
                    />
                  </Stack>

                  {activeGroups.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      {t('gameModifiers.activeEmpty')}
                    </Typography>
                  ) : (
                    <Stack spacing={1}>
                      {activeGroups.map((group) => (
                        <ActiveModifierGroupCard
                          key={group.modifierId}
                          group={group}
                          definition={availableDefinitionsById.get(group.modifierId)}
                        />
                      ))}
                    </Stack>
                  )}
                </Stack>
              </SectionCard>

              <AdminModifierPanel />
            </Stack>

            <SectionCard sx={{ p: 1.5 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle2">{t('gameModifiers.availableTitle')}</Typography>

                {availableGroups.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    {t('gameModifiers.availableEmpty')}
                  </Typography>
                ) : (
                  availableGroups.map((group) => (
                    <Stack key={group.category} spacing={1}>
                      <Stack
                        direction={{ xs: 'column', sm: 'row' }}
                        spacing={1}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', sm: 'center' }}
                      >
                        <Typography variant="overline" color="text.secondary">
                          {getCategoryLabel(t, group.category)}
                        </Typography>
                        <Chip
                          size="small"
                          variant="outlined"
                          label={t('gameModifiers.categoryCountLabel', {
                            count: group.items.length,
                          })}
                        />
                      </Stack>

                      <Box
                        sx={(theme) => ({
                          border: `1px solid ${alpha(theme.palette.divider, 0.5)}`,
                          borderRadius: 1.5,
                          overflow: 'hidden',
                        })}
                      >
                        {group.items.map((availability, index) => (
                          <Box key={availability.modifier.id}>
                            {index > 0 ? <Divider /> : null}
                            <AvailableModifierRow
                              availability={availability}
                              isBusy={activation.isActivating}
                              isPending={activation.pendingModifierId === availability.modifier.id}
                              onActivate={activation.activate}
                            />
                          </Box>
                        ))}
                      </Box>
                    </Stack>
                  ))
                )}
              </Stack>
            </SectionCard>
          </Box>
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

function SummaryMetric({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.48)}`,
        borderRadius: 1.5,
        px: 1.25,
        py: 1,
      })}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="subtitle2" fontWeight={700}>
        {value}
      </Typography>
    </Box>
  )
}

function ActiveModifierGroupCard({
  group,
  definition,
}: {
  group: ReturnType<typeof groupActiveGameModifiers>[number]
  definition?: GameModifierDefinition
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.primary.main, 0.24)}`,
        backgroundColor: alpha(theme.palette.primary.main, 0.06),
        borderRadius: 1.5,
        px: 1.25,
        py: 1,
      })}
    >
      <Stack spacing={0.75}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          {definition?.iconEmoji ? (
            <Typography sx={{ fontSize: '1.25rem', lineHeight: 1 }}>
              {definition.iconEmoji}
            </Typography>
          ) : null}
          <Typography variant="subtitle2" fontWeight={700}>
            {group.modifierName}
          </Typography>
          <Chip
            size="small"
            color="primary"
            label={t('gameModifiers.activeGroupCount', {
              count: group.activationsCount,
            })}
          />
          {definition ? (
            <Chip
              size="small"
              variant="outlined"
              label={getCategoryLabel(t, definition.category)}
            />
          ) : null}
        </Stack>

        <Typography variant="caption" color="text.secondary">
          {t('gameModifiers.activeGroupLatest', {
            player: group.lastActivatedByDisplayName,
            time: new Date(group.lastActivatedAtUtc).toLocaleTimeString(),
          })}
        </Typography>

        <Typography variant="caption" color="text.secondary">
          {t('gameModifiers.activeGroupSpent', {
            cost: group.totalActivationCost,
          })}
        </Typography>

        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          {group.activators.map((activator) => (
            <Chip
              key={activator.displayName}
              size="small"
              variant="outlined"
              label={
                activator.activationsCount > 1
                  ? t('gameModifiers.activeGroupActivatorWithCount', {
                      player: activator.displayName,
                      count: activator.activationsCount,
                    })
                  : activator.displayName
              }
            />
          ))}
        </Stack>
      </Stack>
    </Box>
  )
}

interface AvailableModifierRowProps {
  availability: GameModifierAvailability
  isBusy: boolean
  isPending: boolean
  onActivate: (modifierId: string) => void
}

function AvailableModifierRow({
  availability,
  isBusy,
  isPending,
  onActivate,
}: AvailableModifierRowProps) {
  const { t } = useTranslation()
  const definition = availability.modifier

  return (
    <Box
      sx={(theme) => ({
        px: 1.25,
        py: 1,
        backgroundColor: availability.canActivate
          ? alpha(theme.palette.success.main, 0.04)
          : 'transparent',
      })}
    >
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.25}
        justifyContent="space-between"
        alignItems={{ xs: 'stretch', md: 'center' }}
      >
        <Stack direction="row" spacing={1.25} sx={{ minWidth: 0, flex: 1 }}>
          <Box sx={{ width: 24, display: 'flex', justifyContent: 'center', pt: 0.25 }}>
            {definition.iconEmoji ? (
              <Typography sx={{ fontSize: '1.25rem', lineHeight: 1 }}>
                {definition.iconEmoji}
              </Typography>
            ) : null}
          </Box>

          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="subtitle2" fontWeight={700}>
                {definition.name}
              </Typography>
              {availability.isActive ? (
                <Chip size="small" color="primary" label={t('gameModifiers.activeTag')} />
              ) : null}
            </Stack>

            <Typography
              variant="caption"
              color="text.secondary"
              title={definition.description}
              sx={{
                mt: 0.25,
                display: '-webkit-box',
                overflow: 'hidden',
                WebkitBoxOrient: 'vertical',
                WebkitLineClamp: 2,
              }}
            >
              {definition.description}
            </Typography>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap sx={{ mt: 0.75 }}>
              <Chip
                size="small"
                label={t('gameModifiers.costLabel', { cost: definition.activationCost })}
              />
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
          </Box>
        </Stack>

        <Box
          sx={{
            width: { xs: '100%', md: 190 },
            flexShrink: 0,
            display: 'flex',
            justifyContent: { xs: 'stretch', md: 'flex-end' },
            alignItems: 'center',
          }}
        >
          {availability.canActivate ? (
            <AppButton
              tone="primary"
              size="small"
              fullWidth
              disabled={isBusy}
              onClick={() => onActivate(definition.id)}
            >
              {isPending ? t('gameModifiers.activatePending') : t('gameModifiers.activateAction')}
            </AppButton>
          ) : availability.blockedReason ? (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ textAlign: { xs: 'left', md: 'right' } }}
            >
              {t(`gameModifiers.blockedReasons.${availability.blockedReason}`)}
            </Typography>
          ) : null}
        </Box>
      </Stack>
    </Box>
  )
}

function getCategoryLabel(
  t: ReturnType<typeof useTranslation>['t'],
  category: GameModifierAvailability['modifier']['category'],
) {
  switch (category) {
    case 'preparation':
      return t('gameModifiers.categories.preparation')
    case 'round':
      return t('gameModifiers.categories.round')
    case 'result':
      return t('gameModifiers.categories.result')
  }
}
