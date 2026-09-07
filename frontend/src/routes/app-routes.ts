import type { AuthRole } from '../shared/api/contracts/index.ts'

export {
  adminModifiersRoute,
  adminQuestionsRoute,
  catalogModifiersRoute,
  catalogQuestionsRoute,
  gameApplicationRoute,
  gameBoardRoute,
  gameHistoryRoute,
  modifierHistoryRoute,
  gameLeaderboardRoute,
  gameModifiersRoute,
  gameQuizRoute,
  gameSetupRoute,
  panelRootPath,
  teamRegistrationsRoute,
  type PanelRouteDefinition,
} from '../app/panel-route-metadata.ts'

import { panelRoutes, type PanelRouteDefinition } from '../app/panel-route-metadata.ts'

export function hasAccessToPanelRoute(
  route: PanelRouteDefinition,
  roles: readonly AuthRole[] | undefined,
) {
  if (!route.allowedRoles || route.allowedRoles.length === 0) {
    return true
  }

  if (!roles || roles.length === 0) {
    return false
  }

  return roles.some((role) => route.allowedRoles?.includes(role))
}

export function getAccessiblePanelRoutes(roles: readonly AuthRole[] | undefined) {
  return panelRoutes.filter((route) => hasAccessToPanelRoute(route, roles))
}

export function getPanelRouteByPath(pathname: string) {
  return (
    panelRoutes.find(
      (route) => pathname === route.fullPath || pathname.startsWith(`${route.fullPath}/`),
    ) ?? null
  )
}
