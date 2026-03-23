import type { LoadoutCellId } from '../api/contracts.ts'

const LOADOUT_OPENED_STORAGE_KEY = 'loadout-opened-cells-v1'

export function readOpenedLoadoutCellIds(): LoadoutCellId[] {
  if (typeof window === 'undefined') {
    return []
  }

  try {
    const raw = window.localStorage.getItem(LOADOUT_OPENED_STORAGE_KEY)
    if (!raw) {
      return []
    }

    const parsed = JSON.parse(raw) as unknown
    return Array.isArray(parsed) ? (parsed as LoadoutCellId[]) : []
  } catch {
    return []
  }
}

export function writeOpenedLoadoutCellIds(cellIds: Iterable<LoadoutCellId>) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(LOADOUT_OPENED_STORAGE_KEY, JSON.stringify(Array.from(cellIds)))
}

export function clearOpenedLoadoutCellIds() {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.removeItem(LOADOUT_OPENED_STORAGE_KEY)
}
