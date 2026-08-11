import { Box, Collapse, Divider, List, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierAvailability,
  GameModifierDefinition,
  GameModifierState,
} from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
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
  const { user } = useAuth()
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
    <PageShell sx={{ width: '100%', maxWidth: 1180, mx: 'auto', p: 0 }}>
      <SectionHeader
        title={t('gameModifiers.title')}
        actions={
          state ? (
            <AdminModifierPanel enabledModifiersCount={state.availableModifiers.length} />
          ) : null
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
            <ModifierStatusBar
              state={state}
              search={search}
              userDisplayName={user?.displayName ?? null}
              activeGroupsCount={activeGroups.length}
              onSearchChange={setSearch}
            />

            <Stack spacing={1.5} sx={{ mt: 1.5 }}>
              <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
                <ModifierSectionHeading
                  title={t('gameModifiers.activeTitle')}
                  count={activeGroups.length}
                  description={t('gameModifiers.activeSectionHint')}
                />

                {activeGroups.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
                    {hasSearch ? t('gameModifiers.emptySearch') : t('gameModifiers.activeEmpty')}
                  </Typography>
                ) : (
                  <List disablePadding component="ul" sx={{ mt: 0.55 }}>
                    {activeGroups.map((group) => {
                      const definition = availableDefinitionsById.get(group.modifierId)

                      return (
                        <ActiveModifierRow
                          key={group.modifierId}
                          group={group}
                          {...(definition ? { definition } : {})}
                        />
                      )
                    })}
                  </List>
                )}
              </SectionCard>

              <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
                <ModifierSectionHeading
                  title={t('gameModifiers.availableTitle')}
                  count={filteredAvailableModifiers.length}
                  description={t('gameModifiers.availableSectionHint')}
                />

                {availableGroups.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
                    {hasSearch ? t('gameModifiers.emptySearch') : t('gameModifiers.availableEmpty')}
                  </Typography>
                ) : (
                  <Stack spacing={1.25} sx={{ mt: 0.7 }}>
                    {availableGroups.map((group) => (
                      <CategorySection key={group.category} category={group.category}>
                        <Stack spacing={0.35}>
                          <Stack
                            direction="row"
                            spacing={0.8}
                            justifyContent="space-between"
                            alignItems="center"
                            flexWrap="wrap"
                            useFlexGap
                          >
                            <Typography
                              component="h3"
                              variant="overline"
                              color="text.secondary"
                              sx={{ fontWeight: 700 }}
                            >
                              {getCategoryLabel(t, group.category)}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {t('gameModifiers.categoryCountLabel', {
                                count: group.items.length,
                              })}
                            </Typography>
                          </Stack>

                          <List disablePadding component="ul">
                            {group.items.map((availability) => (
                              <AvailableModifierRow
                                key={availability.modifier.id}
                                availability={availability}
                                isBusy={activation.isActivating}
                                isPending={
                                  activation.pendingModifierId === availability.modifier.id
                                }
                                onActivate={activation.activate}
                              />
                            ))}
                          </List>
                        </Stack>
                      </CategorySection>
                    ))}
                  </Stack>
                )}
              </SectionCard>
            </Stack>
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
  userDisplayName,
  activeGroupsCount,
  onSearchChange,
}: {
  state: GameModifierState
  search: string
  userDisplayName: string | null
  activeGroupsCount: number
  onSearchChange: (value: string) => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      component="section"
      aria-label={t('gameModifiers.summaryTitle')}
      sx={(theme) => ({
        mt: 1.25,
        border: `1px solid ${alpha(theme.palette.primary.main, 0.34)}`,
        borderRadius: '12px',
        backgroundColor: alpha(theme.palette.background.paper, 0.5),
        px: { xs: 1.25, sm: 1.5 },
        py: { xs: 1.15, sm: 1.25 },
      })}
    >
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={{ xs: 1.1, sm: 0 }}
        divider={
          <Divider orientation="vertical" flexItem sx={{ display: { xs: 'none', sm: 'block' } }} />
        }
        alignItems={{ xs: 'stretch', sm: 'center' }}
      >
        <Stack spacing={0.2} sx={{ minWidth: { sm: 170 }, pr: { sm: 1.5 } }}>
          <Typography variant="overline" color="text.secondary">
            {t('gameModifiers.currentUserLabel')}
          </Typography>
          <Typography variant="subtitle2" noWrap>
            {userDisplayName ?? t('gameModifiers.currentUserFallback')}
          </Typography>
        </Stack>

        <StatusMetric
          label={t('gameModifiers.summaryAvailablePoints')}
          value={t('gameModifiers.myPointsValue', { points: state.availableQuizPoints })}
        />
        <StatusMetric
          label={t('gameModifiers.summarySpentPoints')}
          value={t('gameModifiers.myPointsValue', { points: state.spentQuizPoints })}
        />
        <StatusMetric label={t('gameModifiers.activeTitle')} value={String(activeGroupsCount)} />
        <StatusMetric
          label={t('gameModifiers.summaryTitle')}
          value={
            state.isOrderingOpen
              ? t('gameModifiers.orderingOpen')
              : t('gameModifiers.orderingClosed')
          }
          tone={state.isOrderingOpen ? 'success' : 'warning'}
        />
      </Stack>

      <FormTextField
        value={search}
        label={t('gameModifiers.searchLabel')}
        onChange={(event) => onSearchChange(event.target.value)}
        sx={{ mt: 1.15 }}
      />
    </Box>
  )
}

function StatusMetric({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: string
  tone?: 'default' | 'success' | 'warning'
}) {
  return (
    <Stack
      spacing={0.1}
      sx={(theme) => ({
        minWidth: { sm: 120 },
        flex: 1,
        px: { sm: 1.25 },
        borderLeft: {
          xs: 'none',
          sm: `1px solid ${alpha(theme.palette.divider, 0.48)}`,
        },
      })}
    >
      <Typography variant="caption" color="text.secondary" noWrap>
        {label}
      </Typography>
      <Typography
        variant="body2"
        color={tone === 'default' ? 'text.primary' : `${tone}.main`}
        sx={{ fontWeight: 700 }}
        noWrap
      >
        {value}
      </Typography>
    </Stack>
  )
}

function ModifierSectionHeading({
  title,
  count,
  description,
}: {
  title: string
  count: number
  description: string
}) {
  const { t } = useTranslation()

  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={0.75}
      justifyContent="space-between"
      alignItems={{ xs: 'flex-start', sm: 'center' }}
    >
      <Box>
        <Typography variant="subtitle1">{title}</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.2 }}>
          {description}
        </Typography>
      </Box>
      <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'nowrap' }}>
        {t('gameModifiers.categoryCountLabel', { count })}
      </Typography>
    </Stack>
  )
}

function ModifierIcon({ emoji }: { emoji: string | null | undefined }) {
  return (
    <Box
      aria-hidden="true"
      sx={(theme) => ({
        width: 32,
        height: 32,
        borderRadius: '8px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: alpha(theme.palette.background.paper, 0.52),
        border: `1px solid ${alpha(theme.palette.divider, 0.68)}`,
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
          minHeight: 24,
          borderRadius: '999px',
          border: `1px solid ${alpha(accent, tone === 'default' ? 0.74 : 0.52)}`,
          backgroundColor:
            tone === 'default' ? alpha(theme.palette.background.paper, 0.42) : alpha(accent, 0.12),
          px: 0.75,
          typography: 'caption',
          color: tone === 'default' ? 'text.primary' : `${tone}.light`,
          fontWeight: 700,
          lineHeight: 1,
        }
      }}
    >
      {label}
    </Box>
  )
}

function ActiveModifierRow({
  group,
  definition,
}: {
  group: ReturnType<typeof groupActiveGameModifiers>[number]
  definition?: GameModifierDefinition
}) {
  const { t } = useTranslation()

  return (
    <Box
      component="li"
      sx={(theme) => ({
        listStyle: 'none',
        py: 1.05,
        '&:not(:last-child)': {
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.36)}`,
        },
      })}
    >
      <Stack direction="row" spacing={1} alignItems="flex-start">
        <ModifierIcon emoji={definition?.iconEmoji} />
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={{ xs: 0.35, sm: 0.8 }}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
          >
            <Typography variant="subtitle2">{group.modifierName}</Typography>
            <InlineMetaPill label={t('gameModifiers.activeTag')} tone="success" />
          </Stack>

          {definition?.description ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
              {definition.description}
            </Typography>
          ) : null}

          <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.65 }}>
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

          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.6 }}>
            {t('gameModifiers.activeGroupLatest', {
              player: group.lastActivatedByDisplayName,
              time: new Date(group.lastActivatedAtUtc).toLocaleTimeString(),
            })}
          </Typography>

          <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.25 }}>
            {group.activators.map((activator) => (
              <InlineMetaPill
                key={activator.userId}
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
        </Box>
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
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const definition = availability.modifier
  const roundSummaryMeta = deriveModifierRoundSummaryMeta(definition)
  const hasLimit = availability.limit != null
  const limitReached = hasLimit && availability.activationsCount >= (availability.limit ?? 0)
  const hasConflicts = definition.conflictingModifierIds.length > 0
  const detailsId = `modifier-details-${definition.id}`

  return (
    <Box
      component="li"
      sx={(theme) => ({
        listStyle: 'none',
        py: 1,
        '&:not(:last-child)': {
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.32)}`,
        },
      })}
    >
      <Stack spacing={0.65}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'flex-start' }}
        >
          <Stack direction="row" spacing={0.9} sx={{ minWidth: 0, flex: 1 }}>
            <ModifierIcon emoji={definition.iconEmoji} />
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={{ xs: 0.25, md: 0.8 }}
                alignItems={{ xs: 'flex-start', md: 'center' }}
              >
                <Typography variant="subtitle2">{definition.name}</Typography>
                <Typography variant="body2" color="warning.light" sx={{ fontWeight: 700 }}>
                  {t('gameModifiers.costLabel', { cost: definition.activationCost })}
                </Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.35 }}>
                {definition.description}
              </Typography>
            </Box>
          </Stack>

          <Box
            sx={{
              width: { xs: '100%', sm: 192 },
              flexShrink: 0,
              display: 'flex',
              justifyContent: { xs: 'stretch', sm: 'flex-end' },
            }}
          >
            {availability.canActivate ? (
              <AppButton
                tone="primary"
                size="small"
                fullWidth
                disabled={isBusy}
                onClick={() => onActivate(definition.id)}
                sx={{ minHeight: 44 }}
              >
                {isPending ? t('gameModifiers.activatePending') : t('gameModifiers.activateAction')}
              </AppButton>
            ) : (
              <BlockedReasonPlaque blockedReason={availability.blockedReason} />
            )}
          </Box>
        </Stack>

        <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap alignItems="center">
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
          {availability.isActive ? (
            <InlineMetaPill label={t('gameModifiers.activeTag')} tone="success" />
          ) : null}
          <AppButton
            tone="ghost"
            size="small"
            aria-expanded={isDetailsOpen}
            aria-controls={detailsId}
            onClick={() => setIsDetailsOpen((current) => !current)}
            sx={{ minHeight: 44, px: 0.75 }}
          >
            {isDetailsOpen
              ? t('gameModifiers.hideDetailsAction')
              : t('gameModifiers.detailsAction')}
          </AppButton>
        </Stack>

        <Collapse in={isDetailsOpen} timeout="auto" unmountOnExit>
          <Stack
            id={detailsId}
            spacing={0.55}
            sx={(theme) => ({
              borderLeft: `2px solid ${alpha(theme.palette.primary.main, 0.44)}`,
              pl: 1,
              pt: 0.3,
            })}
          >
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
              <InlineMetaPill
                label={t(`gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`)}
              />
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
            <Typography variant="caption" color="text.secondary">
              {t('gameModifiers.detailsHint')}
            </Typography>
          </Stack>
        </Collapse>
      </Stack>
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
      role="status"
      aria-label={
        blockedReason != null
          ? t(`gameModifiers.blockedReasons.${blockedReason}`)
          : t('gameModifiers.unavailableAction')
      }
      sx={(theme) => {
        const accent =
          blockedReason === 'limit_reached'
            ? theme.palette.error.main
            : blockedReason === 'insufficient_points'
              ? theme.palette.warning.main
              : theme.palette.info.main

        return {
          width: '100%',
          minHeight: 44,
          px: 0.9,
          py: 0.65,
          borderRadius: '10px',
          border: `1px solid ${alpha(accent, 0.54)}`,
          backgroundColor: alpha(accent, 0.12),
          display: 'flex',
          alignItems: 'center',
          gap: 0.65,
        }
      }}
    >
      <Box
        aria-hidden="true"
        sx={(theme) => {
          const accent =
            blockedReason === 'limit_reached'
              ? theme.palette.error.main
              : blockedReason === 'insufficient_points'
                ? theme.palette.warning.main
                : theme.palette.info.main

          return {
            width: 22,
            height: 22,
            borderRadius: '999px',
            border: `1px solid ${alpha(accent, 0.64)}`,
            backgroundColor: alpha(theme.palette.background.paper, 0.44),
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
      <Typography sx={{ textAlign: 'left', fontSize: '0.82rem', fontWeight: 700, lineHeight: 1.2 }}>
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
      component="section"
      aria-label={category}
      sx={(theme) => {
        const accent =
          category === 'preparation'
            ? theme.palette.info.main
            : category === 'round'
              ? theme.palette.success.main
              : theme.palette.warning.main

        return {
          borderTop: `1px solid ${alpha(accent, 0.42)}`,
          pt: 0.85,
        }
      }}
    >
      {children}
    </Box>
  )
}

const CATEGORY_LABEL_KEYS = {
  preparation: 'gameModifiers.categories.preparation',
  round: 'gameModifiers.categories.round',
  result: 'gameModifiers.categories.result',
} as const

function getCategoryLabel(
  t: ReturnType<typeof useTranslation>['t'],
  category: GameModifierAvailability['modifier']['category'],
): string {
  const translate = t as unknown as (key: string) => string
  return translate(CATEGORY_LABEL_KEYS[category])
}
