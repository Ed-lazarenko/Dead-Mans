import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../shared/api/contracts/index.ts'
import {
  gameModifierCatalogQueryOptions,
  modifierCategoryCodes,
  modifierRoundSummaryTypes,
  type ModifierCategoryCode,
  type ModifierRoundSummaryType,
} from '../game-modifiers/index.ts'
import { deriveModifierRoundSummaryMeta } from '../game-modifiers/model/modifier-round-summary.ts'
import { matchesModifierSearch } from '../game-modifiers/model/modifier-search.ts'
import {
  createGameModifierMutationOptions,
  deleteGameModifierMutationOptions,
  updateGameModifierMutationOptions,
} from './api/catalog-modifiers-mutations.ts'
import { isModifierRevisionStaleError } from './model/catalog-error.ts'

type ModifierDialogState =
  { mode: 'create'; modifier: undefined } | { mode: 'edit'; modifier: GameModifierDefinition }

function matchesCategory(modifier: GameModifierDefinition, category: ModifierCategoryCode | null) {
  if (!category) {
    return true
  }

  return modifier.category === category
}

function matchesRoundSummaryType(
  modifier: GameModifierDefinition,
  roundSummaryType: ModifierRoundSummaryType | null,
) {
  if (!roundSummaryType) {
    return true
  }

  return deriveModifierRoundSummaryMeta(modifier).type === roundSummaryType
}

export function useCatalogModifiers() {
  const { t, i18n } = useTranslation()
  const locale = i18n.resolvedLanguage
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [selectedCategory, setSelectedCategory] = useState<ModifierCategoryCode | null>(null)
  const [selectedRoundSummaryType, setSelectedRoundSummaryType] =
    useState<ModifierRoundSummaryType | null>(null)
  const catalogQuery = useQuery(gameModifierCatalogQueryOptions)
  const categoryCounts = useMemo(() => {
    const counts = Object.fromEntries(modifierCategoryCodes.map((type) => [type, 0])) as Record<
      ModifierCategoryCode,
      number
    >

    for (const modifier of catalogQuery.data ?? []) {
      if (modifier.category in counts) {
        counts[modifier.category as ModifierCategoryCode] += 1
      }
    }

    return counts
  }, [catalogQuery.data])
  const roundSummaryCounts = useMemo(() => {
    const counts = Object.fromEntries(modifierRoundSummaryTypes.map((type) => [type, 0])) as Record<
      ModifierRoundSummaryType,
      number
    >

    for (const modifier of catalogQuery.data ?? []) {
      counts[deriveModifierRoundSummaryMeta(modifier).type] += 1
    }

    return counts
  }, [catalogQuery.data])
  const filteredModifiers = useMemo(
    () =>
      (catalogQuery.data ?? []).filter(
        (modifier) =>
          matchesModifierSearch(
            modifier,
            search,
            [
              t(`common.modifiers.categories.${modifier.category}`),
              t(`gameCatalog.modifiers.wizard.kinds.${modifier.behaviorV2.kind}`),
              t(
                `gameCatalog.modifiers.roundSummaryType.${
                  deriveModifierRoundSummaryMeta(modifier).type
                }`,
              ),
              modifier.behaviorV2.requiresHostMonitoring
                ? t('gameCatalog.modifiers.hostControlBadge')
                : '',
            ],
            locale,
          ) &&
          matchesCategory(modifier, selectedCategory) &&
          matchesRoundSummaryType(modifier, selectedRoundSummaryType),
      ),
    [catalogQuery.data, locale, search, selectedCategory, selectedRoundSummaryType, t],
  )
  const createMutation = useMutation(createGameModifierMutationOptions(queryClient))
  const updateMutation = useMutation(updateGameModifierMutationOptions(queryClient))
  const deleteMutation = useMutation(deleteGameModifierMutationOptions(queryClient))

  const [dialog, setDialog] = useState<ModifierDialogState | null>(null)
  const [hasStaleConflict, setHasStaleConflict] = useState(false)
  const [staleLatest, setStaleLatest] = useState<GameModifierDefinition | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<GameModifierDefinition | null>(null)

  const resetStale = () => {
    setHasStaleConflict(false)
    setStaleLatest(null)
  }
  const openCreate = () => {
    resetStale()
    setDialog({ mode: 'create', modifier: undefined })
  }
  const openEdit = (modifier: GameModifierDefinition) => {
    resetStale()
    setDialog({ mode: 'edit', modifier })
  }
  const closeDialog = () => {
    resetStale()
    setDialog(null)
  }

  const submitModifier = async (request: CreateGameModifierRequest) => {
    if (dialog?.mode === 'edit') {
      try {
        await updateMutation.mutateAsync({
          modifierId: dialog.modifier.id,
          request: {
            name: request.name,
            description: request.description,
            category: request.category,
            activationCost: request.activationCost,
            activationLimit: request.activationLimit,
            conflictingModifierIds: request.conflictingModifierIds ?? [],
            iconEmoji: request.iconEmoji ?? null,
            activationCommand: request.activationCommand ?? null,
            normalizedTags: request.normalizedTags ?? [],
            behaviorV2: request.behaviorV2,
            expectedRevision: dialog.modifier.revision,
            changeNote: request.changeNote ?? null,
          },
        })
      } catch (error) {
        if (isModifierRevisionStaleError(error)) {
          setHasStaleConflict(true)
        }
        throw error
      }
    } else {
      await createMutation.mutateAsync(request)
    }
    closeDialog()
  }

  const loadLatestForComparison = async () => {
    if (dialog?.mode !== 'edit') return
    const latestCatalog = await queryClient.fetchQuery(gameModifierCatalogQueryOptions)
    setStaleLatest(latestCatalog.find((item) => item.id === dialog.modifier.id) ?? null)
  }

  const requestDelete = (modifier: GameModifierDefinition) => setDeleteTarget(modifier)
  const cancelDelete = () => setDeleteTarget(null)
  const confirmDelete = async () => {
    if (!deleteTarget) {
      return
    }
    await deleteMutation.mutateAsync({
      modifierId: deleteTarget.id,
      expectedRevision: deleteTarget.revision,
    })
    setDeleteTarget(null)
  }

  return {
    search,
    setSearch,
    selectedCategory,
    setSelectedCategory,
    categoryCounts,
    selectedRoundSummaryType,
    setSelectedRoundSummaryType,
    roundSummaryCounts,
    catalogQuery,
    filteredModifiers,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitModifier,
    isSaving: createMutation.isPending || updateMutation.isPending,
    hasStaleConflict,
    staleLatest,
    loadLatestForComparison,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting: deleteMutation.isPending,
  }
}
