import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type {
  CreateGameQuestionCategoryRequest,
  CreateGameQuestionRequest,
  GameQuestionCategoryItem,
  GameQuestionCatalogItem,
} from '../../shared/api/contracts/index.ts'
import {
  createGameQuestionMutationOptions,
  deleteGameQuestionMutationOptions,
  gameQuestionCatalogQueryOptions,
  updateGameQuestionMutationOptions,
} from '../game-setup/index.ts'
import { gameQuestionQueryKeys } from '../game-setup/api/game-question-queries.ts'
import {
  createQuestionCategory,
  deleteQuestionCategory,
  fetchQuestionCategories,
  questionCategoryQueryKey,
  updateQuestionCategory,
} from './api/question-categories-api.ts'
import {
  downloadQuestionImportTemplate,
  importQuestionsFile,
} from './api/question-import-api.ts'

type QuestionDialogState =
  | { mode: 'create'; question: undefined }
  | { mode: 'edit'; question: GameQuestionCatalogItem }

type CategoryDialogState =
  | { mode: 'create'; category: null }
  | { mode: 'edit'; category: GameQuestionCategoryItem }

/**
 * Orchestration for the global question catalog screen: search-filtered catalog
 * query, the create/edit dialog lifecycle, and create/update/delete mutations.
 * The page stays presentational; server-error wording is resolved by the caller.
 */
export function useCatalogQuestions() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(null)
  const catalogQuery = useQuery(
    gameQuestionCatalogQueryOptions({
      search,
      ...(selectedCategoryId ? { categoryId: selectedCategoryId } : {}),
    }),
  )
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
  const updateCategoryMutation = useMutation({
    mutationFn: ({
      categoryId,
      request,
    }: {
      categoryId: string
      request: CreateGameQuestionCategoryRequest
    }) => updateQuestionCategory(categoryId, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey }),
        queryClient.invalidateQueries({ queryKey: gameQuestionQueryKeys.all }),
      ])
    },
  })
  const deleteCategoryMutation = useMutation({
    mutationFn: (categoryId: string) => deleteQuestionCategory(categoryId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey }),
        queryClient.invalidateQueries({ queryKey: gameQuestionQueryKeys.all }),
      ])
    },
  })
  const importQuestionsMutation = useMutation({
    mutationFn: (file: File) => importQuestionsFile(file),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey }),
        queryClient.invalidateQueries({ queryKey: gameQuestionQueryKeys.all }),
      ])
    },
  })
  const downloadTemplateMutation = useMutation({
    mutationFn: () => downloadQuestionImportTemplate(),
  })

  const [dialog, setDialog] = useState<QuestionDialogState | null>(null)
  const [categoryDialog, setCategoryDialog] = useState<CategoryDialogState | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<GameQuestionCatalogItem | null>(null)
  const [deleteCategoryTarget, setDeleteCategoryTarget] = useState<GameQuestionCategoryItem | null>(
    null,
  )

  const openCreate = () => setDialog({ mode: 'create', question: undefined })
  const openEdit = (question: GameQuestionCatalogItem) => setDialog({ mode: 'edit', question })
  const closeDialog = () => setDialog(null)
  const openCreateCategory = () => setCategoryDialog({ mode: 'create', category: null })
  const openEditCategory = (category: GameQuestionCategoryItem) =>
    setCategoryDialog({ mode: 'edit', category })
  const closeCreateCategory = () => setCategoryDialog(null)

  const submitQuestion = async (request: CreateGameQuestionRequest) => {
    if (dialog?.mode === 'edit') {
      await updateMutation.mutateAsync({ questionId: dialog.question.questionId, request })
    } else {
      await createMutation.mutateAsync(request)
    }
    await queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey })
    closeDialog()
  }

  const submitCategory = async (name: string) => {
    const category =
      categoryDialog?.mode === 'edit' && categoryDialog.category
        ? await updateCategoryMutation.mutateAsync({
            categoryId: categoryDialog.category.id,
            request: { name },
          })
        : await createCategoryMutation.mutateAsync({ name })

    setSelectedCategoryId(category.id)
    closeCreateCategory()
  }

  const requestDelete = (question: GameQuestionCatalogItem) => setDeleteTarget(question)
  const cancelDelete = () => setDeleteTarget(null)
  const confirmDelete = async () => {
    if (!deleteTarget) {
      return
    }
    await deleteMutation.mutateAsync(deleteTarget.questionId)
    await queryClient.invalidateQueries({ queryKey: questionCategoryQueryKey })
    setDeleteTarget(null)
  }

  const selectedCategory =
    categoriesQuery.data?.find((category) => category.id === selectedCategoryId) ?? null

  const requestDeleteCategory = (category: GameQuestionCategoryItem) =>
    setDeleteCategoryTarget(category)
  const cancelDeleteCategory = () => setDeleteCategoryTarget(null)
  const confirmDeleteCategory = async () => {
    if (!deleteCategoryTarget) {
      return
    }

    await deleteCategoryMutation.mutateAsync(deleteCategoryTarget.id)
    if (selectedCategoryId === deleteCategoryTarget.id) {
      setSelectedCategoryId(null)
    }
    setDeleteCategoryTarget(null)
  }

  return {
    search,
    setSearch,
    selectedCategoryId,
    setSelectedCategoryId,
    selectedCategory,
    catalogQuery,
    categoriesQuery,
    dialog,
    openCreate,
    openEdit,
    closeDialog,
    submitQuestion,
    categoryDialog,
    openCreateCategory,
    openEditCategory,
    closeCreateCategory,
    submitCategory,
    isSaving: createMutation.isPending || updateMutation.isPending,
    isSavingCategory: createCategoryMutation.isPending || updateCategoryMutation.isPending,
    deleteTarget,
    requestDelete,
    cancelDelete,
    confirmDelete,
    isDeleting: deleteMutation.isPending,
    deleteCategoryTarget,
    requestDeleteCategory,
    cancelDeleteCategory,
    confirmDeleteCategory,
    isDeletingCategory: deleteCategoryMutation.isPending,
    importQuestions: importQuestionsMutation.mutateAsync,
    isImportingQuestions: importQuestionsMutation.isPending,
    downloadTemplate: downloadTemplateMutation.mutateAsync,
    isDownloadingTemplate: downloadTemplateMutation.isPending,
  }
}
