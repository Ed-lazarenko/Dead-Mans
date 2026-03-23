import { controlsApi } from '../../../shared/api/controls.ts'

export function getGameControlState() {
  return controlsApi.getControlState()
}

export function startGame() {
  return controlsApi.startGame()
}

export function pauseGame() {
  return controlsApi.pauseGame()
}

export function resumeGame() {
  return controlsApi.resumeGame()
}

export function nextRound() {
  return controlsApi.nextRound()
}

export function resetGame() {
  return controlsApi.resetAll()
}
