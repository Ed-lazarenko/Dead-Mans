import { describe, expect, it } from 'vitest'
import type { GameBoardSnapshot, GameCellOpenedEvent } from '../../../shared/api/contracts/index.ts'
import { applyCellOpenedEvent, selectNewerGameBoardSnapshot } from './game-board-realtime-model.ts'

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
  })

  it('selects only a strictly newer snapshot during a full resync', () => {
    const newer = { ...snapshot, version: 3 }
    const older = { ...snapshot, version: 1 }

    expect(selectNewerGameBoardSnapshot(undefined, newer)).toBe(newer)
    expect(selectNewerGameBoardSnapshot(snapshot, newer)).toBe(newer)
    expect(selectNewerGameBoardSnapshot(snapshot, older)).toBe(snapshot)
  })
})
