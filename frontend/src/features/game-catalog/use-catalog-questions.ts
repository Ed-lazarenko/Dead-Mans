import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type {
  CreateGameQuestionCategoryRequest,
  CreateGameQuestionRequest,
  GameQuestionCatalogItem,
} from '../../shared/api/contracts/index.ts'
import {
  createGameQuestionMutationOptions,
  deleteGameQuestionMutationOptions,
  gameQuestionCatalogQueryOptions,
  updateGameQuestionMutationOptions,
} from '../game-setup/index.ts'
import {
  createQuestionCategory,
  fetchQuestionCategories,
  questionCategoryQueryKey,
} from './api/question-categories-api.ts'

type QuestionDialogState =
  | { mode: 'create'; question: undefined }
  | { mode: 'edit'; question: GameQuestionCatalogItem }

/**
 * Orchestration for the global question catalog screen: search-filtered catalog
 * query, the create/edit dialog lifecycle, and create/update/delete mutations.
 * The page stays presentational; server-error wording is resolved by the caller.
 */
export function useCatalogQuestions() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const catalogQuery = useQuery(gameQuestionCatalogQueryOptions({ search }))
  const categoriesQuery = useQuery({
    queryKey: questionCategoryQueryKey,
    queryFn: fetchQuestionCategories,
  })
  const createMutation = useMutation(createGameQuestionMutationOptions(queryClient))
  const updateMutation = useMutation(updateGameQuestionMutationOptions(queryClient))
  const deleteMutation = useMutation(deleteGameQuestionMutationOptions(queryClient))
  const createCategoryMutation = useMutation({
    mutationFn: (request: CreateGameQuestionCategoryRequest) => createQuestionCategory(request),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey })
    },
  })

  const [dialog, setDialog] = useState<QuestionDialogState | null>(null)
  const [isCategoryDialogOpen, setIsCategoryDialogOpen] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<GameQuestionCatalogItem | null>(null)

  const openCreate = () => setDialog({ mode: 'create', question: undefined })
  const openEdit = (question: GameQuestionCatalogItem) => setDialog({ mode: 'edit', question })
  const closeDialog = () => setDialog(null)
  const openCreateCategory = () => setIsCategoryDialogOpen(true)
  const closeCreateCategory = () => setIsCategoryDialogOpen(false)

  const submitQuestion = async (request: CreateGameQuestionRequest) => {
    if (dialog?.mode === 'edit') {
      await updateMutation.mutateAsync({ questionId: dialog.question.questionId, request })
    } else {
      await createMutation.mutateAsync(request)
    }
    closeDialog()
  }

  const submitCategory = async (name: string) => {
    await createCategoryMutation.mutateAsync({ name })
    closeCreateCategory()
  }

  const requestDelete = (question: GameQuestionCatalogItem) => setDeleteTarget(question)
  const cancelDelete = () => setDeleteTarget(null)
  const confirmDelete = async () => {
    if (!deleteTarget) {
      return
    }
    await deleteMutation.mutateAsync(deleteTarget.questionId)
    setDeleteTarget(null)
  }

  return {
    search,
    setSearch,
    catalogQuery,
    categoriesQuery,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitQuestion,
    isCategoryDialogOpen,
    openCreateCategory,
    closeCreateCategory,
    submitCategory,
    isSaving: createMutation.isPending || updateMutation.isPending,
    isSavingCategory: createCategoryMutation.isPending,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting: deleteMutation.isPending,
  }
}
