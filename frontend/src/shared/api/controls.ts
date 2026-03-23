import * as controlsMockApi from './controlsMock.ts'
import type { GameControlState } from './contracts.ts'

export interface ControlsApi {
  getControlState: () => Promise<GameControlState>
  startGame: () => Promise<GameControlState>
  pauseGame: () => Promise<GameControlState>
  resumeGame: () => Promise<GameControlState>
  nextRound: () => Promise<GameControlState>
  resetAll: () => Promise<GameControlState>
}

export const controlsApi: ControlsApi = {
  getControlState: () => controlsMockApi.getControlState(),
  startGame: () => controlsMockApi.startGame(),
  pauseGame: () => controlsMockApi.pauseGame(),
  resumeGame: () => controlsMockApi.resumeGame(),
  nextRound: () => controlsMockApi.nextRound(),
  resetAll: () => controlsMockApi.resetAll(),
}
