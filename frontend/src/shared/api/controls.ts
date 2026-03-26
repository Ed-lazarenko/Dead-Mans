import { httpClient } from './client/httpClient.ts'
import type { GameControlState } from './contracts/index.ts'

export interface ControlsApi {
  getControlState: () => Promise<GameControlState>
  startGame: () => Promise<GameControlState>
  pauseGame: () => Promise<GameControlState>
  resumeGame: () => Promise<GameControlState>
  nextRound: () => Promise<GameControlState>
  resetAll: () => Promise<GameControlState>
}

export const controlsApi: ControlsApi = {
  getControlState: () => httpClient.get<GameControlState>('/game-state'),
  startGame: () => httpClient.post<GameControlState>('/game-state/start'),
  pauseGame: () => httpClient.post<GameControlState>('/game-state/pause'),
  resumeGame: () => httpClient.post<GameControlState>('/game-state/resume'),
  nextRound: () => httpClient.post<GameControlState>('/game-state/next-round'),
  resetAll: () => httpClient.post<GameControlState>('/game-state/reset'),
}
