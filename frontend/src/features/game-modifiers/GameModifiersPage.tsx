import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import type { ReactNode } from 'react'
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
import { AdminModifierPanel } from './AdminModifierPanel.tsx'
import { gameModifierStateQueryOptions } from './api/game-modifier-queries.ts'
import {
  groupActiveGameModifiers,
  groupAvailableGameModifiers,
} from './model/game-modifier-groups.ts'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

export function GameModifiersPage() {
  const { t } = useTranslation()
  const stateQuery = useQuery(gameModifierStateQueryOptions)
  const activation = useActivateGameModifier()
  const state: GameModifierState | null = stateQuery.data ?? null
  const isEmpty = !stateQuery.isLoading && !stateQuery.isError && state == null

  const activeGroups = state ? groupActiveGameModifiers(state.activeModifiers) : []
  const availableGroups = state ? groupAvailableGameModifiers(state.availableModifiers) : []
  const availableDefinitionsById = new Map(
    state?.availableModifiers.map((item) => [item.modifier.id, item.modifier]) ?? [],
  )

  return (
    <PageShell sx={{ width: '100%', maxWidth: 'none', mx: 0 }}>
      <SectionHeader
        title={t('gameModifiers.title')}
        actions={
          state ? (
            <Stack
              direction="row"
              spacing={1}
              flexWrap="wrap"
              useFlexGap
              justifyContent={{ xs: 'stretch', md: 'flex-end' }}
            >
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
          <>
            <Box
              sx={{
                mt: 1.25,
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr',
                  lg: 'minmax(0, 1fr) minmax(0, 1fr)',
                },
                gap: 1.5,
                alignItems: 'stretch',
              }}
            >
              <SectionCard
                sx={{
                  p: 1.25,
                  display: 'flex',
                  flexDirection: 'column',
                }}
              >
                <Stack spacing={1.1} sx={{ flex: 1 }}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={1}
                    justifyContent="space-between"
                    alignItems={{ xs: 'flex-start', sm: 'center' }}
                  >
                    <Typography variant="subtitle2">{t('gameModifiers.activeTitle')}</Typography>
                    <Chip
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
                    <Stack spacing={0.85}>
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

              <SectionCard
                sx={{
                  p: 1.25,
                  display: 'flex',
                  flexDirection: 'column',
                }}
              >
                <Stack spacing={1.1} sx={{ flex: 1 }}>
                  <Typography variant="subtitle2">{t('gameModifiers.availableTitle')}</Typography>

                  {availableGroups.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      {t('gameModifiers.availableEmpty')}
                    </Typography>
                  ) : (
                    <Stack spacing={1}>
                      {availableGroups.map((group) => (
                        <CategorySectionCard key={group.category} category={group.category}>
                          <Stack spacing={0.75}>
                            <Stack
                              direction="row"
                              spacing={1}
                              justifyContent="space-between"
                              alignItems="center"
                              flexWrap="wrap"
                              useFlexGap
                            >
                              <Typography variant="overline" sx={{ letterSpacing: '0.08em' }}>
                                {getCategoryLabel(t, group.category)}
                              </Typography>
                              <Chip
                                variant="outlined"
                                label={t('gameModifiers.categoryCountLabel', {
                                  count: group.items.length,
                                })}
                              />
                            </Stack>

                            <Stack spacing={0.75}>
                              {group.items.map((availability) => (
                                <AvailableModifierCard
                                  key={availability.modifier.id}
                                  availability={availability}
                                  isBusy={activation.isActivating}
                                  isPending={
                                    activation.pendingModifierId === availability.modifier.id
                                  }
                                  onActivate={activation.activate}
                                />
                              ))}
                            </Stack>
                          </Stack>
                        </CategorySectionCard>
                      ))}
                    </Stack>
                  )}
                </Stack>
              </SectionCard>
            </Box>

            <AdminModifierPanel enabledModifiersCount={state.availableModifiers.length} />
          </>
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
        border: `1px solid ${alpha(theme.palette.primary.main, 0.28)}`,
        backgroundColor: alpha(theme.palette.primary.main, 0.07),
        borderRadius: 1.5,
        px: 1,
        py: 0.8,
        height: '100%',
      })}
    >
      <Stack spacing={0.75}>
        <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
          {definition?.iconEmoji ? (
            <Typography sx={{ fontSize: '1.1rem', lineHeight: 1 }}>
              {definition.iconEmoji}
            </Typography>
          ) : null}
          <Typography variant="subtitle2" fontWeight={700}>
            {group.modifierName}
          </Typography>
          <Chip
            color="primary"
            label={t('gameModifiers.activeGroupCount', {
              count: group.activationsCount,
            })}
          />
          <Chip
            color="warning"
            label={t('gameModifiers.costShortLabel', {
              cost: group.totalActivationCost,
            })}
          />
          {definition ? <Chip variant="outlined" label={getCategoryLabel(t, definition.category)} /> : null}
        </Stack>

        {definition?.description ? <DescriptionBlock description={definition.description} compact /> : null}

        <Typography variant="caption" color="text.secondary">
          {t('gameModifiers.activeGroupLatest', {
            player: group.lastActivatedByDisplayName,
            time: new Date(group.lastActivatedAtUtc).toLocaleTimeString(),
          })}
        </Typography>

        <Stack direction="row" spacing={0.6} flexWrap="wrap" useFlexGap>
          {group.activators.map((activator) => (
            <Chip
              key={activator.userId}
              color="info"
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

interface AvailableModifierCardProps {
  availability: GameModifierAvailability
  isBusy: boolean
  isPending: boolean
  onActivate: (modifierId: string) => void
}

function AvailableModifierCard({
  availability,
  isBusy,
  isPending,
  onActivate,
}: AvailableModifierCardProps) {
  const { t } = useTranslation()
  const definition = availability.modifier
  const hasLimit = availability.limit != null
  const limitReached = hasLimit && availability.activationsCount >= (availability.limit ?? 0)
  const hasConflicts = definition.conflictingModifierIds.length > 0

  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${
          availability.blockedReason === 'limit_reached'
            ? alpha(theme.palette.error.main, 0.72)
            : availability.canActivate
            ? alpha(theme.palette.success.main, 0.28)
            : alpha(theme.palette.divider, 0.48)
        }`,
        backgroundColor: availability.canActivate
          ? alpha(theme.palette.success.main, 0.05)
          : alpha(theme.palette.background.paper, 0.18),
        borderRadius: 1.5,
        p: 0.9,
        height: '100%',
      })}
    >
      <Stack spacing={0.8}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={0.85}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', md: 'flex-start' }}
        >
          <Stack direction="row" spacing={0.9} sx={{ minWidth: 0, flex: 1 }}>
            <Box
              sx={{
                width: 26,
                height: 26,
                borderRadius: 1.25,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: 'rgba(255,255,255,0.04)',
                flexShrink: 0,
              }}
            >
              {definition.iconEmoji ? (
                <Typography sx={{ fontSize: '1rem', lineHeight: 1 }}>
                  {definition.iconEmoji}
                </Typography>
              ) : null}
            </Box>

            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack
                direction="row"
                spacing={0.55}
                flexWrap="wrap"
                useFlexGap
                alignItems="center"
              >
                <Typography variant="subtitle2" fontWeight={700}>
                  {definition.name}
                </Typography>
                <Chip
                  color="warning"
                  label={t('gameModifiers.costShortLabel', {
                    cost: definition.activationCost,
                  })}
                />
                <Chip
                  color={limitReached ? 'error' : 'info'}
                  variant={limitReached ? 'filled' : 'outlined'}
                  label={
                    hasLimit
                      ? t('gameModifiers.limitProgressLabel', {
                          count: availability.activationsCount,
                          limit: availability.limit,
                        })
                      : t('gameModifiers.noLimit')
                  }
                />
                {availability.isActive ? (
                  <Chip color="success" label={t('gameModifiers.activeTag')} />
                ) : null}
                {definition.requiresHostControl ? (
                  <Chip color="info" label={t('gameModifiers.hostControlTag')} />
                ) : null}
                {hasConflicts ? (
                  <Chip
                    color={availability.blockedReason === 'conflict_active' ? 'error' : 'warning'}
                    variant={
                      availability.blockedReason === 'conflict_active' ? 'filled' : 'outlined'
                    }
                    label={t('gameModifiers.conflictsTag', {
                      count: definition.conflictingModifierIds.length,
                    })}
                  />
                ) : null}
              </Stack>
            </Box>
          </Stack>

          <Box
            sx={{
              width: { xs: '100%', md: 210 },
              flexShrink: 0,
              display: 'flex',
              justifyContent: { xs: 'flex-start', md: 'flex-end' },
              alignItems: 'stretch',
            }}
          >
            {availability.canActivate ? (
              <AppButton
                tone="primary"
                size="small"
                fullWidth
                disabled={isBusy}
                onClick={() => onActivate(definition.id)}
                sx={{ minHeight: 34 }}
              >
                {isPending ? t('gameModifiers.activatePending') : t('gameModifiers.activateAction')}
              </AppButton>
            ) : (
              <BlockedReasonPlaque blockedReason={availability.blockedReason} />
            )}
          </Box>
        </Stack>

        <DescriptionBlock description={definition.description} compact />
      </Stack>
    </Box>
  )
}

function DescriptionBlock({
  description,
  compact = false,
}: {
  description: string
  compact?: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.44)}`,
        backgroundColor: alpha(theme.palette.common.black, 0.1),
        borderRadius: 1.25,
        px: compact ? 0.85 : 1,
        py: compact ? 0.65 : 0.85,
      })}
    >
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{
          display: 'block',
          mb: 0.25,
          textTransform: 'uppercase',
          letterSpacing: '0.06em',
        }}
      >
        {t('gameModifiers.descriptionLabel')}
      </Typography>
      <Typography variant="body2">
        {description}
      </Typography>
    </Box>
  )
}

function BlockedReasonPlaque({
  blockedReason,
}: {
  blockedReason: GameModifierAvailability['blockedReason']
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => {
        const accent =
          blockedReason === 'limit_reached'
            ? theme.palette.error.main
            : blockedReason === 'insufficient_points'
              ? theme.palette.warning.main
              : theme.palette.info.main

        return {
          width: '100%',
          minHeight: 48,
          px: 0.95,
          py: 0.8,
          borderRadius: 1.75,
          border: `1px dashed ${alpha(accent, 0.76)}`,
          background: `repeating-linear-gradient(-45deg, ${alpha(accent, 0.17)} 0 10px, ${alpha(
            theme.palette.common.black,
            0.08,
          )} 10px 20px)`,
          boxShadow: `inset 0 0 0 1px ${alpha(accent, 0.28)}`,
          display: 'flex',
          alignItems: 'center',
          gap: 0.9,
          position: 'relative',
          overflow: 'hidden',
          '&::after': {
            content: '""',
            position: 'absolute',
            inset: 4,
            borderRadius: 1.1,
            border: `1px solid ${alpha(theme.palette.common.white, 0.08)}`,
            pointerEvents: 'none',
          },
        }
      }}
    >
      <Box
        aria-hidden
        sx={(theme) => {
          const accent =
            blockedReason === 'limit_reached'
              ? theme.palette.error.main
              : blockedReason === 'insufficient_points'
                ? theme.palette.warning.main
                : theme.palette.info.main

          return {
            width: 26,
            height: 26,
            borderRadius: 999,
            border: `1px solid ${alpha(accent, 0.9)}`,
            backgroundColor: alpha(theme.palette.common.black, 0.28),
            color: accent,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '1rem',
            fontWeight: 900,
            flexShrink: 0,
            position: 'relative',
            zIndex: 1,
          }
        }}
      >
        !
      </Box>
      <Typography
        sx={{
            position: 'relative',
            zIndex: 1,
            textAlign: 'left',
            fontSize: { xs: '0.82rem', md: '0.9rem' },
            fontWeight: 800,
            lineHeight: 1.15,
            letterSpacing: '0.02em',
            pr: 0.25,
          }}
      >
        {blockedReason != null
          ? t(`gameModifiers.blockedReasons.${blockedReason}`)
          : t('gameModifiers.unavailableAction')}
      </Typography>
    </Box>
  )
}

function CategorySectionCard({
  category,
  children,
}: {
  category: GameModifierAvailability['modifier']['category']
  children: ReactNode
}) {
  return (
    <SectionCard
      sx={(theme) => {
        const accent =
          category === 'preparation'
            ? theme.palette.info.main
            : category === 'round'
              ? theme.palette.success.main
              : theme.palette.warning.main

        return {
          p: 0.9,
          borderColor: alpha(accent, 0.34),
          boxShadow: `inset 0 1px 0 ${alpha(accent, 0.16)}`,
          position: 'relative',
          overflow: 'hidden',
          '&::before': {
            content: '""',
            position: 'absolute',
            inset: 0,
            borderTop: `3px solid ${alpha(accent, 0.88)}`,
            pointerEvents: 'none',
          },
        }
      }}
    >
      {children}
    </SectionCard>
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
