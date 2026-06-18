import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
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

type ModifierDialogState =
  | { mode: 'create'; modifier: undefined }
  | { mode: 'edit'; modifier: GameModifierDefinition }

/**
 * Orchestration for the global modifier catalog screen: catalog query, the
 * create/edit dialog lifecycle, and create/update/archive mutations. The page
 * stays presentational; server-error wording is resolved by the caller.
 */
export function useCatalogModifiers() {
  const queryClient = useQueryClient()
  const catalogQuery = useQuery(gameModifierCatalogQueryOptions)
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
        modifierCode: dialog.modifier.code,
        request: {
          name: request.name,
          description: request.description,
          kind: request.kind,
          category: request.category,
          scoringType: request.scoringType,
          tier: request.tier,
          activationCost: request.activationCost,
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
    await deleteMutation.mutateAsync(deleteTarget.code)
    setDeleteTarget(null)
  }

  return {
    catalogQuery,
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
