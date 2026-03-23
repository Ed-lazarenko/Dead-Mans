import type { GameControlState, GamePhase } from './types.ts'

// TODO: перенести эту логику в backend (/api/game-state + SignalR),
// чтобы несколько клиентов (стример, модеры) видели одно и то же состояние игры.

let state: GameControlState = {
  phase: 'idle',
  currentRound: 1,
  totalRounds: 3,
  lastActionAt: null,
}

function setState(partial: Partial<GameControlState>) {
  state = {
    ...state,
    ...partial,
    lastActionAt: new Date().toISOString(),
  }
}

export async function getControlState(): Promise<GameControlState> {
  await new Promise((resolve) => setTimeout(resolve, 120))
  return state
}

export async function startGame(): Promise<GameControlState> {
  setState({ phase: 'running', currentRound: 1 })
  return getControlState()
}

export async function pauseGame(): Promise<GameControlState> {
  if (state.phase === 'running') {
    setState({ phase: 'paused' })
  }
  return getControlState()
}

export async function resumeGame(): Promise<GameControlState> {
  if (state.phase === 'paused') {
    setState({ phase: 'running' })
  }
  return getControlState()
}

export async function nextRound(): Promise<GameControlState> {
  const next = Math.min(state.currentRound + 1, state.totalRounds)
  setState({ currentRound: next, phase: (next >= state.totalRounds ? 'finished' : state.phase) as GamePhase })
  return getControlState()
}

export async function resetAll(): Promise<GameControlState> {
  state = {
    phase: 'idle',
    currentRound: 1,
    totalRounds: 3,
    lastActionAt: new Date().toISOString(),
  }
  return getControlState()
}


