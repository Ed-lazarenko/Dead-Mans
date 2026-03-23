import { loadoutApi } from '../../../shared/api/loadout.ts'

export function getLoadoutBoard() {
  return loadoutApi.getLoadoutBoard()
}
