import type { ComponentType, LazyExoticComponent } from 'react'
import { GameBoardRealtimeSync } from '../features/game-board/realtime/GameBoardRealtimeSync.tsx'
import { GameModifiersRealtimeSync } from '../features/game-modifiers/realtime/GameModifiersRealtimeSync.tsx'
import { GameSetupRealtimeSync } from '../features/game-setup/realtime/GameSetupRealtimeSync.tsx'
import { lazyPanelPage } from './lazy-panel-page.ts'
import { panelRoutes, type PanelRouteDefinition } from './panel-route-metadata.ts'

type PanelRoutePage = LazyExoticComponent<ComponentType<unknown>>

type PanelRouteConfigEntry = PanelRouteDefinition & {
  Page: PanelRoutePage
  Sync?: ComponentType
}

const panelPages = {
  'game-board': lazyPanelPage(
    () => import('../features/game-board/GameBoardPage.tsx'),
    'GameBoardPage',
  ),
  'game-application': lazyPanelPage(
    () => import('../features/game-application/GameApplicationPage.tsx'),
    'GameApplicationPage',
  ),
  'game-modifiers': lazyPanelPage(
    () => import('../features/game-modifiers/GameModifiersPage.tsx'),
    'GameModifiersPage',
  ),
  'game-quiz': lazyPanelPage(
    () => import('../features/game-quiz/GameQuizPage.tsx'),
    'GameQuizPage',
  ),
  'game-setup': lazyPanelPage(
    () => import('../features/game-setup/GameSetupPage.tsx'),
    'GameSetupPage',
  ),
  'admin-modifiers': lazyPanelPage(
    () => import('../features/game-setup/AdminGameModifiersPage.tsx'),
    'AdminGameModifiersPage',
  ),
  'admin-questions': lazyPanelPage(
    () => import('../features/game-setup/AdminGameQuestionsPage.tsx'),
    'AdminGameQuestionsPage',
  ),
  'catalog-modifiers': lazyPanelPage(
    () => import('../features/game-catalog/CatalogModifiersPage.tsx'),
    'CatalogModifiersPage',
  ),
  'catalog-questions': lazyPanelPage(
    () => import('../features/game-catalog/CatalogQuestionsPage.tsx'),
    'CatalogQuestionsPage',
  ),
  'team-registrations': lazyPanelPage(
    () => import('../features/team-registrations/TeamRegistrationsPage.tsx'),
    'TeamRegistrationsPage',
  ),
} as const satisfies Record<(typeof panelRoutes)[number]['id'], PanelRoutePage>

const panelSyncComponents = {
  'game-board': GameBoardRealtimeSync,
  'game-modifiers': GameModifiersRealtimeSync,
  'game-setup': GameSetupRealtimeSync,
} as const satisfies Partial<Record<(typeof panelRoutes)[number]['id'], ComponentType>>

export const panelRouteConfig = panelRoutes.map((definition) => ({
  ...definition,
  Page: panelPages[definition.id as keyof typeof panelPages],
  ...(definition.id in panelSyncComponents
    ? { Sync: panelSyncComponents[definition.id as keyof typeof panelSyncComponents] }
    : {}),
})) satisfies readonly PanelRouteConfigEntry[]
