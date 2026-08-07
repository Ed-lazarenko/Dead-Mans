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

type ModifierDialogState =
  | { mode: 'create'; modifier: undefined }
  | { mode: 'edit'; modifier: GameModifierDefinition }

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
  const { t } = useTranslation()
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
          matchesModifierSearch(modifier, search, [
            t(`gameCatalog.modifiers.categories.${modifier.category}`),
            t(`gameCatalog.modifiers.mechanics.${modifier.mechanicType}`),
            t(
              `gameCatalog.modifiers.roundSummaryType.${
                deriveModifierRoundSummaryMeta(modifier).type
              }`,
            ),
            modifier.requiresHostControl ? t('gameCatalog.modifiers.hostControlBadge') : '',
          ]) &&
          matchesCategory(modifier, selectedCategory) &&
          matchesRoundSummaryType(modifier, selectedRoundSummaryType),
      ),
    [catalogQuery.data, search, selectedCategory, selectedRoundSummaryType, t],
  )
  const createMutation = useMutation(createGameModifierMutationOptions(queryClient))
  const updateMutation = useMutation(updateGameModifierMutationOptions(queryClient))
  const deleteMutation = useMutation(deleteGameModifierMutationOptions(queryClient))

  const [dialog, setDialog] = useState<ModifierDialogState | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<GameModifierDefinition | null>(null)

  const openCreate = () => setDialog({ mode: 'create', modifier: undefined })
  const openEdit = (modifier: GameModifierDefinition) => setDialog({ mode: 'edit', modifier })
  const closeDialog = () => setDialog(null)

  const submitModifier = async (request: CreateGameModifierRequest) => {
    if (dialog?.mode === 'edit') {
      await updateMutation.mutateAsync({
        modifierId: dialog.modifier.id,
        request: {
          name: request.name,
          description: request.description,
          category: request.category,
          requiresHostControl: request.requiresHostControl,
          mechanicType: request.mechanicType,
          scoringType: request.scoringType ?? null,
          activationCost: request.activationCost,
          activationLimit: request.activationLimit,
          effect: request.effect,
          conflictingModifierIds: request.conflictingModifierIds ?? [],
          defaultLimitPerGame: request.defaultLimitPerGame ?? null,
          iconEmoji: request.iconEmoji ?? null,
          activationCommand: request.activationCommand ?? null,
        },
      })
    } else {
      await createMutation.mutateAsync(request)
    }
    closeDialog()
  }

  const requestDelete = (modifier: GameModifierDefinition) => setDeleteTarget(modifier)
  const cancelDelete = () => setDeleteTarget(null)
  const confirmDelete = async () => {
    if (!deleteTarget) {
      return
    }
    await deleteMutation.mutateAsync(deleteTarget.id)
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
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting: deleteMutation.isPending,
  }
}
