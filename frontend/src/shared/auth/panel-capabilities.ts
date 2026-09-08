import type { AuthRole } from '../api/contracts/index.ts'

type PanelCapability =
  'gameSetup' | 'openGameBoardCell' | 'manageGame' | 'manageGameRounds' | 'startGame' | 'finishGame'

const panelCapabilityRoles: Record<PanelCapability, readonly AuthRole[]> = {
  gameSetup: ['admin'],
  openGameBoardCell: ['admin'],
  manageGame: ['admin', 'moderator'],
  manageGameRounds: ['admin', 'moderator'],
  startGame: ['admin'],
  finishGame: ['admin'],
}

export function hasPanelCapability(
  capability: PanelCapability,
  roles: readonly AuthRole[] | undefined,
) {
  if (!roles || roles.length === 0) {
    return false
  }

  return roles.some((role) => panelCapabilityRoles[capability].includes(role))
}
