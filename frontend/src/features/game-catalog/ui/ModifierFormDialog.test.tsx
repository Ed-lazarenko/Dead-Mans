import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { ModifierFormDialog } from './ModifierFormDialog.tsx'

const apiMocks = vi.hoisted(() => ({ previewGameModifier: vi.fn() }))

vi.mock('../api/catalog-modifiers-api.ts', () => ({
  previewGameModifier: apiMocks.previewGameModifier,
}))

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  apiMocks.previewGameModifier.mockResolvedValue({
    name: 'Новое правило',
    description: 'Описание правила',
    iconEmoji: null,
    activationCommand: '!активировать новое правило',
    normalizedTags: ['бой'],
    behaviorV2: {
      schemaVersion: 2,
      kind: 'rule',
      phase: 'round',
      performer: 'activeTeam',
      requiresHostMonitoring: false,
      rule: 'Выполнить правило.',
      stackingPolicy: 'aggregateParameters',
      resolution: { type: 'ruleStatus' },
      reward: 'none',
      formulaReference: null,
    },
    example: {
      cardValue: 100,
      killsCount: 3,
      bountyCount: 1,
      resolutionExample: 'completed',
      pointsDelta: 0,
      bonusKillsDelta: 0,
      finalScore: 400,
    },
  })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

function renderDialog(onClose = vi.fn()) {
  renderWithAppProviders(
    <ModifierFormDialog
      open
      mode="create"
      modifiers={[]}
      isBusy={false}
      onClose={onClose}
      onSubmit={vi.fn()}
    />,
  )
  return onClose
}

function fillCard() {
  fireEvent.change(screen.getByRole('textbox', { name: 'Название' }), {
    target: { value: 'Новое правило' },
  })
  fireEvent.change(screen.getByRole('textbox', { name: 'Описание' }), {
    target: { value: 'Описание правила' },
  })
}

describe('ModifierFormDialog', () => {
  it('skips the impact step for a rule and loads the backend review', async () => {
    renderDialog()
    fillCard()
    fireEvent.click(screen.getByRole('button', { name: 'Далее' }))

    const ruleField = await screen.findByRole('textbox', {
      name: 'Правило для команды и ведущего',
    })
    fireEvent.change(ruleField, { target: { value: 'Выполнить правило.' } })
    fireEvent.click(screen.getByRole('button', { name: 'Далее' }))

    await waitFor(() => expect(apiMocks.previewGameModifier).toHaveBeenCalledTimes(1))
    expect(await screen.findByText('Карточка игрока')).toBeInTheDocument()
    expect(screen.queryByLabelText('Что изменяется')).not.toBeInTheDocument()
  })

  it('shows the typed impact step for a scoring modifier', async () => {
    renderDialog()
    fireEvent.mouseDown(screen.getByRole('combobox', { name: 'Что делает модификатор?' }))
    fireEvent.click(screen.getByRole('option', { name: 'Влияет на итог раунда' }))
    fillCard()
    fireEvent.click(screen.getByRole('button', { name: 'Далее' }))

    fireEvent.change(
      await screen.findByRole('textbox', { name: 'Правило для команды и ведущего' }),
      { target: { value: 'Начислить бонус.' } },
    )
    fireEvent.click(screen.getByRole('button', { name: 'Далее' }))

    expect(await screen.findByRole('combobox', { name: 'Что изменяется' })).toBeInTheDocument()
    expect(apiMocks.previewGameModifier).not.toHaveBeenCalled()
    expect(
      screen.queryByText(/BehaviorV2|schemaVersion|resolutionKind|formulaReference/i),
    ).not.toBeInTheDocument()
  })

  it('uses a full-screen dialog on a mobile viewport', () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation((query: string) => ({
        matches: query.includes('max-width'),
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    )

    renderDialog()

    expect(screen.getByRole('dialog')).toHaveClass('MuiDialog-paperFullScreen')
  })

  it('asks for confirmation before discarding a dirty draft', async () => {
    const onClose = renderDialog()
    const nameField = screen.getByRole('textbox', { name: 'Название' })
    fireEvent.input(nameField, { target: { value: 'Новое правило' } })
    fireEvent.blur(nameField)
    await waitFor(() => expect(nameField).toHaveValue('Новое правило'))
    fireEvent.click(screen.getByRole('button', { name: 'Отмена' }))

    const confirmation = await screen.findByRole('dialog', {
      name: 'Отменить черновик модификатора?',
    })
    expect(onClose).not.toHaveBeenCalled()
    fireEvent.click(within(confirmation).getByRole('button', { name: 'Отменить изменения' }))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('keeps locked content read-only without exposing a save action', () => {
    renderWithAppProviders(
      <ModifierFormDialog
        open
        mode="edit"
        modifiers={[]}
        isBusy={false}
        isReadOnly
        onClose={vi.fn()}
        onSubmit={vi.fn()}
      />,
    )

    expect(screen.getByRole('textbox', { name: 'Название' })).toBeDisabled()
    expect(screen.getByText(/доступно только для просмотра/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Сохранить' })).not.toBeInTheDocument()
  })
})
