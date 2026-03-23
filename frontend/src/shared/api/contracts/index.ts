import type { components } from './generated.ts'

export type TeamId = components['schemas']['LeaderboardTeamDto']['id']
export type LeaderboardTeam = components['schemas']['LeaderboardTeamDto']
export type LeaderboardSummary = components['schemas']['LeaderboardSummaryDto']

export type LoadoutCellId = components['schemas']['LoadoutCellDto']['id']
export type LoadoutCellState = components['schemas']['LoadoutCellDto']['state']
export type LoadoutCell = components['schemas']['LoadoutCellDto']
export type LoadoutBoard = components['schemas']['LoadoutBoardDto']

export type ModifierId = components['schemas']['ModifierDefinitionDto']['id']
export type ModifierDefinition = components['schemas']['ModifierDefinitionDto']
export type ActiveModifier = components['schemas']['ActiveModifierDto']
export type ModifiersSnapshot = components['schemas']['ModifiersSnapshotDto']
export type ActivateModifierInput = components['schemas']['ActivateModifierRequest']

export type GamePhase = components['schemas']['GameControlStateDto']['phase']
export type GameControlState = components['schemas']['GameControlStateDto']
