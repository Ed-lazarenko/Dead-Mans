import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../shared/api/contracts/index.ts'
import { gameModifierCatalogQueryOptions } from '../game-modifiers/index.ts'
import {
  createGameModifierMutationOptions,
  deleteGameModifierMutationOptions,
  updateGameModifierMutationOptions,
} from './api/catalog-modifiers-mutations.ts'
import { modifierMechanicTypes, type ModifierMechanicType } from './model/modifier-form-schema.ts'

type ModifierDialogState =
  | { mode: 'create'; modifier: undefined }
  | { mode: 'edit'; modifier: GameModifierDefinition }

/**
 * Orchestration for the global modifier catalog screen: catalog query, the
 * create/edit dialog lifecycle, and create/update/archive mutations. The page
 * stays presentational; server-error wording is resolved by the caller.
 */
function matchesModifierSearch(modifier: GameModifierDefinition, search: string) {
  const normalizedSearch = search.trim().toLowerCase()
  if (!normalizedSearch) {
    return true
  }

  const haystack = [
    modifier.name,
    modifier.description,
    modifier.scoringType,
    modifier.mechanicType,
    modifier.iconEmoji ?? '',
    modifier.activationCommand ?? '',
  ]
    .join(' ')
    .toLowerCase()

  return haystack.includes(normalizedSearch)
}

function matchesMechanicType(
  modifier: GameModifierDefinition,
  mechanicType: ModifierMechanicType | null,
) {
  if (!mechanicType) {
    return true
  }

  return modifier.mechanicType === mechanicType
}

export function useCatalogModifiers() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [selectedMechanicType, setSelectedMechanicType] = useState<ModifierMechanicType | null>(
    null,
  )
  const catalogQuery = useQuery(gameModifierCatalogQueryOptions)
  const mechanicTypeCounts = useMemo(() => {
    const counts = Object.fromEntries(modifierMechanicTypes.map((type) => [type, 0])) as Record<
      ModifierMechanicType,
      number
    >

    for (const modifier of catalogQuery.data ?? []) {
      if (modifier.mechanicType in counts) {
        counts[modifier.mechanicType as ModifierMechanicType] += 1
      }
    }

    return counts
  }, [catalogQuery.data])
  const filteredModifiers = useMemo(
    () =>
      (catalogQuery.data ?? []).filter(
        (modifier) =>
          matchesModifierSearch(modifier, search) &&
          matchesMechanicType(modifier, selectedMechanicType),
      ),
    [catalogQuery.data, search, selectedMechanicType],
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
    selectedMechanicType,
    setSelectedMechanicType,
    mechanicTypeCounts,
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
