import { cleanup, screen } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import i18n from '../../../i18n.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { renderWithAppProviders } from '../../../test/render-with-app-providers.tsx'
import { ModifierRuntimePanel } from './ModifierRuntimePanel.tsx'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

beforeAll(async () => {
  await i18n.changeLanguage('ru')
})

afterEach(() => {
  vi.useRealTimers()
  cleanup()
})

describe('ModifierRuntimePanel', () => {
  it('shows frozen host instruction, countdown, stacking, and stale clock state', () => {
    vi.useFakeTimers()
    vi.setSystemTime('2026-08-20T10:01:00.000Z')
    renderWithAppProviders(<ModifierRuntimePanel round={createRound()} isOffline />)

    expect(screen.getByText('Мониторинг ведущего')).toBeInTheDocument()
    expect(screen.getByText('Использовать только обманки.')).toBeInTheDocument()
    expect(screen.getByText('Активаций: ×2')).toBeInTheDocument()
    expect(screen.getByText('Осталось 3:00')).toBeInTheDocument()
    expect(screen.getByText('Время может устареть')).toBeInTheDocument()
    expect(screen.getByText(/Соединение потеряно/)).toBeInTheDocument()
  })
})

function createRound(): GameRoundDetails {
  return {
    roundId: 'round-1',
    gameId: 'game-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    status: 'in_progress',
    roundVersion: 3,
    startedAtUtc: '2026-08-20T09:59:00Z',
    gameplayStartedAtUtc: '2026-08-20T10:00:00Z',
    baseScore: 100,
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
    serverNowUtc: '2026-08-20T10:01:00Z',
    participants: [],
    modifierResults: [createResult('result-1'), createResult('result-2')],
  }
}

function createResult(id: string): GameRoundDetails['modifierResults'][number] {
  return {
    modifierResultId: id,
    modifierId: 'trickster',
    modifierName: 'Проказник',
    modifierDescription: 'Использовать только обманки.',
    modifierCategory: 'round',
    outcomeStatus: 'pending',
    scoreDelta: 0,
    killDelta: 0,
    activationId: `activation-${id}`,
    resolutionGroupId: 'group-1',
    resolutionKind: 'ruleStatus',
    runtimeBehavior: {
      phase: 'round',
      performer: 'activeTeam',
      requiresHostMonitoring: true,
      rule: 'Использовать только обманки.',
      stackingPolicy: 'aggregateParameters',
      durationSecondsPerActivation: 120,
    },
  }
}
