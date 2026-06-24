import type {
  GameSetupSnapshot,
  UpdateGameSetupRequest,
} from '../../../shared/api/contracts/index.ts'
import { rebuildGameSetupCells } from './game-setup-board-ops.ts'
import {
  defaultGameSetupColumnLabel,
  defaultGameSetupRowCost,
  defaultGameSetupRowLabel,
} from './game-setup-defaults.ts'

export interface GameSetupCellDraft {
  id?: string
  row: number
  col: number
  title: string
  cost: number
}

export interface GameSetupDraftState {
  title: string
  rowLabels: string[]
  colLabels: string[]
  cells: GameSetupCellDraft[]
  enabledModifierIds: string[]
  enabledQuestionIds: string[]
}

export function createDraftFromSnapshot(snapshot: GameSetupSnapshot): GameSetupDraftState {
  return {
    title: snapshot.title,
    rowLabels: [...snapshot.rowLabels],
    colLabels: [...snapshot.colLabels],
    cells: snapshot.cells.map((cell) => ({
      id: cell.id,
      row: cell.row,
      col: cell.col,
      title: cell.title ?? '',
      cost: cell.cost,
    })),
    enabledModifierIds: [...snapshot.enabledModifierIds],
    enabledQuestionIds: [...snapshot.enabledQuestionIds],
  }
}

export function getGameSetupCellAt(
  draft: GameSetupDraftState,
  row: number,
  col: number,
): GameSetupCellDraft | undefined {
  return draft.cells.find((cell) => cell.row === row && cell.col === col)
}

export function upsertGameSetupCellDraft(
  draft: GameSetupDraftState,
  row: number,
  col: number,
  patch: Partial<Pick<GameSetupCellDraft, 'title' | 'cost'>>,
): GameSetupDraftState {
  const existing = getGameSetupCellAt(draft, row, col)

  if (existing) {
    return {
      ...draft,
      cells: draft.cells.map((cell) =>
        cell.row === row && cell.col === col ? { ...cell, ...patch } : cell,
      ),
    }
  }

  return {
    ...draft,
    cells: [
      ...draft.cells,
      {
        row,
        col,
        title: patch.title ?? '',
        cost: patch.cost ?? defaultGameSetupRowCost(row),
      },
    ],
  }
}

export function isGameSetupDraftDirty(
  saved: GameSetupDraftState,
  current: GameSetupDraftState,
): boolean {
  if (saved.title !== current.title) {
    return true
  }

  if (saved.rowLabels.length !== current.rowLabels.length) {
    return true
  }

  for (let index = 0; index < saved.rowLabels.length; index += 1) {
    if (saved.rowLabels[index] !== current.rowLabels[index]) {
      return true
    }
  }

  if (saved.colLabels.length !== current.colLabels.length) {
    return true
  }

  for (let index = 0; index < saved.colLabels.length; index += 1) {
    if (saved.colLabels[index] !== current.colLabels[index]) {
      return true
    }
  }

  if (saved.cells.length !== current.cells.length) {
    return true
  }

  if (saved.enabledModifierIds.length !== current.enabledModifierIds.length) {
    return true
  }

  for (let index = 0; index < saved.enabledModifierIds.length; index += 1) {
    if (saved.enabledModifierIds[index] !== current.enabledModifierIds[index]) {
      return true
    }
  }

  if (saved.enabledQuestionIds.length !== current.enabledQuestionIds.length) {
    return true
  }

  for (let index = 0; index < saved.enabledQuestionIds.length; index += 1) {
    if (saved.enabledQuestionIds[index] !== current.enabledQuestionIds[index]) {
      return true
    }
  }

  for (const savedCell of saved.cells) {
    const currentCell = getGameSetupCellAt(current, savedCell.row, savedCell.col)
    if (!currentCell) {
      return true
    }

    if (
      savedCell.id !== currentCell.id ||
      savedCell.title !== currentCell.title ||
      savedCell.cost !== currentCell.cost
    ) {
      return true
    }
  }

  return false
}

function normalizeGameSetupDraftForSave(draft: GameSetupDraftState): GameSetupDraftState {
  const rowLabels = draft.rowLabels.map((label, index) => {
    const trimmed = label.trim()
    return trimmed.length > 0 ? trimmed : defaultGameSetupRowLabel(index)
  })
  const colLabels = draft.colLabels.map((label, index) => {
    const trimmed = label.trim()
    return trimmed.length > 0 ? trimmed : defaultGameSetupColumnLabel(index)
  })

  return {
    title: draft.title.trim(),
    rowLabels,
    colLabels,
    cells: rebuildGameSetupCells(rowLabels, colLabels, draft.cells),
    enabledModifierIds: [...draft.enabledModifierIds].sort((left, right) =>
      left.localeCompare(right),
    ),
    enabledQuestionIds: [...draft.enabledQuestionIds].sort((left, right) =>
      left.localeCompare(right),
    ),
  }
}

export function buildUpdateGameSetupRequest(
  draft: GameSetupDraftState,
  expectedVersion: number,
): UpdateGameSetupRequest {
  const normalized = normalizeGameSetupDraftForSave(draft)

  return {
    expectedVersion,
    title: normalized.title,
    rowLabels: normalized.rowLabels,
    colLabels: normalized.colLabels,
    cells: normalized.cells.map((cell) => {
      const title = cell.title.trim()
      const cost = Number.isFinite(cell.cost) ? Math.max(0, Math.round(cell.cost)) : 0

      return {
        ...(cell.id ? { id: cell.id } : {}),
        row: cell.row,
        col: cell.col,
        title: title.length > 0 ? title : null,
        cost,
      }
    }),
    enabledModifierIds: normalized.enabledModifierIds,
    enabledQuestionIds: normalized.enabledQuestionIds,
  }
}
