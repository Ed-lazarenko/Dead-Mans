import { httpClient } from './client/httpClient.ts'
import { isHttpApiMode } from './config.ts'
import type { GameControlState } from './contracts/index.ts'
import * as controlsMockApi from './mocks/controlsMock.ts'

export interface ControlsApi {
  getControlState: () => Promise<GameControlState>
  startGame: () => Promise<GameControlState>
  pauseGame: () => Promise<GameControlState>
  resumeGame: () => Promise<GameControlState>
  nextRound: () => Promise<GameControlState>
  resetAll: () => Promise<GameControlState>
}

export const controlsApi: ControlsApi = {
  getControlState: () =>
    isHttpApiMode()
      ? httpClient.get<GameControlState>('/game-state')
      : controlsMockApi.getControlState(),
  startGame: () =>
    isHttpApiMode()
      ? httpClient.post<GameControlState>('/game-state/start')
      : controlsMockApi.startGame(),
  pauseGame: () =>
    isHttpApiMode()
      ? httpClient.post<GameControlState>('/game-state/pause')
      : controlsMockApi.pauseGame(),
  resumeGame: () =>
    isHttpApiMode()
      ? httpClient.post<GameControlState>('/game-state/resume')
      : controlsMockApi.resumeGame(),
  nextRound: () =>
    isHttpApiMode()
      ? httpClient.post<GameControlState>('/game-state/next-round')
      : controlsMockApi.nextRound(),
  resetAll: () =>
    isHttpApiMode()
      ? httpClient.post<GameControlState>('/game-state/reset')
      : controlsMockApi.resetAll(),
}
