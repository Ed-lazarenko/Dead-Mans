import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../i18n.ts'
import { renderWithAppProviders } from '../../test/render-with-app-providers.tsx'
import { GameSetupPage } from './GameSetupPage.tsx'

const pageMocks = vi.hoisted(() => ({
  useGameSetupPage: vi.fn(),
}))

vi.mock('./use-game-setup-page.ts', () => ({
  useGameSetupPage: pageMocks.useGameSetupPage,
}))

function createPageController(overrides: Record<string, unknown> = {}) {
  return {
    snapshot: null,
    draft: null,
    isLoading: false,
    isError: false,
    isDirty: false,
    syncStatus: 'idle',
    remoteChangeNotice: false,
    draftRemovedNotice: false,
    saveErrorMessage: null,
    resetErrorMessage: null,
    updateDraft: vi.fn(),
    applyLayoutChange: vi.fn(),
    saveDraft: vi.fn(),
    reloadFromServer: vi.fn(),
    createDraft: vi.fn(),
    deleteDraft: vi.fn(),
    isCreating: false,
    isResetting: false,
    isSaving: false,
    cellMediaDisplayByCellId: {},
    isCellMediaBusy: vi.fn(() => false),
    cellMediaErrorKey: null,
    uploadCellMedia: vi.fn(),
    deleteCellMedia: vi.fn(),
    dismissCellMediaError: vi.fn(),
    dismissRemoteChangeNotice: vi.fn(),
    dismissDraftRemovedNotice: vi.fn(),
    toggleModifier: vi.fn(),
    ...overrides,
  }
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  pageMocks.useGameSetupPage.mockReturnValue(createPageController())
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameSetupPage', () => {
  it('renders the loading state', () => {
    pageMocks.useGameSetupPage.mockReturnValue(createPageController({ isLoading: true }))

    renderWithAppProviders(<GameSetupPage />)

    expect(screen.getByText('Загрузка настройки игры...')).toBeInTheDocument()
  })

  it('renders the error state', () => {
    pageMocks.useGameSetupPage.mockReturnValue(createPageController({ isError: true }))

    renderWithAppProviders(<GameSetupPage />)

    expect(screen.getByText('Не удалось загрузить настройку игры.')).toBeInTheDocument()
  })

  it('renders the empty draft state with an inline create panel instead of a dialog', () => {
    renderWithAppProviders(<GameSetupPage />)

    expect(
      screen.getByText(
        'Сейчас нет игры в статусе черновика. Создайте новую, чтобы начать настройку.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Создать игру' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
