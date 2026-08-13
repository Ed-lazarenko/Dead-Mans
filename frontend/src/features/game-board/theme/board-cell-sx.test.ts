import { describe, expect, it } from 'vitest'
import { appTheme } from '../../../app/theme/appTheme.ts'
import { createBoardCellSx } from './board-cell-sx.ts'

describe('createBoardCellSx', () => {
  it('gives closed cards a layered surface and hover lift', () => {
    const resolver = createBoardCellSx({ isOpen: false, isInteractive: true })
    if (typeof resolver !== 'function') {
      throw new Error('Expected a theme style resolver')
    }

    const styles = resolver(appTheme) as Record<string, unknown>
    expect(styles.background).toContain('linear-gradient')
    expect(styles.boxShadow).not.toBe('none')
    expect(styles['&:hover']).toMatchObject({ transform: 'translateY(-2px)' })
  })

  it('adds a stronger non-color-only treatment to the active round card', () => {
    const resolver = createBoardCellSx({
      isOpen: true,
      isInteractive: true,
      isActiveRound: true,
    })
    if (typeof resolver !== 'function') {
      throw new Error('Expected a theme style resolver')
    }

    const styles = resolver(appTheme) as Record<string, unknown>
    expect(styles.border).toBe('2px solid')
    expect(styles.boxShadow).toContain('0 0 0 3px')
  })
})
