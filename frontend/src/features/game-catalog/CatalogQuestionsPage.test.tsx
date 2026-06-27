import { cleanup, fireEvent, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { CatalogQuestionsPage } from './CatalogQuestionsPage.tsx'

const catalogMocks = vi.hoisted(() => ({
  useCatalogQuestions: vi.fn(),
}))

vi.mock('./use-catalog-questions.ts', () => ({
  useCatalogQuestions: catalogMocks.useCatalogQuestions,
}))

function createCatalogController(overrides: Record<string, unknown> = {}) {
  return {
    search: '',
    setSearch: vi.fn(),
    selectedCategoryId: null,
    setSelectedCategoryId: vi.fn(),
    selectedCategory: null,
    catalogQuery: { isLoading: false, isError: false, data: [] },
    categoriesQuery: { isLoading: false, isError: false, data: [] },
    dialog: null,
    openCreate: vi.fn(),
    openEdit: vi.fn(),
    closeDialog: vi.fn(),
    submitQuestion: vi.fn(),
    categoryDialog: null,
    openCreateCategory: vi.fn(),
    openEditCategory: vi.fn(),
    closeCreateCategory: vi.fn(),
    submitCategory: vi.fn(),
    isSaving: false,
    isSavingCategory: false,
    deleteTarget: null,
    requestDelete: vi.fn(),
    cancelDelete: vi.fn(),
    confirmDelete: vi.fn(),
    isDeleting: false,
    deleteCategoryTarget: null,
    requestDeleteCategory: vi.fn(),
    cancelDeleteCategory: vi.fn(),
    confirmDeleteCategory: vi.fn(),
    isDeletingCategory: false,
    importQuestions: vi.fn(),
    isImportingQuestions: false,
    downloadTemplate: vi.fn(),
    isDownloadingTemplate: false,
    ...overrides,
  }
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  catalogMocks.useCatalogQuestions.mockReturnValue(createCatalogController())
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('CatalogQuestionsPage', () => {
  it('shows skipped import details and downloads a report from the warning alert', async () => {
    const importQuestions = vi.fn().mockResolvedValue({
      importedCount: 1,
      skippedQuestions: [
        {
          rowNumber: 3,
          questionText: 'Сломанный вопрос',
          reasonCode: 'game_question.import_duplicate_code_existing',
          reason: 'External code already exists.',
          sourceQuestion: {
            text: 'Сломанный вопрос',
            answer: 'Да',
            reward: 50,
            categoryId: null,
            externalCode: 'broken-q',
            isEnabled: false,
            priority: 0,
          },
        },
      ],
    })

    catalogMocks.useCatalogQuestions.mockReturnValue(
      createCatalogController({
        importQuestions,
      }),
    )

    const createObjectURL = vi.fn((blob: Blob) => {
      savedBlob = blob
      return 'blob:test-url'
    })
    const revokeObjectURL = vi.fn()
    let savedBlob: Blob | null = null

    Object.defineProperty(URL, 'createObjectURL', {
      value: createObjectURL,
      configurable: true,
    })
    Object.defineProperty(URL, 'revokeObjectURL', {
      value: revokeObjectURL,
      configurable: true,
    })

    renderWithAppProviders(<CatalogQuestionsPage />)

    fireEvent.click(screen.getByRole('button', { name: 'Развернуть импорт из JSON' }))
    fireEvent.click(screen.getByRole('button', { name: 'Загрузить JSON' }))
    const input = document.querySelector('input[type="file"]')
    expect(input).not.toBeNull()

    const file = new File(['{"questions":[]}'], 'questions.jsonc', {
      type: 'application/json',
    })
    fireEvent.change(input!, { target: { files: [file] } })

    expect(await screen.findByText('Пропущенные вопросы')).toBeInTheDocument()
    expect(
      screen.getByText('#3 - Сломанный вопрос: Такой код вопроса уже есть в каталоге.'),
    ).toBeInTheDocument()

    fireEvent.click(screen.getByRole('button', { name: 'Скачать отчёт' }))

    await waitFor(() => {
      expect(createObjectURL).toHaveBeenCalledTimes(1)
    })

    expect(savedBlob).not.toBeNull()
    const report = JSON.parse(await savedBlob!.text()) as {
      sourceFileName: string
      skippedCount: number
      skippedQuestions: Array<{
        rowNumber: number
        sourceQuestion: { externalCode: string | null } | null
      }>
    }

    expect(report.sourceFileName).toBe('questions.jsonc')
    expect(report.skippedCount).toBe(1)
    expect(report.skippedQuestions[0]).toEqual({
      rowNumber: 3,
      questionText: 'Сломанный вопрос',
      reasonCode: 'game_question.import_duplicate_code_existing',
      reason: 'External code already exists.',
      sourceQuestion: {
        text: 'Сломанный вопрос',
        answer: 'Да',
        reward: 50,
        categoryId: null,
        externalCode: 'broken-q',
        isEnabled: false,
        priority: 0,
      },
    })
  })
})
