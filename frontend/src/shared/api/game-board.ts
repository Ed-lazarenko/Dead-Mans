import { httpClient } from './client/httpClient.ts'
import type { GameBoardSnapshot } from './contracts/index.ts'

export const gameBoardApi = {
  getCurrentSnapshot: () => httpClient.get<GameBoardSnapshot>('/game'),
}
