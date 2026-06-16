import type { ParseKeys } from 'i18next'
import type { ComponentType, LazyExoticComponent } from 'react'
import { lazy } from 'react'
import type { AuthRole } from '../shared/api/contracts/index.ts'
import { GameBoardRealtimeSync } from '../features/game-board/realtime/GameBoardRealtimeSync.tsx'
import { GameSetupRealtimeSync } from '../features/game-setup/realtime/GameSetupRealtimeSync.tsx'

export const panelRootPath = '/panel'

const authenticatedPanelRoles = [
  'viewer',
  'moderator',
  'admin',
] as const satisfies readonly AuthRole[]

type PanelRoutePage = LazyExoticComponent<ComponentType<unknown>>
type PanelRouteLabelKey = Extract<ParseKeys, `navigation.items.${string}.label`>

export type PanelRouteDefinition = {
  id: string
  path: string
  fullPath: string
  labelKey: PanelRouteLabelKey
  allowedRoles?: readonly AuthRole[]
  group: 'player' | 'admin'
}

type PanelRouteConfigInput = Omit<PanelRouteDefinition, 'fullPath'> & {
  Page: PanelRoutePage
  Sync?: ComponentType
}

type PanelRouteConfigEntry = PanelRouteDefinition & {
  Page: PanelRoutePage
  Sync?: ComponentType
}

function createPanelRouteEntry(entry: PanelRouteConfigInput): PanelRouteConfigEntry {
  return {
    ...entry,
    fullPath: `${panelRootPath}/${entry.path}`,
  }
}

function definePanelRouteConfig<const T extends readonly PanelRouteConfigEntry[]>(config: T): T {
  return config
}

export const panelRouteConfig = definePanelRouteConfig([
  createPanelRouteEntry({
    id: 'game-board',
    path: 'game-board',
    labelKey: 'navigation.items.gameBoard.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
    Page: lazy(() =>
      import('../features/game-board/GameBoardPage.tsx').then((module) => ({
        default: module.GameBoardPage,
      })),
    ),
    Sync: GameBoardRealtimeSync,
  }),
  createPanelRouteEntry({
    id: 'game-application',
    path: 'game-application',
    labelKey: 'navigation.items.gameApplication.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
    Page: lazy(() =>
      import('../features/game-application/GameApplicationPage.tsx').then((module) => ({
        default: module.GameApplicationPage,
      })),
    ),
  }),
  createPanelRouteEntry({
    id: 'game-modifiers',
    path: 'game-modifiers',
    labelKey: 'navigation.items.gameModifiers.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
    Page: lazy(() =>
      import('../features/game-modifiers/GameModifiersPage.tsx').then((module) => ({
        default: module.GameModifiersPage,
      })),
    ),
  }),
  createPanelRouteEntry({
    id: 'game-quiz',
    path: 'game-quiz',
    labelKey: 'navigation.items.gameQuiz.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
    Page: lazy(() =>
      import('../features/game-quiz/GameQuizPage.tsx').then((module) => ({
        default: module.GameQuizPage,
      })),
    ),
  }),
  createPanelRouteEntry({
    id: 'game-setup',
    path: 'game-setup',
    labelKey: 'navigation.items.gameSetup.label',
    allowedRoles: ['admin'],
    group: 'admin',
    Page: lazy(() =>
      import('../features/game-setup/GameSetupPage.tsx').then((module) => ({
        default: module.GameSetupPage,
      })),
    ),
    Sync: GameSetupRealtimeSync,
  }),
  createPanelRouteEntry({
    id: 'admin-modifiers',
    path: 'admin-modifiers',
    labelKey: 'navigation.items.adminModifiers.label',
    allowedRoles: ['admin'],
    group: 'admin',
    Page: lazy(() =>
      import('../features/game-setup/AdminGameModifiersPage.tsx').then((module) => ({
        default: module.AdminGameModifiersPage,
      })),
    ),
  }),
  createPanelRouteEntry({
    id: 'admin-questions',
    path: 'admin-questions',
    labelKey: 'navigation.items.adminQuestions.label',
    allowedRoles: ['admin'],
    group: 'admin',
    Page: lazy(() =>
      import('../features/game-setup/AdminGameQuestionsPage.tsx').then((module) => ({
        default: module.AdminGameQuestionsPage,
      })),
    ),
  }),
  createPanelRouteEntry({
    id: 'team-registrations',
    path: 'team-registrations',
    labelKey: 'navigation.items.teamRegistrations.label',
    allowedRoles: ['admin'],
    group: 'admin',
    Page: lazy(() =>
      import('../features/team-registrations/TeamRegistrationsPage.tsx').then((module) => ({
        default: module.TeamRegistrationsPage,
      })),
    ),
  }),
])

type PanelRouteId = (typeof panelRouteConfig)[number]['id']

type PanelRouteMetadata = Omit<PanelRouteConfigEntry, 'Page' | 'Sync'> & {
  id: PanelRouteId
}

function toPanelRouteMetadata(entry: (typeof panelRouteConfig)[number]): PanelRouteMetadata {
  return {
    id: entry.id,
    path: entry.path,
    fullPath: entry.fullPath,
    labelKey: entry.labelKey,
    group: entry.group,
    ...(entry.allowedRoles ? { allowedRoles: entry.allowedRoles } : {}),
  }
}

export const panelRoutes = panelRouteConfig.map(toPanelRouteMetadata)

function requirePanelRoute(routeId: PanelRouteId): PanelRouteMetadata {
  const route = panelRoutes.find(({ id }) => id === routeId)
  if (!route) {
    throw new Error(`Panel route "${routeId}" is not configured`)
  }

  return route
}

export const gameBoardRoute = requirePanelRoute('game-board')
export const gameApplicationRoute = requirePanelRoute('game-application')
export const gameModifiersRoute = requirePanelRoute('game-modifiers')
export const gameQuizRoute = requirePanelRoute('game-quiz')
export const gameSetupRoute = requirePanelRoute('game-setup')
export const adminModifiersRoute = requirePanelRoute('admin-modifiers')
export const adminQuestionsRoute = requirePanelRoute('admin-questions')
export const teamRegistrationsRoute = requirePanelRoute('team-registrations')
