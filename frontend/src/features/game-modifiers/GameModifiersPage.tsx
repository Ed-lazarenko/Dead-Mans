import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState, type ReactNode } from 'react'
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
  FormTextField,
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
import { deriveModifierRoundSummaryMeta } from './model/modifier-round-summary.ts'
import { matchesModifierSearch } from './model/modifier-search.ts'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

export function GameModifiersPage() {
  const { t } = useTranslation()
  const stateQuery = useQuery(gameModifierStateQueryOptions)
  const activation = useActivateGameModifier()
  const [search, setSearch] = useState('')
  const state: GameModifierState | null = stateQuery.data ?? null
  const isEmpty = !stateQuery.isLoading && !stateQuery.isError && state == null

  const availableDefinitionsById = useMemo(
    () => new Map(state?.availableModifiers.map((item) => [item.modifier.id, item.modifier]) ?? []),
    [state],
  )
  const filteredAvailableModifiers = useMemo(
    () =>
      (state?.availableModifiers ?? []).filter((availability) =>
        matchesModifierSearch(availability.modifier, search, [
          t(`gameModifiers.categories.${availability.modifier.category}`),
          t(`gameCatalog.modifiers.mechanics.${availability.modifier.mechanicType}`),
          t(
            `gameCatalog.modifiers.roundSummaryType.${
              deriveModifierRoundSummaryMeta(availability.modifier).type
            }`,
          ),
          availability.modifier.requiresHostControl ? t('gameModifiers.hostControlTag') : '',
        ]),
      ),
    [search, state?.availableModifiers, t],
  )
  const availableGroups = state ? groupAvailableGameModifiers(filteredAvailableModifiers) : []
  const activeGroups = useMemo(() => {
    if (!state) {
      return []
    }

    return groupActiveGameModifiers(state.activeModifiers).filter((group) => {
      const definition = availableDefinitionsById.get(group.modifierId)
      if (!definition) {
        return group.modifierName.toLowerCase().includes(search.trim().toLowerCase())
      }

      return matchesModifierSearch(definition, search, [
        t(`gameModifiers.categories.${definition.category}`),
        t(`gameCatalog.modifiers.mechanics.${definition.mechanicType}`),
        t(
          `gameCatalog.modifiers.roundSummaryType.${deriveModifierRoundSummaryMeta(definition).type}`,
        ),
        definition.requiresHostControl ? t('gameModifiers.hostControlTag') : '',
      ])
    })
  }, [availableDefinitionsById, search, state, t])
  const hasSearch = search.trim().length > 0

  return (
    <PageShell sx={{ width: '100%', maxWidth: 'none', mx: 0 }}>
      <SectionHeader title={t('gameModifiers.title')} />

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
            <ModifierStatusBar
              state={state}
              search={search}
              activeGroupsCount={activeGroups.length}
              availableCount={filteredAvailableModifiers.length}
              onSearchChange={setSearch}
            />

            <Box
              sx={{
                mt: 1.5,
                display: 'grid',
                gridTemplateColumns: {
                  xs: '1fr',
                  lg: 'minmax(320px, 0.86fr) minmax(0, 1.4fr)',
                },
                gap: 1.25,
                alignItems: 'start',
              }}
            >
              <SectionCard
                sx={{
                  p: 1.15,
                  display: 'flex',
                  flexDirection: 'column',
                  position: { lg: 'sticky' },
                  top: { lg: 88 },
                }}
              >
                <Stack spacing={0.95} sx={{ flex: 1 }}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={0.8}
                    justifyContent="space-between"
                    alignItems={{ xs: 'flex-start', sm: 'center' }}
                  >
                    <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
                      {t('gameModifiers.activeTitle')}
                    </Typography>
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
                      {hasSearch ? t('gameModifiers.emptySearch') : t('gameModifiers.activeEmpty')}
                    </Typography>
                  ) : (
                    <Stack spacing={0.7}>
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
                  p: 1.15,
                  display: 'flex',
                  flexDirection: 'column',
                }}
              >
                <Stack spacing={0.95} sx={{ flex: 1 }}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={0.8}
                    alignItems={{ xs: 'flex-start', sm: 'center' }}
                    justifyContent="space-between"
                  >
                    <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
                      {t('gameModifiers.availableTitle')}
                    </Typography>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={t('gameModifiers.categoryCountLabel', {
                        count: filteredAvailableModifiers.length,
                      })}
                    />
                  </Stack>

                  {availableGroups.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      {hasSearch
                        ? t('gameModifiers.emptySearch')
                        : t('gameModifiers.availableEmpty')}
                    </Typography>
                  ) : (
                    <Stack spacing={1.1}>
                      {availableGroups.map((group) => (
                        <CategorySection key={group.category} category={group.category}>
                          <Stack spacing={0.7}>
                            <Stack
                              direction="row"
                              spacing={0.8}
                              justifyContent="space-between"
                              alignItems="center"
                              flexWrap="wrap"
                              useFlexGap
                            >
                              <Typography
                                variant="caption"
                                color="text.secondary"
                                sx={{ fontWeight: 850 }}
                              >
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

                            <Stack spacing={0.65}>
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
                        </CategorySection>
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

function ModifierStatusBar({
  state,
  search,
  activeGroupsCount,
  availableCount,
  onSearchChange,
}: {
  state: GameModifierState
  search: string
  activeGroupsCount: number
  availableCount: number
  onSearchChange: (value: string) => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        mt: 1.25,
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        px: { xs: 1.15, sm: 1.35 },
        py: { xs: 1.1, sm: 1.25 },
      })}
    >
      <Box
        sx={{
          display: 'grid',
          gap: 1,
          gridTemplateColumns: {
            xs: '1fr',
            lg: 'minmax(260px, 1fr) minmax(0, 1.35fr)',
          },
          alignItems: 'center',
        }}
      >
        <FormTextField
          value={search}
          label={t('gameModifiers.searchLabel')}
          onChange={(event) => onSearchChange(event.target.value)}
        />

        <Box
          sx={{
            display: 'grid',
            gap: 0.7,
            gridTemplateColumns: {
              xs: 'repeat(2, minmax(0, 1fr))',
              md: 'repeat(5, minmax(0, 1fr))',
            },
          }}
        >
          <ModifierSummaryTile
            label={t('gameModifiers.summaryAvailablePoints')}
            value={t('gameModifiers.myPointsValue', { points: state.availableQuizPoints })}
          />
          <ModifierSummaryTile
            label={t('gameModifiers.summarySpentPoints')}
            value={t('gameModifiers.myPointsValue', { points: state.spentQuizPoints })}
          />
          <ModifierSummaryTile
            label={t('gameModifiers.activeTitle')}
            value={String(activeGroupsCount)}
          />
          <ModifierSummaryTile
            label={t('gameModifiers.summaryEnabledCount')}
            value={String(availableCount)}
          />
          <ModifierSummaryTile
            label={t('gameModifiers.summaryTitle')}
            value={
              state.isOrderingOpen
                ? t('gameModifiers.orderingOpen')
                : t('gameModifiers.orderingClosed')
            }
            tone={state.isOrderingOpen ? 'success' : 'warning'}
          />
        </Box>
      </Box>
    </Box>
  )
}

function ModifierSummaryTile({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: string
  tone?: 'default' | 'success' | 'warning'
}) {
  return (
    <Box
      sx={(theme) => {
        const accent =
          tone === 'success'
            ? theme.palette.success.main
            : tone === 'warning'
              ? theme.palette.warning.main
              : theme.palette.divider

        return {
          minWidth: 0,
          borderRadius: 1.5,
          border: `1px solid ${alpha(accent, tone === 'default' ? 0.72 : 0.42)}`,
          backgroundColor:
            tone === 'default' ? alpha(theme.palette.background.paper, 0.36) : alpha(accent, 0.08),
          px: 0.85,
          py: 0.7,
        }
      }}
    >
      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 850 }} noWrap>
        {value}
      </Typography>
    </Box>
  )
}

function ModifierIcon({ emoji }: { emoji?: string | null }) {
  return (
    <Box
      sx={(theme) => ({
        width: 28,
        height: 28,
        borderRadius: 1.25,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        border: `1px solid ${alpha(theme.palette.divider, 0.7)}`,
        flexShrink: 0,
      })}
    >
      {emoji ? <Typography sx={{ fontSize: '1rem', lineHeight: 1 }}>{emoji}</Typography> : null}
    </Box>
  )
}

function InlineMetaPill({
  label,
  tone = 'default',
}: {
  label: string
  tone?: 'default' | 'success' | 'warning' | 'error'
}) {
  return (
    <Box
      component="span"
      sx={(theme) => {
        const accent =
          tone === 'success'
            ? theme.palette.success.main
            : tone === 'warning'
              ? theme.palette.warning.main
              : tone === 'error'
                ? theme.palette.error.main
                : theme.palette.divider

        return {
          display: 'inline-flex',
          alignItems: 'center',
          minWidth: 0,
          minHeight: 22,
          borderRadius: 999,
          border: `1px solid ${alpha(accent, tone === 'default' ? 0.74 : 0.44)}`,
          backgroundColor:
            tone === 'default' ? alpha(theme.palette.background.paper, 0.26) : alpha(accent, 0.08),
          px: 0.7,
          typography: 'caption',
          color: 'text.secondary',
          fontWeight: 750,
          lineHeight: 1,
        }
      }}
    >
      {label}
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
        px: 1,
        py: 0.8,
        height: '100%',
      })}
    >
      <Stack spacing={0.75}>
        <Stack direction="row" spacing={0.8} alignItems="flex-start">
          <ModifierIcon emoji={definition?.iconEmoji} />
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="subtitle2" fontWeight={800}>
              {group.modifierName}
            </Typography>
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.45 }}>
              <InlineMetaPill
                label={t('gameModifiers.activeGroupCount', {
                  count: group.activationsCount,
                })}
              />
              <InlineMetaPill
                label={t('gameModifiers.costShortLabel', {
                  cost: group.activationCost,
                })}
                tone="warning"
              />
              {definition ? (
                <InlineMetaPill label={getCategoryLabel(t, definition.category)} />
              ) : null}
              {definition ? (
                <InlineMetaPill
                  label={t(
                    `gameCatalog.modifiers.roundSummaryType.${
                      deriveModifierRoundSummaryMeta(definition).type
                    }`,
                  )}
                />
              ) : null}
            </Stack>
          </Box>
        </Stack>

        {definition?.description ? (
          <DescriptionBlock description={definition.description} compact />
        ) : null}

        <Typography variant="caption" color="text.secondary">
          {t('gameModifiers.activeGroupLatest', {
            player: group.lastActivatedByDisplayName,
            time: new Date(group.lastActivatedAtUtc).toLocaleTimeString(),
          })}
        </Typography>

        <Stack direction="row" spacing={0.45} flexWrap="wrap" useFlexGap>
          {group.activators.map((activator) => (
            <Chip
              key={activator.userId}
              size="small"
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
  const roundSummaryMeta = deriveModifierRoundSummaryMeta(definition)
  const hasLimit = availability.limit != null
  const limitReached = hasLimit && availability.activationsCount >= (availability.limit ?? 0)
  const hasConflicts = definition.conflictingModifierIds.length > 0

  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${
          availability.blockedReason === 'limit_reached'
            ? alpha(theme.palette.error.main, 0.46)
            : availability.canActivate
              ? alpha(theme.palette.success.main, 0.24)
              : alpha(theme.palette.divider, 0.72)
        }`,
        backgroundColor: availability.canActivate
          ? alpha(theme.palette.success.main, 0.04)
          : alpha(theme.palette.background.paper, 0.26),
        borderRadius: 1.5,
        px: 1,
        py: 0.85,
        height: '100%',
      })}
    >
      <Stack spacing={0.7}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={0.9}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', md: 'center' }}
        >
          <Stack direction="row" spacing={0.8} sx={{ minWidth: 0, flex: 1 }}>
            <ModifierIcon emoji={definition.iconEmoji} />

            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="subtitle2" fontWeight={800}>
                {definition.name}
              </Typography>
              <Stack direction="row" spacing={0.45} flexWrap="wrap" useFlexGap sx={{ mt: 0.45 }}>
                <InlineMetaPill
                  label={t('gameModifiers.costShortLabel', {
                    cost: definition.activationCost,
                  })}
                  tone="warning"
                />
                <InlineMetaPill
                  tone={limitReached ? 'error' : 'default'}
                  label={
                    hasLimit
                      ? t('gameModifiers.limitProgressLabel', {
                          count: availability.activationsCount,
                          limit: availability.limit,
                        })
                      : t('gameModifiers.noLimit')
                  }
                />
                <InlineMetaPill
                  label={t(`gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`)}
                />
                {availability.isActive ? (
                  <InlineMetaPill label={t('gameModifiers.activeTag')} tone="success" />
                ) : null}
                {definition.requiresHostControl ? (
                  <InlineMetaPill label={t('gameModifiers.hostControlTag')} />
                ) : null}
                {hasConflicts ? (
                  <InlineMetaPill
                    tone={availability.blockedReason === 'conflict_active' ? 'error' : 'warning'}
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
              width: { xs: '100%', md: 190 },
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
                sx={{ minHeight: 32 }}
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
  return (
    <Box
      sx={{
        px: compact ? 0 : 0.2,
        py: compact ? 0 : 0.2,
      }}
    >
      <Typography variant="body2" color="text.secondary">
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
          minHeight: 32,
          px: 0.85,
          py: 0.55,
          borderRadius: 1.5,
          border: `1px solid ${alpha(accent, 0.42)}`,
          backgroundColor: alpha(accent, 0.08),
          display: 'flex',
          alignItems: 'center',
          gap: 0.65,
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
            width: 20,
            height: 20,
            borderRadius: 999,
            border: `1px solid ${alpha(accent, 0.56)}`,
            backgroundColor: alpha(theme.palette.background.paper, 0.36),
            color: accent,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '0.78rem',
            fontWeight: 900,
            flexShrink: 0,
          }
        }}
      >
        !
      </Box>
      <Typography
        sx={{
          textAlign: 'left',
          fontSize: '0.82rem',
          fontWeight: 750,
          lineHeight: 1.2,
        }}
      >
        {blockedReason != null
          ? t(`gameModifiers.blockedReasons.${blockedReason}`)
          : t('gameModifiers.unavailableAction')}
      </Typography>
    </Box>
  )
}

function CategorySection({
  category,
  children,
}: {
  category: GameModifierAvailability['modifier']['category']
  children: ReactNode
}) {
  return (
    <Box
      sx={(theme) => {
        const accent =
          category === 'preparation'
            ? theme.palette.info.main
            : category === 'round'
              ? theme.palette.success.main
              : theme.palette.warning.main

        return {
          borderTop: `1px solid ${alpha(accent, 0.36)}`,
          pt: 0.9,
          position: 'relative',
        }
      }}
    >
      {children}
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
