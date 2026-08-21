import { act, cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { GameRoundSummaryDialog } from './GameRoundSummaryDialog.tsx'

const apiMocks = vi.hoisted(() => ({ previewGameRoundScore: vi.fn() }))

vi.mock('../../game-rounds/api/game-rounds-api.ts', () => ({
  previewGameRoundScore: apiMocks.previewGameRoundScore,
}))

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type ModifierResult = GameRoundDetails['modifierResults'][number]
type ScorePreview = components['schemas']['GameRoundScorePreviewDto']

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  apiMocks.previewGameRoundScore.mockResolvedValue(createPreview())
})

afterEach(() => {
  vi.useRealTimers()
  cleanup()
  vi.clearAllMocks()
})

describe('GameRoundSummaryDialog', () => {
  it('asks before closing when the form has unsaved changes', async () => {
    const onClose = vi.fn()
    renderDialog(createRound(), { onClose })

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Убитые враги' }), {
      target: { value: '2' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }))

    const confirmDialog = screen.getByRole('dialog', { name: 'Закрыть итоги без сохранения?' })
    expect(onClose).not.toHaveBeenCalled()
    fireEvent.click(
      within(confirmDialog).getByRole('button', { name: 'Продолжить редактирование' }),
    )
    await waitFor(() =>
      expect(screen.getByRole('spinbutton', { name: 'Убитые враги' })).toHaveValue(2),
    )
  })

  it('shows grouped rule members, independent Shot rows, and waits for required inputs', async () => {
    const round = createRound({
      modifierResults: [
        createModifier({
          modifierResultId: 'rule-1',
          activationId: 'activation-rule-1',
          modifierName: 'Чирик',
          resolutionKind: 'ruleStatus',
          resolutionGroupId: 'group-1',
          runtimeBehavior: createRuntimeBehavior(),
        }),
        createModifier({
          modifierResultId: 'rule-2',
          activationId: 'activation-rule-2',
          modifierName: 'Чирик',
          resolutionKind: 'ruleStatus',
          resolutionGroupId: 'group-1',
          runtimeBehavior: createRuntimeBehavior(),
        }),
        createModifier({
          modifierResultId: 'shot-1',
          activationId: 'activation-shot-1',
          modifierId: 'shot',
          modifierName: 'Шот',
          resolutionKind: 'boolean',
        }),
        createModifier({
          modifierResultId: 'shot-2',
          activationId: 'activation-shot-2',
          modifierId: 'shot',
          modifierName: 'Шот',
          resolutionKind: 'boolean',
        }),
        createModifier({
          modifierResultId: 'auto-1',
          activationId: 'activation-auto-1',
          modifierName: 'Жажда',
          resolutionKind: 'automaticRoundMetric',
        }),
      ],
    })
    renderDialog(round)

    expect(screen.getByText('Применён ×2')).toBeInTheDocument()
    expect(screen.getByText(/Участники группы: #1 .*rule-1, #2 .*rule-2/)).toBeInTheDocument()
    expect(screen.getByText('Активация 1 из 2')).toBeInTheDocument()
    expect(screen.getByText('Активация 2 из 2')).toBeInTheDocument()
    expect(screen.getByText(/Ручной ввод не нужен/)).toBeInTheDocument()
    expect(screen.getByText('Карточка: Карта 100')).toBeInTheDocument()
    expect(screen.getByText('Зафиксированная стоимость: 100 очк.')).toBeInTheDocument()
    expect(screen.getByText('Время игры: 2:00')).toBeInTheDocument()
    expect(screen.getByText('Таймер «Чирик» завершён')).toBeInTheDocument()
    expect(screen.getByText(/Укажите все обязательные исходы/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Завершить раунд' })).toBeDisabled()

    await act(async () => new Promise((resolve) => window.setTimeout(resolve, 400)))
    expect(apiMocks.previewGameRoundScore).not.toHaveBeenCalled()
  })

  it('debounces preview and submits only the latest server-approved draft', async () => {
    const onSubmit = vi.fn()
    renderDialog(createRound(), { onSubmit })

    const kills = screen.getByRole('spinbutton', { name: 'Убитые враги' })
    const notes = screen.getByRole('textbox', { name: 'Заметка о раунде' })
    fireEvent.change(kills, { target: { value: '1' } })
    fireEvent.change(kills, { target: { value: '3' } })
    fireEvent.change(notes, { target: { value: '  Подтверждено ведущим.  ' } })

    await waitFor(() => expect(apiMocks.previewGameRoundScore).toHaveBeenCalledTimes(1), {
      timeout: 1_000,
    })
    expect(apiMocks.previewGameRoundScore).toHaveBeenCalledWith(
      'round-1',
      expect.objectContaining({
        killsCount: 3,
        notes: 'Подтверждено ведущим.',
        expectedRoundVersion: 4,
        modifierResults: [],
        ruleGroups: [],
      }),
    )
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Завершить раунд' })).toBeEnabled(),
    )
    fireEvent.click(screen.getByRole('button', { name: 'Завершить раунд' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        roundSummary: expect.objectContaining({
          killsCount: 3,
          notes: 'Подтверждено ведущим.',
          expectedRoundVersion: 4,
        }),
      }),
    )
  })

  it('ignores an older preview response that arrives after the latest response', async () => {
    const first = deferred<ScorePreview>()
    const second = deferred<ScorePreview>()
    apiMocks.previewGameRoundScore
      .mockImplementationOnce(() => first.promise)
      .mockImplementationOnce(() => second.promise)
    renderDialog(createRound())

    await waitFor(() => expect(apiMocks.previewGameRoundScore).toHaveBeenCalledTimes(1))
    fireEvent.change(screen.getByRole('spinbutton', { name: 'Убитые враги' }), {
      target: { value: '2' },
    })
    await waitFor(() => expect(apiMocks.previewGameRoundScore).toHaveBeenCalledTimes(2))

    second.resolve(createPreview({ scoreDetails: createScoreDetails({ finalScore: 222 }) }))
    await waitFor(() => expect(screen.getByText('222 очк.')).toBeInTheDocument())
    first.resolve(createPreview({ scoreDetails: createScoreDetails({ finalScore: 111 }) }))
    await act(async () => Promise.resolve())

    expect(screen.getByText('222 очк.')).toBeInTheDocument()
    expect(screen.queryByText('111 очк.')).not.toBeInTheDocument()
  })

  it('blocks completion for stale round version and preview errors', async () => {
    apiMocks.previewGameRoundScore.mockRejectedValueOnce(
      new ApiError('stale', {
        status: 409,
        details: { code: 'game_round.stale_version' },
      }),
    )
    renderDialog(createRound())

    await waitFor(() => expect(screen.getByText(/Раунд изменился на сервере/)).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Завершить раунд' })).toBeDisabled()
  })

  it('shows loading and a blocking error when authoritative preview fails', async () => {
    const preview = deferred<ScorePreview>()
    apiMocks.previewGameRoundScore.mockReturnValueOnce(preview.promise)
    renderDialog(createRound())

    await waitFor(() => expect(apiMocks.previewGameRoundScore).toHaveBeenCalledTimes(1))
    expect(screen.getByText(/Сервер рассчитывает итоговый результат/)).toBeInTheDocument()

    await act(async () => {
      preview.reject(
        new ApiError('failed', { status: 422, details: { code: 'formula.incompatible' } }),
      )
    })
    await waitFor(() => expect(screen.getByText(/formula.incompatible/)).toBeInTheDocument())
    expect(screen.getByRole('button', { name: 'Завершить раунд' })).toBeDisabled()
  })
})

function renderDialog(
  activeRound: GameRoundDetails,
  overrides: { onClose?: () => void; onSubmit?: () => void } = {},
) {
  return renderWithAppProviders(
    <GameRoundSummaryDialog
      open
      activeRound={activeRound}
      isSubmitting={false}
      onClose={overrides.onClose ?? vi.fn()}
      onSubmit={overrides.onSubmit ?? vi.fn()}
    />,
  )
}

function createRound(overrides: Partial<GameRoundDetails> = {}): GameRoundDetails {
  return {
    roundId: 'round-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    cellTitle: 'Карта 100',
    cellDescription: 'Описание карты.',
    teamId: 'team-1',
    teamName: 'Team One',
    teamSlotIndex: 1,
    status: 'reviewing_results',
    roundVersion: 4,
    startedAtUtc: '2026-08-14T00:00:00Z',
    gameplayStartedAtUtc: '2026-08-14T00:00:00Z',
    reviewedAtUtc: '2026-08-14T00:02:00Z',
    baseScore: 100,
    emptyCardPenaltyApplied: false,
    scoreDetails: createScoreDetails(),
    killsCount: 0,
    bountyCount: 0,
    serverNowUtc: '2026-08-14T00:02:00Z',
    participants: [],
    modifierResults: [],
    ...overrides,
  }
}

function createRuntimeBehavior(): NonNullable<ModifierResult['runtimeBehavior']> {
  return {
    phase: 'round',
    performer: 'activeTeam',
    requiresHostMonitoring: true,
    rule: 'Правило Чирика.',
    stackingPolicy: 'aggregateParameters',
    durationSecondsPerActivation: 60,
  }
}

function createModifier(overrides: Partial<ModifierResult> = {}): ModifierResult {
  return {
    modifierResultId: 'modifier-result-1',
    modifierId: 'modifier-1',
    modifierName: 'Modifier',
    modifierDescription: 'Modifier description.',
    modifierCategory: 'round',
    outcomeStatus: 'pending',
    scoreDelta: 0,
    killDelta: 0,
    activationId: 'activation-1',
    definitionRevision: 1,
    resolutionGroupId: null,
    resolutionKind: 'boolean',
    ...overrides,
  }
}

function createPreview(overrides: Partial<ScorePreview> = {}): ScorePreview {
  return {
    scoreDetails: createScoreDetails(),
    modifierResults: [],
    roundVersion: 4,
    normalizedInputHash: 'authoritative-hash',
    calculationTrace: [],
    ...overrides,
  }
}

function createScoreDetails(
  overrides: Partial<GameRoundDetails['scoreDetails']> = {},
): GameRoundDetails['scoreDetails'] {
  return {
    scoreUnit: 100,
    killsScore: 0,
    bountyScore: 0,
    modifierKillDelta: 0,
    modifierKillScore: 0,
    modifierScoreDelta: 0,
    emptyCardPenaltyApplied: false,
    emptyCardPenaltyScore: 0,
    penaltyTotal: 0,
    bonusDelta: 0,
    totalKillCount: 0,
    finalScore: 0,
    ...overrides,
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((promiseResolve, promiseReject) => {
    resolve = promiseResolve
    reject = promiseReject
  })
  return { promise, reject, resolve }
}
