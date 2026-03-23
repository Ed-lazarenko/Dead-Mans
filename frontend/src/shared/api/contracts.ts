export type TeamId = string

export interface LeaderboardTeam {
  id: TeamId
  name: string
  colorHex: string
  score: number
  penalty: number
}

export interface LeaderboardSummary {
  updatedAt: string
  teams: LeaderboardTeam[]
}

export type LoadoutCellId = string

export type LoadoutCellState = 'available' | 'played' | 'locked'

export interface LoadoutCell {
  id: LoadoutCellId
  row: number
  col: number
  label: string
  points: number
  /**
   * Относительный путь к изображению ячейки (например, /Loadouts/Стихии/1-1.png).
   * В продакшене будет приходить с backend.
   */
  imageUrl?: string
  state: LoadoutCellState
}

export interface LoadoutBoard {
  rows: number
  cols: number
  /**
   * Подписи строк (стоимость отряда и т.п.). Длина должна быть равна rows.
   */
  rowLabels: string[]
  /**
   * Подписи колонок (типы охотников и т.п.). Длина должна быть равна cols.
   */
  colLabels: string[]
  cells: LoadoutCell[]
}

export type ModifierId = string

export interface ModifierDefinition {
  id: ModifierId
  name: string
  cost: number
  description: string
}

export interface ActiveModifier {
  id: string
  modifierId: ModifierId
  activatedAt: string
  triggeredBy: string
}

export interface ModifiersSnapshot {
  available: ModifierDefinition[]
  active: ActiveModifier[]
}

export type GamePhase = 'idle' | 'running' | 'paused' | 'finished'

export interface GameControlState {
  phase: GamePhase
  currentRound: number
  totalRounds: number
  lastActionAt: string | null
}
