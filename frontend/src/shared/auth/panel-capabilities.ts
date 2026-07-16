import type { AuthRole } from '../api/contracts/index.ts'

type PanelCapability = 'gameSetup' | 'openGameBoardCell' | 'manageGameCardRuns'

const panelCapabilityRoles: Record<PanelCapability, readonly AuthRole[]> = {
  gameSetup: ['admin'],
  openGameBoardCell: ['admin'],
  manageGameCardRuns: ['admin', 'moderator'],
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
