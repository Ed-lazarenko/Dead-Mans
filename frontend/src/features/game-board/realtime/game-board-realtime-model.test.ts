import { describe, expect, it } from 'vitest'
import type {
  GameBoardSnapshot,
  GameCellOpenedEvent,
  GameModifierActivation,
  GameModifierActivatedEvent,
} from '../../../shared/api/contracts/index.ts'
import {
  applyCellOpenedEvent,
  applyModifierActivationCancelledEvent,
  applyModifierActivatedEvent,
  selectNewerGameBoardSnapshot,
} from './game-board-realtime-model.ts'

const snapshot: GameBoardSnapshot = {
  gameId: 'game-1',
  title: 'Game',
  description: null,
  status: 'active',
  version: 2,
  rows: 1,
  cols: 1,
  rowLabels: ['100'],
  colLabels: ['A'],
  cells: [
    {
      id: 'cell-1',
      row: 0,
      col: 0,
      cellType: 'question',
      title: null,
      description: null,
      cost: 100,
      state: 'closed',
      media: [],
    },
  ],
  enabledModifierIds: [],
  activeModifiers: [],
}

function createCellOpenedEvent(overrides: Partial<GameCellOpenedEvent> = {}): GameCellOpenedEvent {
  return {
    gameId: snapshot.gameId,
    version: snapshot.version + 1,
    cell: {
      ...snapshot.cells[0]!,
      title: 'Opened cell',
      state: 'open',
    },
    ...overrides,
  }
}

function createModifierActivation(
  overrides: Partial<GameModifierActivation> = {},
): GameModifierActivation {
  return {
    activationId: 'activation-1',
    modifierId: 'modifier-1',
    modifierName: 'Chirik',
    activatedByUserId: 'user-1',
    activatedByDisplayName: 'Player One',
    activationCost: 15,
    activatedAtUtc: '2026-07-21T18:00:00Z',
    ...overrides,
  }
}

function createModifierActivatedEvent(
  overrides: Partial<GameModifierActivatedEvent> = {},
): GameModifierActivatedEvent {
  return {
    gameId: snapshot.gameId,
    version: snapshot.version + 1,
    activation: createModifierActivation(),
    ...overrides,
  }
}

describe('game board realtime model', () => {
  it('patches a newer cell event into the current snapshot', () => {
    const result = applyCellOpenedEvent(snapshot, createCellOpenedEvent())

    expect(result.requiresResync).toBe(false)
    expect(result.nextSnapshot?.version).toBe(3)
    expect(result.nextSnapshot?.cells[0]).toMatchObject({
      id: 'cell-1',
      title: 'Opened cell',
      state: 'open',
    })
  })

  it('requests a resync when the current snapshot or event cell is missing', () => {
    expect(applyCellOpenedEvent(null, createCellOpenedEvent())).toEqual({
      nextSnapshot: null,
      requiresResync: true,
    })

    const event = createCellOpenedEvent({
      cell: { ...createCellOpenedEvent().cell, id: 'unknown-cell' },
    })
    expect(applyCellOpenedEvent(snapshot, event)).toEqual({
      nextSnapshot: snapshot,
      requiresResync: true,
    })
  })

  it('patches a newer modifier activation into the current snapshot', () => {
    const result = applyModifierActivatedEvent(snapshot, createModifierActivatedEvent())

    expect(result.requiresResync).toBe(false)
    expect(result.nextSnapshot?.version).toBe(3)
    expect(result.nextSnapshot?.activeModifiers).toHaveLength(1)
    expect(result.nextSnapshot?.activeModifiers[0]).toMatchObject({
      activationId: 'activation-1',
      modifierId: 'modifier-1',
      modifierName: 'Chirik',
    })
  })

  it('replaces an existing modifier activation instead of duplicating it', () => {
    const current = {
      ...snapshot,
      activeModifiers: [createModifierActivation({ modifierName: 'Old name' })],
    }

    const result = applyModifierActivatedEvent(
      current,
      createModifierActivatedEvent({
        version: snapshot.version + 1,
        activation: createModifierActivation({ modifierName: 'Updated name' }),
      }),
    )

    expect(result.requiresResync).toBe(false)
    expect(result.nextSnapshot?.version).toBe(3)
    expect(result.nextSnapshot?.activeModifiers).toEqual([
      expect.objectContaining({
        activationId: 'activation-1',
        modifierName: 'Updated name',
      }),
    ])
  })

  it('removes a cancelled modifier activation from the current snapshot', () => {
    const withModifier = applyModifierActivatedEvent(
      snapshot,
      createModifierActivatedEvent(),
    ).nextSnapshot

    const result = applyModifierActivationCancelledEvent(withModifier, {
      gameId: snapshot.gameId,
      version: snapshot.version + 2,
      activationId: 'activation-1',
    })

    expect(result.requiresResync).toBe(false)
    expect(result.nextSnapshot?.version).toBe(4)
    expect(result.nextSnapshot?.activeModifiers).toHaveLength(0)
  })

  it('requests a modifier resync when there is no current snapshot', () => {
    expect(applyModifierActivatedEvent(null, createModifierActivatedEvent())).toEqual({
      nextSnapshot: null,
      requiresResync: true,
    })
  })

  it('ignores events for another game or an already applied version', () => {
    expect(applyCellOpenedEvent(snapshot, createCellOpenedEvent({ gameId: 'other-game' }))).toEqual(
      {
        nextSnapshot: snapshot,
        requiresResync: false,
      },
    )
    expect(
      applyCellOpenedEvent(snapshot, createCellOpenedEvent({ version: snapshot.version })),
    ).toEqual({
      nextSnapshot: snapshot,
      requiresResync: false,
    })
    expect(
      applyModifierActivatedEvent(snapshot, {
        gameId: 'other-game',
        version: snapshot.version + 1,
        activation: createModifierActivation(),
      }),
    ).toEqual({
      nextSnapshot: snapshot,
      requiresResync: false,
    })
    expect(
      applyModifierActivationCancelledEvent(
        { ...snapshot, activeModifiers: [createModifierActivation()] },
        {
          gameId: snapshot.gameId,
          version: snapshot.version,
          activationId: 'activation-1',
        },
      ),
    ).toEqual({
      nextSnapshot: { ...snapshot, activeModifiers: [createModifierActivation()] },
      requiresResync: false,
    })
  })

  it('selects only a strictly newer snapshot during a full resync', () => {
    const newer = { ...snapshot, version: 3 }
    const older = { ...snapshot, version: 1 }

    expect(selectNewerGameBoardSnapshot(undefined, newer)).toBe(newer)
    expect(selectNewerGameBoardSnapshot(snapshot, newer)).toBe(newer)
    expect(selectNewerGameBoardSnapshot(snapshot, older)).toBe(snapshot)
  })
})
