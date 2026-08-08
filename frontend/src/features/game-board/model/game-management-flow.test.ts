import { describe, expect, it } from 'vitest'
import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { buildGameManagementFlow } from './game-management-flow.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

const baseSnapshot: GameBoardSnapshot = {
  gameId: 'game-1',
  title: 'Test game',
  description: null,
  status: 'active',
  version: 1,
  rows: 1,
  cols: 1,
  rowLabels: ['A'],
  colLabels: ['1'],
  cells: [
    {
      id: 'cell-1',
      row: 0,
      col: 0,
      title: 'Cell',
      cost: 100,
      state: 'hidden',
      media: [],
    },
  ],
  enabledModifierIds: [],
  activeModifiers: [],
  activeTeamId: null,
}

function createRound(overrides: Partial<GameRoundDetails>): GameRoundDetails {
  return {
    roundId: 'round-1',
    cellId: 'cell-1',
    teamId: 'team-1',
    teamSlotIndex: 1,
    baseScore: 100,
    emptyCardPenaltyApplied: false,
    status: 'awaiting_modifiers',
    ...overrides,
  }
}

describe('buildGameManagementFlow', () => {
  it('blocks round flow until the game is launched', () => {
    const flow = buildGameManagementFlow(
      {
        ...baseSnapshot,
        status: 'ready',
      },
      null,
    )

    expect(flow.summaryKey).toBe('gameBoard.flowSummary.waitingForLaunch')
    expect(flow.steps.map((step) => step.state)).toEqual([
      'blocked',
      'blocked',
      'blocked',
      'blocked',
      'blocked',
      'blocked',
    ])
  })

  it('starts with active team selection when the game is active', () => {
    const flow = buildGameManagementFlow(baseSnapshot, null)

    expect(flow.summaryKey).toBe('gameBoard.flowSummary.selectActiveTeam')
    expect(flow.steps.map((step) => step.state)).toEqual([
      'current',
      'blocked',
      'blocked',
      'blocked',
      'blocked',
      'blocked',
    ])
  })

  it('moves to card selection after the active team is chosen', () => {
    const flow = buildGameManagementFlow(
      {
        ...baseSnapshot,
        activeTeamId: 'team-1',
      },
      null,
    )

    expect(flow.summaryKey).toBe('gameBoard.flowSummary.selectCard')
    expect(flow.steps.map((step) => step.state)).toEqual([
      'complete',
      'current',
      'upcoming',
      'upcoming',
      'upcoming',
      'upcoming',
    ])
  })

  it('opens modifiers and round start after a round is created', () => {
    const flow = buildGameManagementFlow(
      baseSnapshot,
      createRound({ status: 'awaiting_modifiers' }),
    )

    expect(flow.summaryKey).toBe('gameBoard.flowSummary.awaitingModifiers')
    expect(flow.steps.map((step) => step.state)).toEqual([
      'complete',
      'complete',
      'current',
      'ready',
      'upcoming',
      'upcoming',
    ])
  })

  it('shows gameplay and result review phases in order', () => {
    const inProgress = buildGameManagementFlow(baseSnapshot, createRound({ status: 'in_progress' }))
    expect(inProgress.summaryKey).toBe('gameBoard.flowSummary.roundInProgress')
    expect(inProgress.steps.map((step) => step.state)).toEqual([
      'complete',
      'complete',
      'complete',
      'complete',
      'current',
      'ready',
    ])

    const reviewing = buildGameManagementFlow(
      baseSnapshot,
      createRound({ status: 'reviewing_results' }),
    )
    expect(reviewing.summaryKey).toBe('gameBoard.flowSummary.reviewingResults')
    expect(reviewing.steps.map((step) => step.state)).toEqual([
      'complete',
      'complete',
      'complete',
      'complete',
      'complete',
      'current',
    ])
  })
})
