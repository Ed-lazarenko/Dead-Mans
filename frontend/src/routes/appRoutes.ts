import { lazy, type ElementType } from 'react'
import type { UserRole } from '../shared/auth/authContext.ts'

export type AppRouteId = 'loadout' | 'leaderboard' | 'modifiers' | 'controls'
export const panelRootPath = '/panel'

const LoadoutPage = lazy(() =>
  import('../features/loadout/LoadoutPage.tsx').then((module) => ({
    default: module.LoadoutPage,
  })),
)
const LeaderboardPage = lazy(() =>
  import('../features/leaderboard/LeaderboardPage.tsx').then((module) => ({
    default: module.LeaderboardPage,
  })),
)
const ModifiersPage = lazy(() =>
  import('../features/modifiers/ModifiersPage.tsx').then((module) => ({
    default: module.ModifiersPage,
  })),
)
const ControlsPage = lazy(() =>
  import('../features/controls/ControlsPage.tsx').then((module) => ({
    default: module.ControlsPage,
  })),
)

export interface AppRoute {
  id: AppRouteId
  /**
   * Path relative to the layout root (without a leading slash), e.g. "loadout".
   */
  path: string
  /**
   * Absolute route path, e.g. "/panel/loadout".
   */
  fullPath: string
  /**
   * i18n key used for tab/page labels.
   */
  labelKey: string
  /**
   * Roles allowed to access this route.
   */
  allowedRoles: UserRole[]
  Component: ElementType
}

export const appRoutes: AppRoute[] = [
  {
    id: 'loadout',
    path: 'loadout',
    fullPath: `${panelRootPath}/loadout`,
    labelKey: 'nav.loadout',
    allowedRoles: ['admin', 'moderator', 'viewer', 'guest'],
    Component: LoadoutPage,
  },
  {
    id: 'leaderboard',
    path: 'leaderboard',
    fullPath: `${panelRootPath}/leaderboard`,
    labelKey: 'nav.leaderboard',
    allowedRoles: ['admin', 'moderator', 'viewer', 'guest'],
    Component: LeaderboardPage,
  },
  {
    id: 'modifiers',
    path: 'modifiers',
    fullPath: `${panelRootPath}/modifiers`,
    labelKey: 'nav.modifiers',
    allowedRoles: ['admin', 'moderator'],
    Component: ModifiersPage,
  },
  {
    id: 'controls',
    path: 'controls',
    fullPath: `${panelRootPath}/controls`,
    labelKey: 'nav.controls',
    allowedRoles: ['admin', 'moderator'],
    Component: ControlsPage,
  },
]

export const defaultRoute = appRoutes[0]

export function getRoutesForRole(role: UserRole): AppRoute[] {
  return appRoutes.filter((route) => route.allowedRoles.includes(role))
}


