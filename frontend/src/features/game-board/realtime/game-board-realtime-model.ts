import type {
  GameBoardSnapshot,
  GameCellOpenedEvent,
  GameModifierActivatedEvent,
  GameModifierActivationCancelledEvent,
} from '../../../shared/api/contracts/index.ts'

export type CellOpenedEvent = GameCellOpenedEvent
export type ModifierActivatedEvent = GameModifierActivatedEvent
export type ModifierActivationCancelledEvent = GameModifierActivationCancelledEvent

interface CellOpenedPatchResult {
  nextSnapshot: GameBoardSnapshot | null
  requiresResync: boolean
}

interface ModifierActivatedPatchResult {
  nextSnapshot: GameBoardSnapshot | null
  requiresResync: boolean
}

interface ModifierCancelledPatchResult {
  nextSnapshot: GameBoardSnapshot | null
  requiresResync: boolean
}

export function selectNewerGameBoardSnapshot(
  current: GameBoardSnapshot | null | undefined,
  incoming: GameBoardSnapshot,
): GameBoardSnapshot {
  if (!current) {
    return incoming
  }

  return incoming.version > current.version ? incoming : current
}

export function applyCellOpenedEvent(
  current: GameBoardSnapshot | null | undefined,
  event: CellOpenedEvent,
): CellOpenedPatchResult {
  if (!current) {
    return { nextSnapshot: null, requiresResync: true }
  }

  if (current.gameId !== event.gameId || event.version <= current.version) {
    return { nextSnapshot: current, requiresResync: false }
  }

  let updated = false
  const cells = current.cells.map((cell) => {
    if (cell.id !== event.cell.id) {
      return cell
    }

    updated = true
    return event.cell
  })

  if (!updated) {
    return { nextSnapshot: current, requiresResync: true }
  }

  return {
    nextSnapshot: {
      ...current,
      version: event.version,
      cells,
    },
    requiresResync: false,
  }
}

export function applyModifierActivatedEvent(
  current: GameBoardSnapshot | null | undefined,
  event: ModifierActivatedEvent,
): ModifierActivatedPatchResult {
  if (!current) {
    return { nextSnapshot: null, requiresResync: true }
  }

  if (current.gameId !== event.gameId || event.version <= current.version) {
    return { nextSnapshot: current, requiresResync: false }
  }

  return {
    nextSnapshot: {
      ...current,
      version: event.version,
      activeModifiers: [...current.activeModifiers, event.activation],
    },
    requiresResync: false,
  }
}

export function applyModifierActivationCancelledEvent(
  current: GameBoardSnapshot | null | undefined,
  event: ModifierActivationCancelledEvent,
): ModifierCancelledPatchResult {
  if (!current) {
    return { nextSnapshot: null, requiresResync: true }
  }

  if (current.gameId !== event.gameId || event.version <= current.version) {
    return { nextSnapshot: current, requiresResync: false }
  }

  const activeModifiers = current.activeModifiers.filter(
    (activation) => activation.activationId !== event.activationId,
  )

  if (activeModifiers.length === current.activeModifiers.length) {
    return { nextSnapshot: current, requiresResync: true }
  }

  return {
    nextSnapshot: {
      ...current,
      version: event.version,
      activeModifiers,
    },
    requiresResync: false,
  }
}
