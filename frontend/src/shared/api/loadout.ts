import { httpClient } from './client/httpClient.ts'
import type { LoadoutBoard, LoadoutCellId } from './contracts/index.ts'
import * as loadoutMockApi from './mocks/loadoutMock.ts'

export interface LoadoutApi {
  getLoadoutBoard: () => Promise<LoadoutBoard>
  toggleCellPlayed: (cellId: LoadoutCellId) => Promise<LoadoutBoard>
}

function shouldUseMockApi() {
  return (import.meta.env.VITE_API_MODE ?? 'mock') !== 'http'
}

export const loadoutApi: LoadoutApi = {
  getLoadoutBoard: () =>
    shouldUseMockApi()
      ? loadoutMockApi.getLoadoutBoard()
      : httpClient.get<LoadoutBoard>('/loadout'),
  toggleCellPlayed: (cellId) =>
    shouldUseMockApi()
      ? loadoutMockApi.toggleCellPlayed(cellId)
      : Promise.reject(new Error(`Loadout updates are not implemented over HTTP yet for ${cellId}`)),
}
