import { describe, expect, it } from 'vitest'
import { hasPanelCapability } from './panel-capabilities.ts'

describe('panel capabilities', () => {
  it('denies access when the session has no roles', () => {
    expect(hasPanelCapability('gameSetup', undefined)).toBe(false)
    expect(hasPanelCapability('gameSetup', [])).toBe(false)
  })

  it('keeps game setup restricted to admins', () => {
    expect(hasPanelCapability('gameSetup', ['admin'])).toBe(true)
    expect(hasPanelCapability('gameSetup', ['moderator'])).toBe(false)
    expect(hasPanelCapability('gameSetup', ['viewer'])).toBe(false)
  })

  it('keeps opening game board cells restricted to admins', () => {
    expect(hasPanelCapability('openGameBoardCell', ['admin'])).toBe(true)
    expect(hasPanelCapability('openGameBoardCell', ['moderator'])).toBe(false)
    expect(hasPanelCapability('openGameBoardCell', ['viewer'])).toBe(false)
  })

  it('allows moderators and admins to manage card runs', () => {
    expect(hasPanelCapability('manageGameCardRuns', ['admin'])).toBe(true)
    expect(hasPanelCapability('manageGameCardRuns', ['moderator'])).toBe(true)
    expect(hasPanelCapability('manageGameCardRuns', ['viewer'])).toBe(false)
  })

  it('allows moderators and admins to see game management controls', () => {
    expect(hasPanelCapability('manageGame', ['admin'])).toBe(true)
    expect(hasPanelCapability('manageGame', ['moderator'])).toBe(true)
    expect(hasPanelCapability('manageGame', ['viewer'])).toBe(false)
  })

  it('keeps game launch restricted to admins', () => {
    expect(hasPanelCapability('startGame', ['admin'])).toBe(true)
    expect(hasPanelCapability('startGame', ['moderator'])).toBe(false)
    expect(hasPanelCapability('startGame', ['viewer'])).toBe(false)
  })
})
