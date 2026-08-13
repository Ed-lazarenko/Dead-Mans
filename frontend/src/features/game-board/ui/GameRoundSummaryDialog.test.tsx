import { cleanup, fireEvent, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { GameRoundSummaryDialog } from './GameRoundSummaryDialog.tsx'

const apiMocks = vi.hoisted(() => ({
  previewGameRoundScore: vi.fn(),
}))

vi.mock('../../game-rounds/api/game-rounds-api.ts', () => ({
  previewGameRoundScore: apiMocks.previewGameRoundScore,
}))

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

const scoreDetails: components['schemas']['GameRoundScoreDetailsDto'] = {
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
}

const activeRound: GameRoundDetails = {
  roundId: 'round-1',
  gameId: 'game-1',
  cellId: 'cell-1',
  teamId: 'team-1',
  teamName: 'Team One',
  teamSlotIndex: 1,
  status: 'reviewing_results',
  startedAtUtc: '2026-08-14T00:00:00Z',
  baseScore: 100,
  emptyCardPenaltyApplied: false,
  scoreDetails,
  killsCount: 0,
  bountyCount: 0,
  participants: [],
  modifierResults: [],
}

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

beforeEach(() => {
  apiMocks.previewGameRoundScore.mockResolvedValue({ scoreDetails, modifierResults: [] })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('GameRoundSummaryDialog', () => {
  it('asks before closing when the form has unsaved changes', async () => {
    const onClose = vi.fn()
    renderWithAppProviders(
      <GameRoundSummaryDialog
        open
        activeRound={activeRound}
        isSubmitting={false}
        onClose={onClose}
        onSubmit={vi.fn()}
      />,
    )

    fireEvent.change(screen.getByRole('spinbutton', { name: 'Убитые враги' }), {
      target: { value: '2' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }))

    const confirmDialog = screen.getByRole('dialog', {
      name: 'Закрыть итоги без сохранения?',
    })
    expect(onClose).not.toHaveBeenCalled()
    fireEvent.click(
      within(confirmDialog).getByRole('button', { name: 'Продолжить редактирование' }),
    )
    await waitFor(() =>
      expect(
        screen.queryByRole('dialog', { name: 'Закрыть итоги без сохранения?' }),
      ).not.toBeInTheDocument(),
    )
    expect(screen.getByRole('spinbutton', { name: 'Убитые враги' })).toHaveValue(2)

    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }))
    fireEvent.click(
      within(screen.getByRole('dialog', { name: 'Закрыть итоги без сохранения?' })).getByRole(
        'button',
        { name: 'Закрыть без сохранения' },
      ),
    )

    expect(onClose).toHaveBeenCalledTimes(1)
  })
})
