import type { SxProps, Theme } from '@mui/material'
import { alpha } from '@mui/material/styles'

interface BoardCellSxOptions {
  isOpen: boolean
  isInteractive: boolean
}

export function createBoardCellSx({ isOpen, isInteractive }: BoardCellSxOptions): SxProps<Theme> {
  return (theme) => ({
    border: '1px solid',
    borderColor: isOpen ? theme.palette.primary.main : alpha(theme.palette.primary.main, 0.28),
    borderWidth: isOpen ? 2 : 1,
    borderRadius: theme.shape.borderRadius,
    position: 'relative',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    aspectRatio: '5 / 6',
    gap: 0.5,
    p: 0.5,
    backgroundColor: isOpen
      ? alpha(theme.palette.primary.main, 0.08)
      : alpha(theme.palette.common.black, 0.18),
    cursor: isInteractive ? 'pointer' : 'default',
    transition: 'border-color 0.15s ease, transform 0.15s ease, box-shadow 0.15s ease',
    boxShadow: isOpen
      ? `inset 0 0 0 1px ${alpha(theme.palette.primary.light, 0.18)}`
      : `inset 0 1px 0 ${alpha(theme.palette.primary.light, 0.05)}`,
    '&:hover': isInteractive
      ? {
          borderColor: theme.palette.primary.light,
          transform: 'translateY(-1px)',
          boxShadow: `0 6px 16px ${alpha(theme.palette.common.black, 0.35)}`,
        }
      : undefined,
    '&:focus-visible': isInteractive
      ? {
          outline: '2px solid',
          outlineColor: theme.palette.primary.main,
          outlineOffset: 2,
        }
      : undefined,
  })
}
