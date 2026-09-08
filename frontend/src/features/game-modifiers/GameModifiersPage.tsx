import { Box } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameBoardCell,
  GameModifierActivation,
  GameModifierState,
} from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import {
  AppToast,
  AsyncSection,
  ConfirmDialog,
  PageShell,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { GameBoardCardPreviewDialog } from '../game-board/ui/GameBoardCardPreviewDialog.tsx'
import { formatTeamNameWithFallback } from '../game-registration/model/team-name.ts'
import { activeGameRoundQueryOptions } from '../game-rounds/api/game-rounds-queries.ts'
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
import { ActiveModifiersSection } from './ui/ActiveModifiersSection.tsx'
import { AvailableModifiersSection } from './ui/AvailableModifiersSection.tsx'
import { ModifierRuntimePanel } from './ui/ModifierRuntimePanel.tsx'
import { ModifierStatusBar } from './ui/ModifierStatusBar.tsx'
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

    for (const activeModifier of state?.activeModifiers ?? []) {
      names.set(activeModifier.modifierId, activeModifier.modifierName)
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
      <SectionHeader title={t('common.entities.modifiers')} />

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
              <ActiveModifiersSection
                groups={activeGroups}
                activationsCount={state.activeModifiers.length}
                definitionsById={availableDefinitionsById}
                currentUserId={user?.id ?? null}
                canSelfCancel={state.isOrderingOpen}
                isCancelling={selfCancelMutation.isPending}
                hasSearch={hasSearch}
                onSelfCancel={setSelfCancelToConfirm}
              />
              <AvailableModifiersSection
                groups={availableGroups}
                modifierNamesById={modifierNamesById}
                activeModifierIds={activeModifierIds}
                hasSearch={hasSearch}
                isBusy={activation.isActivating}
                pendingModifierId={activation.pendingModifierId}
                onActivate={setActivationToConfirmId}
              />
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
