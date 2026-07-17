import type { AuthRole } from '../api/contracts/index.ts'

type PanelCapability =
  | 'gameSetup'
  | 'openGameBoardCell'
  | 'manageGame'
  | 'manageGameCardRuns'
  | 'startGame'

const panelCapabilityRoles: Record<PanelCapability, readonly AuthRole[]> = {
  gameSetup: ['admin'],
  openGameBoardCell: ['admin'],
  manageGame: ['admin', 'moderator'],
  manageGameCardRuns: ['admin', 'moderator'],
  startGame: ['admin'],
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
