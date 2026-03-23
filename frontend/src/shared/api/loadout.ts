import * as loadoutMockApi from './loadoutMock.ts'
import type { LoadoutBoard, LoadoutCellId } from './contracts.ts'

export interface LoadoutApi {
  getLoadoutBoard: () => Promise<LoadoutBoard>
  toggleCellPlayed: (cellId: LoadoutCellId) => Promise<LoadoutBoard>
}

export const loadoutApi: LoadoutApi = {
  getLoadoutBoard: () => loadoutMockApi.getLoadoutBoard(),
  toggleCellPlayed: (cellId) => loadoutMockApi.toggleCellPlayed(cellId),
}
