import { httpClient } from './client/httpClient.ts'
import type { LoadoutBoard, LoadoutCellId } from './contracts/index.ts'

export interface LoadoutApi {
  getLoadoutBoard: () => Promise<LoadoutBoard>
  toggleCellPlayed: (cellId: LoadoutCellId) => Promise<LoadoutBoard>
}

export const loadoutApi: LoadoutApi = {
  getLoadoutBoard: () => httpClient.get<LoadoutBoard>('/loadout'),
  toggleCellPlayed: (cellId) => httpClient.post<LoadoutBoard>(`/loadout/${cellId}/toggle`),
}
