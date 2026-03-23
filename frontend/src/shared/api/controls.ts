import { httpClient } from './client/httpClient.ts'
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

function shouldUseMockApi() {
  return (import.meta.env.VITE_API_MODE ?? 'mock') !== 'http'
}

export const controlsApi: ControlsApi = {
  getControlState: () =>
    shouldUseMockApi()
      ? controlsMockApi.getControlState()
      : httpClient.get<GameControlState>('/game-state'),
  startGame: () =>
    shouldUseMockApi()
      ? controlsMockApi.startGame()
      : httpClient.post<GameControlState>('/game-state/start'),
  pauseGame: () =>
    shouldUseMockApi()
      ? controlsMockApi.pauseGame()
      : httpClient.post<GameControlState>('/game-state/pause'),
  resumeGame: () =>
    shouldUseMockApi()
      ? controlsMockApi.resumeGame()
      : httpClient.post<GameControlState>('/game-state/resume'),
  nextRound: () =>
    shouldUseMockApi()
      ? controlsMockApi.nextRound()
      : httpClient.post<GameControlState>('/game-state/next-round'),
  resetAll: () =>
    shouldUseMockApi()
      ? controlsMockApi.resetAll()
      : httpClient.post<GameControlState>('/game-state/reset'),
}
