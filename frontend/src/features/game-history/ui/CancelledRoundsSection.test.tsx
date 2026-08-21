import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { CancelledRoundsSection } from './CancelledRoundsSection.tsx'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(cleanup)

describe('CancelledRoundsSection', () => {
  it('shows public cancellation audit without score totals or internal details', () => {
    renderWithAppProviders(
      <CancelledRoundsSection rounds={[createCancelledRound()]} onPreviewCard={vi.fn()} />,
    )

    expect(screen.getByText('Технически отменённые раунды')).toBeInTheDocument()
    expect(screen.getByText('Card One')).toBeInTheDocument()
    expect(screen.getByText('Отмена из стадии: игровой процесс')).toBeInTheDocument()
    expect(screen.getByText('Сбой стрима или инфраструктуры')).toBeInTheDocument()
    expect(screen.getByText('Трансляция была недоступна.')).toBeInTheDocument()
    expect(screen.getByText('Покупки полностью возвращены')).toBeInTheDocument()
    expect(screen.queryByText('private operator detail')).not.toBeInTheDocument()
  })
})

function createCancelledRound(): GameHistoryRound {
  return {
    roundId: 'round-cancelled',
    teamId: 'team-1',
    teamName: 'Team One',
    teamSlotIndex: 1,
    status: 'cancelled',
    roundVersion: 5,
    startedAtUtc: '2026-08-20T10:00:00Z',
    gameplayStartedAtUtc: '2026-08-20T10:01:00Z',
    finishedAtUtc: '2026-08-20T10:02:00Z',
    baseScore: 100,
    finalScore: 0,
    emptyCardPenaltyApplied: false,
    scoreDetails: {
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
    },
    killsCount: 0,
    bountyCount: 0,
    cellId: 'cell-1',
    cellRowIndex: 0,
    cellColIndex: 0,
    cellType: 'question',
    cellTitle: 'Card One',
    cellCost: 100,
    technicalCancellationReasonCode: 'stream_or_infrastructure_failure',
    publicCancellationSummary: 'Трансляция была недоступна.',
    technicalCancellationStage: 'in_progress',
    purchasesRefunded: true,
    cellMedia: [],
    participants: [],
    modifiers: [],
  }
}
