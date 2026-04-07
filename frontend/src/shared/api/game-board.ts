import { httpClient } from './client/httpClient.ts'
import type { GameBoardCellId, GameBoardSnapshot } from './contracts/index.ts'

export const gameBoardApi = {
  getCurrentSnapshot: () => httpClient.get<GameBoardSnapshot>('/game'),
  openCell: (cellId: GameBoardCellId) => httpClient.post<void>(`/game/cells/${cellId}/open`),
}
