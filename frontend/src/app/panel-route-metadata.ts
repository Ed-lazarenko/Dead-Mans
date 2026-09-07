import type { ParseKeys } from 'i18next'
import type { AuthRole } from '../shared/api/contracts/index.ts'

export const panelRootPath = '/panel'

const authenticatedPanelRoles = [
  'viewer',
  'moderator',
  'admin',
] as const satisfies readonly AuthRole[]

type PanelRouteLabelKey = Extract<ParseKeys, `navigation.items.${string}.label`>

type PanelAdminSection = 'current-game' | 'catalog'

type PanelRouteDefinitionInput = {
  id: string
  path: string
  labelKey: PanelRouteLabelKey
  allowedRoles?: readonly AuthRole[]
  group: 'player' | 'admin'
  adminSection?: PanelAdminSection
}

export type PanelRouteDefinition = PanelRouteDefinitionInput & {
  fullPath: string
}

function createPanelRouteDefinition(entry: PanelRouteDefinitionInput): PanelRouteDefinition {
  return {
    ...entry,
    fullPath: `${panelRootPath}/${entry.path}`,
  }
}

function definePanelRouteDefinitions<const T extends readonly PanelRouteDefinition[]>(
  definitions: T,
): T {
  return definitions
}

export const panelRoutes = definePanelRouteDefinitions([
  createPanelRouteDefinition({
    id: 'game-board',
    path: 'game-board',
    labelKey: 'navigation.items.gameBoard.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-leaderboard',
    path: 'game-leaderboard',
    labelKey: 'navigation.items.gameLeaderboard.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-application',
    path: 'game-application',
    labelKey: 'navigation.items.gameApplication.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-modifiers',
    path: 'game-modifiers',
    labelKey: 'common.entities.modifiers',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-quiz',
    path: 'game-quiz',
    labelKey: 'navigation.items.gameQuiz.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-history',
    path: 'game-history',
    labelKey: 'navigation.items.gameHistory.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'modifier-history',
    path: 'modifier-history',
    labelKey: 'navigation.items.modifierHistory.label',
    allowedRoles: authenticatedPanelRoles,
    group: 'player',
  }),
  createPanelRouteDefinition({
    id: 'game-setup',
    path: 'game-setup',
    labelKey: 'navigation.items.gameSetup.label',
    allowedRoles: ['admin'],
    group: 'admin',
    adminSection: 'current-game',
  }),
  createPanelRouteDefinition({
    id: 'admin-modifiers',
    path: 'admin-modifiers',
    labelKey: 'navigation.items.adminModifiers.label',
    allowedRoles: ['admin'],
    group: 'admin',
    adminSection: 'current-game',
  }),
  createPanelRouteDefinition({
    id: 'admin-questions',
    path: 'admin-questions',
    labelKey: 'navigation.items.adminQuestions.label',
    allowedRoles: ['admin'],
    group: 'admin',
    adminSection: 'current-game',
  }),
  createPanelRouteDefinition({
    id: 'catalog-modifiers',
    path: 'catalog-modifiers',
    labelKey: 'navigation.items.catalogModifiers.label',
    allowedRoles: ['admin'],
    group: 'admin',
    adminSection: 'catalog',
  }),
  createPanelRouteDefinition({
    id: 'catalog-questions',
    path: 'catalog-questions',
    labelKey: 'navigation.items.catalogQuestions.label',
    allowedRoles: ['admin'],
    group: 'admin',
    adminSection: 'catalog',
  }),
  createPanelRouteDefinition({
    id: 'team-registrations',
    path: 'team-registrations',
    labelKey: 'common.entities.teams',
    allowedRoles: ['admin', 'moderator'],
    group: 'admin',
    adminSection: 'current-game',
  }),
])

type PanelRouteId = (typeof panelRoutes)[number]['id']

function requirePanelRoute(routeId: PanelRouteId): PanelRouteDefinition {
  const route = panelRoutes.find(({ id }) => id === routeId)
  if (!route) {
    throw new Error(`Panel route "${routeId}" is not configured`)
  }

  return route
}

export const gameBoardRoute = requirePanelRoute('game-board')
export const gameLeaderboardRoute = requirePanelRoute('game-leaderboard')
export const gameHistoryRoute = requirePanelRoute('game-history')
export const modifierHistoryRoute = requirePanelRoute('modifier-history')
export const gameApplicationRoute = requirePanelRoute('game-application')
export const gameModifiersRoute = requirePanelRoute('game-modifiers')
export const gameQuizRoute = requirePanelRoute('game-quiz')
export const gameSetupRoute = requirePanelRoute('game-setup')
export const adminModifiersRoute = requirePanelRoute('admin-modifiers')
export const adminQuestionsRoute = requirePanelRoute('admin-questions')
export const catalogModifiersRoute = requirePanelRoute('catalog-modifiers')
export const catalogQuestionsRoute = requirePanelRoute('catalog-questions')
export const teamRegistrationsRoute = requirePanelRoute('team-registrations')
