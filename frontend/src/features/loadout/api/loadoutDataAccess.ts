import { loadoutApi } from '../../../shared/api/loadout.ts'

export function getLoadoutBoard() {
  return loadoutApi.getLoadoutBoard()
}

export function toggleLoadoutCellState(cellId: string) {
  return loadoutApi.toggleCellState(cellId)
}
