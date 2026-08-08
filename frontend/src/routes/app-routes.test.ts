import { describe, expect, it } from 'vitest'
import {
  adminModifiersRoute,
  adminQuestionsRoute,
  catalogModifiersRoute,
  catalogQuestionsRoute,
  gameApplicationRoute,
  gameBoardRoute,
  gameHistoryRoute,
  gameLeaderboardRoute,
  gameModifiersRoute,
  gameQuizRoute,
  gameSetupRoute,
  getAccessiblePanelRoutes,
  getPanelRouteByPath,
  hasAccessToPanelRoute,
  teamRegistrationsRoute,
} from './app-routes.ts'

describe('panel route helpers', () => {
  it('keeps player navigation focused on player routes', () => {
    expect(getAccessiblePanelRoutes(['viewer'])).toEqual([
      gameBoardRoute,
      gameLeaderboardRoute,
      gameApplicationRoute,
      gameModifiersRoute,
      gameQuizRoute,
      gameHistoryRoute,
    ])
  })

  it('includes administration routes for admins', () => {
    expect(getAccessiblePanelRoutes(['admin'])).toEqual([
      gameBoardRoute,
      gameLeaderboardRoute,
      gameApplicationRoute,
      gameModifiersRoute,
      gameQuizRoute,
      gameHistoryRoute,
      gameSetupRoute,
      adminModifiersRoute,
      adminQuestionsRoute,
      catalogModifiersRoute,
      catalogQuestionsRoute,
      teamRegistrationsRoute,
    ])
  })

  it('allows moderators to manage team registrations without exposing admin setup pages', () => {
    expect(getAccessiblePanelRoutes(['moderator'])).toEqual([
      gameBoardRoute,
      gameLeaderboardRoute,
      gameApplicationRoute,
      gameModifiersRoute,
      gameQuizRoute,
      gameHistoryRoute,
      teamRegistrationsRoute,
    ])
  })

  it('resolves nested panel paths to their route metadata', () => {
    expect(getPanelRouteByPath('/panel/game-board/cell/1')).toBe(gameBoardRoute)
    expect(getPanelRouteByPath('/outside')).toBeNull()
  })

  it('denies restricted routes without a matching authenticated role', () => {
    expect(hasAccessToPanelRoute(gameSetupRoute, undefined)).toBe(false)
    expect(hasAccessToPanelRoute(gameSetupRoute, [])).toBe(false)
    expect(hasAccessToPanelRoute(gameSetupRoute, ['viewer'])).toBe(false)
    expect(hasAccessToPanelRoute(gameSetupRoute, ['admin'])).toBe(true)
  })

  it('allows routes that do not declare role restrictions', () => {
    const unrestrictedRoute = { ...gameBoardRoute }
    delete unrestrictedRoute.allowedRoles

    expect(hasAccessToPanelRoute(unrestrictedRoute, undefined)).toBe(true)
  })
})
