import { describe, expect, it } from 'vitest'
import { alpha } from '@mui/material/styles'
import { appTheme } from '../../../app/theme/appTheme.ts'
import { createBoardCellSx } from './board-cell-sx.ts'

describe('createBoardCellSx', () => {
  it('gives closed cards a calm, distinct surface and hover lift', () => {
    const resolver = createBoardCellSx({ isOpen: false, isInteractive: true })
    if (typeof resolver !== 'function') {
      throw new Error('Expected a theme style resolver')
    }

    const styles = resolver(appTheme) as Record<string, unknown>
    expect(styles.background).toBe(alpha(appTheme.palette.primary.main, 0.065))
    expect(styles.borderColor).toBe(alpha(appTheme.palette.primary.main, 0.3))
    expect(styles.boxShadow).toContain('0 2px 8px')
    expect(styles.boxShadow).not.toContain('inset')
    expect(styles['&:hover']).toMatchObject({
      transform: 'translateY(-1px)',
      boxShadow: expect.stringContaining('0 4px 10px'),
    })
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
