import { Box, Collapse, List, Stack, Tooltip, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardCell,
  GameModifierActivation,
  GameModifierAvailability,
  GameModifierDefinition,
  GameModifierState,
} from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import {
  AppButton,
  AppToast,
  AsyncSection,
  ConfirmDialog,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { GameBoardCardPreviewDialog } from '../game-board/ui/GameBoardCardPreviewDialog.tsx'
import { formatTeamNameWithFallback } from '../game-registration/model/team-name.ts'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
import { AdminModifierPanel } from './AdminModifierPanel.tsx'
import {
  gameModifierQueryKeys,
  gameModifierStateQueryOptions,
} from './api/game-modifier-queries.ts'
import { selfCancelGameModifierActivation } from './api/game-modifiers-api.ts'
import {
  groupActiveGameModifiers,
  groupAvailableGameModifiers,
} from './model/game-modifier-groups.ts'
import { deriveModifierRoundSummaryMeta } from './model/modifier-round-summary.ts'
import { matchesModifierSearch } from './model/modifier-search.ts'
import { ModifierStatusBar } from './ui/ModifierStatusBar.tsx'
import { ModifierRuntimePanel } from './ui/ModifierRuntimePanel.tsx'
import { useActivateGameModifier } from './use-activate-game-modifier.ts'

export function GameModifiersPage() {
  const { t, i18n } = useTranslation()
  const locale = i18n.resolvedLanguage
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const stateQuery = useQuery(gameModifierStateQueryOptions)
  const snapshotQuery = useQuery(currentGameBoardQueryOptions)
  const activeRoundQuery = useQuery(activeGameRoundQueryOptions)
  const activation = useActivateGameModifier()
  const [search, setSearch] = useState('')
  const [activationToConfirmId, setActivationToConfirmId] = useState<string | null>(null)
  const [selfCancelToConfirm, setSelfCancelToConfirm] = useState<GameModifierActivation | null>(
    null,
  )
  const [selfCancelToastMessage, setSelfCancelToastMessage] = useState<string | null>(null)
  const [previewCell, setPreviewCell] = useState<GameBoardCell | null>(null)
  const state: GameModifierState | null = stateQuery.data ?? null
  const snapshot = snapshotQuery.data ?? null
  const activeRound = activeRoundQuery.data ?? null
  const activeCard = activeRound
    ? (snapshot?.cells.find((cell) => cell.id === activeRound.cellId) ?? null)
    : null
  const isEmpty = !stateQuery.isLoading && !stateQuery.isError && state == null
  const selfCancelMutation = useMutation({
    mutationFn: (item: GameModifierActivation) =>
      selfCancelGameModifierActivation(item.activationId, item.roundVersion),
    onSuccess: () => {
      setSelfCancelToConfirm(null)
      setSelfCancelToastMessage(t('gameModifiers.selfCancelSuccess'))
      void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
      void queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey })
    },
    onError: () => {
      setSelfCancelToConfirm(null)
      setSelfCancelToastMessage(t('gameModifiers.selfCancelFailed'))
      void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
      void queryClient.invalidateQueries({ queryKey: activeGameRoundQueryOptions.queryKey })
    },
  })

  const availableDefinitionsById = useMemo(
    () => new Map(state?.availableModifiers.map((item) => [item.modifier.id, item.modifier]) ?? []),
    [state],
  )
  const modifierNamesById = useMemo(() => {
    const names = new Map(
      state?.availableModifiers.map((item) => [item.modifier.id, item.modifier.name]) ?? [],
    )

    for (const activation of state?.activeModifiers ?? []) {
      names.set(activation.modifierId, activation.modifierName)
    }

    return names
  }, [state])
  const activeModifierIds = useMemo(
    () => new Set(state?.activeModifiers.map((item) => item.modifierId) ?? []),
    [state?.activeModifiers],
  )
  const filteredAvailableModifiers = useMemo(
    () =>
      (state?.availableModifiers ?? []).filter((availability) =>
        matchesModifierSearch(
          availability.modifier,
          search,
          [
            t(`common.modifiers.categories.${availability.modifier.category}`),
            t(`gameCatalog.modifiers.wizard.kinds.${availability.modifier.behaviorV2.kind}`),
            t(
              `gameCatalog.modifiers.roundSummaryType.${
                deriveModifierRoundSummaryMeta(availability.modifier).type
              }`,
            ),
            availability.modifier.behaviorV2.requiresHostMonitoring
              ? t('gameModifiers.hostControlTag')
              : '',
          ],
          locale,
        ),
      ),
    [locale, search, state?.availableModifiers, t],
  )
  const availableGroups = state
    ? groupAvailableGameModifiers(filteredAvailableModifiers, locale)
    : []
  const activeGroups = useMemo(() => {
    if (!state) {
      return []
    }

    return groupActiveGameModifiers(state.activeModifiers, locale).filter((group) => {
      const definition = availableDefinitionsById.get(group.modifierId)
      if (!definition) {
        return group.modifierName
          .toLocaleLowerCase(locale)
          .includes(search.trim().toLocaleLowerCase(locale))
      }

      return matchesModifierSearch(
        definition,
        search,
        [
          t(`common.modifiers.categories.${definition.category}`),
          t(`gameCatalog.modifiers.wizard.kinds.${definition.behaviorV2.kind}`),
          t(
            `gameCatalog.modifiers.roundSummaryType.${deriveModifierRoundSummaryMeta(definition).type}`,
          ),
          definition.behaviorV2.requiresHostMonitoring ? t('gameModifiers.hostControlTag') : '',
        ],
        locale,
      )
    })
  }, [availableDefinitionsById, locale, search, state, t])
  const hasSearch = search.trim().length > 0
  const activationToConfirm = activationToConfirmId
    ? (availableDefinitionsById.get(activationToConfirmId) ?? null)
    : null
  const activeRoundActivationCount = state?.activeModifiers.length ?? 0
  const hasAdminPanel = user?.roles.includes('admin') ?? false
  const currentTeamLabel = activeRoundQuery.isLoading
    ? t('gameModifiers.summaryContextLoading')
    : activeRoundQuery.isError
      ? t('gameModifiers.summaryContextUnavailable')
      : activeRound
        ? formatTeamNameWithFallback(
            activeRound.teamName,
            t('common.teamWithSlot', { slot: activeRound.teamSlotIndex }),
          )
        : t('gameModifiers.summaryNoCurrentTeam')
  const currentTeamParticipantNames =
    activeRound?.participants.map((participant) => participant.displayName) ?? []
  const currentTeamParticipantsEmptyLabel = activeRoundQuery.isLoading
    ? t('gameModifiers.summaryContextLoading')
    : activeRoundQuery.isError
      ? t('gameModifiers.summaryContextUnavailable')
      : t('gameModifiers.summaryNoParticipants')
  const activeCardLabel =
    activeRoundQuery.isLoading || (activeRound !== null && snapshotQuery.isLoading)
      ? t('gameModifiers.summaryContextLoading')
      : activeRoundQuery.isError || (activeRound !== null && snapshotQuery.isError)
        ? t('gameModifiers.summaryContextUnavailable')
        : activeRound
          ? activeCard?.title?.trim() || t('gameModifiers.summaryUntitledCard')
          : t('gameModifiers.summaryNoActiveCard')

  return (
    <PageShell
      data-testid="game-modifiers-page"
      sx={{
        maxWidth: 'none',
        width: { xs: '100%', md: hasAdminPanel ? 'calc(100% - 72px)' : '100%' },
        ml: { xs: 0, md: 'auto' },
        mr: { xs: 0, md: hasAdminPanel ? 9 : 0 },
        px: { xs: 0, sm: 0 },
      }}
    >
      <SectionHeader
        title={t('common.entities.modifiers')}
        actions={state && hasAdminPanel ? <AdminModifierPanel /> : null}
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
              onSearchChange={setSearch}
              currentTeamLabel={currentTeamLabel}
              currentTeamParticipantNames={currentTeamParticipantNames}
              currentTeamParticipantsEmptyLabel={currentTeamParticipantsEmptyLabel}
              activeCardLabel={activeCardLabel}
              canOpenActiveCard={activeCard !== null}
              onOpenActiveCard={() => {
                if (activeCard) {
                  setPreviewCell(activeCard)
                }
              }}
            />

            <ModifierRuntimePanel
              key={`${activeRound?.roundId ?? 'none'}:${activeRound?.roundVersion ?? 0}:${activeRound?.serverNowUtc ?? 'unsynced'}`}
              round={activeRound}
              isOffline={activeRoundQuery.isError || snapshotQuery.isError}
            />

            <Box
              sx={{
                mt: 1.5,
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                gap: 1.5,
                alignItems: 'start',
              }}
            >
              <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
                <ModifierSectionHeading
                  title={t('gameModifiers.activeTitle')}
                  count={activeRoundActivationCount}
                />

                {activeGroups.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
                    {hasSearch ? t('common.modifiers.emptySearch') : t('gameModifiers.activeEmpty')}
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
                          currentUserId={user?.id ?? null}
                          canSelfCancel={state.isOrderingOpen}
                          isCancelling={selfCancelMutation.isPending}
                          onSelfCancel={setSelfCancelToConfirm}
                        />
                      )
                    })}
                  </List>
                )}
              </SectionCard>

              <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
                <ModifierSectionHeading title={t('gameModifiers.availableTitle')} />

                {availableGroups.length === 0 ? (
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
                    {hasSearch
                      ? t('common.modifiers.emptySearch')
                      : t('gameModifiers.availableEmpty')}
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
                            <Typography component="h3" variant="subtitle2" sx={{ fontWeight: 850 }}>
                              {getCategoryLabel(t, group.category)}
                            </Typography>
                            <ModifierCountBadge count={group.items.length} />
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
                                onActivate={setActivationToConfirmId}
                                conflictingModifierNames={availability.modifier.conflictingModifierIds.map(
                                  (modifierId) => modifierNamesById.get(modifierId) ?? modifierId,
                                )}
                                activeConflictingModifierNames={availability.modifier.conflictingModifierIds
                                  .filter((modifierId) => activeModifierIds.has(modifierId))
                                  .map(
                                    (modifierId) => modifierNamesById.get(modifierId) ?? modifierId,
                                  )}
                              />
                            ))}
                          </List>
                        </Stack>
                      </CategorySection>
                    ))}
                  </Stack>
                )}
              </SectionCard>
            </Box>
          </>
        ) : null}
      </AsyncSection>

      <ConfirmDialog
        open={activationToConfirm !== null}
        title={t('gameModifiers.activationConfirmTitle')}
        description={
          activationToConfirm
            ? t('gameModifiers.activationConfirmDescription', {
                modifier: activationToConfirm.name,
                cost: activationToConfirm.activationCost,
              })
            : ''
        }
        confirmLabel={t('gameModifiers.activateAction')}
        cancelLabel={t('gameModifiers.activationConfirmCancel')}
        onClose={() => setActivationToConfirmId(null)}
        onConfirm={() => {
          if (!activationToConfirmId) {
            return
          }

          const modifierId = activationToConfirmId
          setActivationToConfirmId(null)
          activation.activate(modifierId)
        }}
      />

      <ConfirmDialog
        open={selfCancelToConfirm !== null}
        title={t('gameModifiers.selfCancelConfirmTitle')}
        description={
          selfCancelToConfirm
            ? t('gameModifiers.selfCancelConfirmDescription', {
                modifier: selfCancelToConfirm.modifierName,
                cost: selfCancelToConfirm.activationCost,
              })
            : ''
        }
        confirmLabel={t('gameModifiers.selfCancelAction')}
        cancelLabel={t('gameModifiers.activationConfirmCancel')}
        confirmTone="danger"
        isBusy={selfCancelMutation.isPending}
        onClose={() => setSelfCancelToConfirm(null)}
        onConfirm={() => {
          if (selfCancelToConfirm) {
            selfCancelMutation.mutate(selfCancelToConfirm)
          }
        }}
      />

      <GameBoardCardPreviewDialog
        cell={previewCell}
        playResult={{ round: null, isLoading: false, isError: false }}
        onClose={() => setPreviewCell(null)}
      />

      <AppToast
        message={activation.toastMessage}
        onClose={activation.dismissToast}
        severity="info"
        autoHideDuration={3000}
      />
      <AppToast
        message={selfCancelToastMessage}
        onClose={() => setSelfCancelToastMessage(null)}
        severity={selfCancelMutation.isError ? 'error' : 'info'}
        autoHideDuration={3000}
      />
    </PageShell>
  )
}

function ModifierSectionHeading({ title, count }: { title: string; count?: number }) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={0.75}
      justifyContent="space-between"
      alignItems={{ xs: 'flex-start', sm: 'center' }}
    >
      <Typography variant="subtitle1">{title}</Typography>
      {count === undefined ? null : <ModifierCountBadge count={count} />}
    </Stack>
  )
}

function ModifierCountBadge({ count }: { count: number }) {
  const { t } = useTranslation()

  return (
    <Box
      component="span"
      sx={(theme) => ({
        display: 'inline-flex',
        alignItems: 'center',
        minHeight: 28,
        borderRadius: '999px',
        border: `1px solid ${alpha(theme.palette.primary.main, 0.56)}`,
        backgroundColor: alpha(theme.palette.primary.main, 0.12),
        color: 'text.primary',
        px: 1,
        typography: 'caption',
        fontWeight: 850,
        whiteSpace: 'nowrap',
      })}
    >
      {t('gameModifiers.categoryCountLabel', { count })}
    </Box>
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
  currentUserId,
  canSelfCancel,
  isCancelling,
  onSelfCancel,
}: {
  group: ReturnType<typeof groupActiveGameModifiers>[number]
  definition?: GameModifierDefinition
  currentUserId: string | null
  canSelfCancel: boolean
  isCancelling: boolean
  onSelfCancel: (activation: GameModifierActivation) => void
}) {
  const { t } = useTranslation()
  const ownActivations = currentUserId
    ? group.activations.filter((item) => item.activatedByUserId === currentUserId)
    : []

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

          <Box
            sx={(theme) => ({
              mt: 0.7,
              borderLeft: `2px solid ${alpha(theme.palette.primary.main, 0.48)}`,
              backgroundColor: alpha(theme.palette.primary.main, 0.055),
              borderRadius: '0 8px 8px 0',
              px: 0.8,
              py: 0.65,
            })}
          >
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 750 }}>
              {t('gameModifiers.activatorsLabel')}
            </Typography>
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.4 }}>
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

          {canSelfCancel && ownActivations.length > 0 ? (
            <Stack spacing={0.5} sx={{ mt: 0.75 }}>
              {ownActivations.map((item) => (
                <AppButton
                  key={item.activationId}
                  tone="dangerSecondary"
                  size="small"
                  disabled={isCancelling}
                  onClick={() => onSelfCancel(item)}
                >
                  {t('gameModifiers.selfCancelActionWithCost', {
                    cost: item.activationCost,
                  })}
                </AppButton>
              ))}
            </Stack>
          ) : null}
        </Box>
      </Stack>
    </Box>
  )
}

interface AvailableModifierRowProps {
  availability: GameModifierAvailability
  isBusy: boolean
  isPending: boolean
  conflictingModifierNames: readonly string[]
  activeConflictingModifierNames: readonly string[]
  onActivate: (modifierId: string) => void
}

function AvailableModifierRow({
  availability,
  isBusy,
  isPending,
  conflictingModifierNames,
  activeConflictingModifierNames,
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
  const blockedReasonLabel =
    availability.blockedReason != null
      ? t(`gameModifiers.blockedReasonLabels.${availability.blockedReason}`)
      : t('gameModifiers.unavailableAction')
  const blockedReasonTooltip =
    availability.blockedReason === 'conflict_active' && activeConflictingModifierNames.length > 0
      ? t('gameModifiers.blockedByConflicts', {
          names: activeConflictingModifierNames.join(', '),
        })
      : availability.blockedReason != null
        ? t(`gameModifiers.blockedReasons.${availability.blockedReason}`)
        : t('gameModifiers.unavailableAction')

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
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{
                  mt: 0.35,
                  ...(isDetailsOpen
                    ? {}
                    : {
                        display: '-webkit-box',
                        WebkitBoxOrient: 'vertical',
                        WebkitLineClamp: 2,
                        overflow: 'hidden',
                      }),
                }}
              >
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
                sx={{
                  height: 32,
                  minHeight: 32,
                  borderRadius: '8px',
                  fontSize: '0.75rem',
                  lineHeight: 1.15,
                }}
              >
                {isPending ? t('gameModifiers.activatePending') : t('gameModifiers.activateAction')}
              </AppButton>
            ) : availability.blockedReason === 'ordering_closed' ? (
              <Tooltip
                title={blockedReasonTooltip}
                arrow
                describeChild
                enterDelay={150}
                enterTouchDelay={0}
              >
                <Box
                  component="span"
                  tabIndex={0}
                  sx={{ display: 'block', width: '100%', cursor: 'help' }}
                >
                  <AppButton
                    tone="primary"
                    size="small"
                    fullWidth
                    disabled
                    sx={{
                      height: 32,
                      minHeight: 32,
                      borderRadius: '8px',
                      pointerEvents: 'none',
                      fontSize: '0.75rem',
                      lineHeight: 1.15,
                    }}
                  >
                    <Stack
                      component="span"
                      alignItems="center"
                      justifyContent="center"
                      sx={{ position: 'relative', width: '100%', minHeight: 14 }}
                    >
                      <Box component="span" sx={{ width: '100%', textAlign: 'center' }}>
                        {blockedReasonLabel}
                      </Box>
                      <Box
                        component="span"
                        aria-hidden="true"
                        sx={{
                          position: 'absolute',
                          top: '50%',
                          left: 0,
                          transform: 'translateY(-50%)',
                          display: 'inline-flex',
                          width: 14,
                          height: 14,
                          alignItems: 'center',
                          justifyContent: 'center',
                          border: '1px solid currentColor',
                          borderRadius: '50%',
                          fontSize: '0.62rem',
                          fontWeight: 900,
                          lineHeight: 1,
                        }}
                      >
                        ?
                      </Box>
                    </Stack>
                  </AppButton>
                </Box>
              </Tooltip>
            ) : (
              <BlockedReasonPlaque
                blockedReason={availability.blockedReason}
                label={blockedReasonLabel}
                tooltip={blockedReasonTooltip}
              />
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
              {definition.behaviorV2.requiresHostMonitoring ? (
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
            {conflictingModifierNames.length > 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.conflictsListLabel', {
                  names: conflictingModifierNames.join(', '),
                })}
              </Typography>
            ) : null}
          </Stack>
        </Collapse>
      </Stack>
    </Box>
  )
}

function BlockedReasonPlaque({
  blockedReason,
  label,
  tooltip,
}: {
  blockedReason: GameModifierAvailability['blockedReason']
  label: string
  tooltip: string
}) {
  return (
    <Tooltip title={tooltip} arrow describeChild enterDelay={150} enterTouchDelay={0}>
      <Box
        role="status"
        aria-label={tooltip}
        tabIndex={0}
        sx={(theme) => {
          const accent =
            blockedReason === 'limit_reached' || blockedReason === 'active_team_member'
              ? theme.palette.error.main
              : blockedReason === 'insufficient_points'
                ? theme.palette.warning.main
                : theme.palette.info.main

          return {
            '--blocked-reason-accent': accent,
            width: '100%',
            height: 32,
            minHeight: 32,
            px: 0.7,
            borderRadius: '8px',
            border: `1px solid ${alpha(accent, 0.46)}`,
            backgroundColor: alpha(accent, 0.08),
            cursor: 'help',
            position: 'relative',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            '& .blocked-reason-help': {
              color: 'var(--blocked-reason-accent)',
              borderColor: alpha(accent, 0.72),
            },
          }
        }}
      >
        <Typography
          variant="caption"
          sx={{
            display: 'block',
            width: '100%',
            px: 2,
            textAlign: 'center',
            fontSize: '0.7rem',
            fontWeight: 700,
            lineHeight: 1.15,
          }}
        >
          {label}
        </Typography>
        <Box
          component="span"
          className="blocked-reason-help"
          aria-hidden="true"
          sx={{
            position: 'absolute',
            top: '50%',
            left: 6,
            transform: 'translateY(-50%)',
            display: 'inline-flex',
            width: 14,
            height: 14,
            alignItems: 'center',
            justifyContent: 'center',
            border: '1px solid',
            borderRadius: '50%',
            fontSize: '0.62rem',
            fontWeight: 900,
            lineHeight: 1,
          }}
        >
          ?
        </Box>
      </Box>
    </Tooltip>
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
          border: `1px solid ${alpha(accent, 0.42)}`,
          borderLeftWidth: 3,
          borderRadius: '10px',
          backgroundColor: alpha(accent, 0.045),
          px: { xs: 0.9, sm: 1.1 },
          py: 0.9,
        }
      }}
    >
      {children}
    </Box>
  )
}

const CATEGORY_LABEL_KEYS = {
  preparation: 'common.modifiers.categories.preparation',
  round: 'common.modifiers.categories.round',
  result: 'common.modifiers.categories.result',
} as const

function getCategoryLabel(
  t: ReturnType<typeof useTranslation>['t'],
  category: GameModifierAvailability['modifier']['category'],
): string {
  const translate = t as unknown as (key: string) => string
  return translate(CATEGORY_LABEL_KEYS[category])
}
