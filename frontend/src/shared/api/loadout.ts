import { httpClient } from './client/httpClient.ts'
import { isHttpApiMode } from './config.ts'
import type { LoadoutBoard, LoadoutCellId } from './contracts/index.ts'
import * as loadoutMockApi from './mocks/loadoutMock.ts'

export interface LoadoutApi {
  getLoadoutBoard: () => Promise<LoadoutBoard>
  toggleCellPlayed: (cellId: LoadoutCellId) => Promise<LoadoutBoard>
}

export const loadoutApi: LoadoutApi = {
  getLoadoutBoard: () =>
    isHttpApiMode()
      ? httpClient.get<LoadoutBoard>('/loadout')
      : loadoutMockApi.getLoadoutBoard(),
  toggleCellPlayed: (cellId) =>
    isHttpApiMode()
      ? Promise.reject(new Error(`Loadout updates are not implemented over HTTP yet for ${cellId}`))
      : loadoutMockApi.toggleCellPlayed(cellId),
}
