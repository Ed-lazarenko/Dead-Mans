import type { SxProps, Theme } from '@mui/material'
import { alpha } from '@mui/material/styles'

interface BoardCellSxOptions {
  isOpen: boolean
  isInteractive: boolean
  isPlayed?: boolean
}

export function createBoardCellSx({
  isOpen,
  isInteractive,
  isPlayed = false,
}: BoardCellSxOptions): SxProps<Theme> {
  return (theme) => ({
    border: isPlayed ? '2px solid' : '1px solid',
    borderColor: isPlayed
      ? alpha(theme.palette.success.main, 0.82)
      : isOpen
        ? alpha(theme.palette.primary.main, 0.58)
        : alpha(theme.palette.divider, 0.86),
    borderRadius: theme.shape.borderRadius,
    position: 'relative',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    aspectRatio: '5 / 6',
    gap: 0.35,
    p: 0.45,
    backgroundColor: isPlayed
      ? alpha(theme.palette.success.main, 0.08)
      : isOpen
        ? alpha(theme.palette.primary.main, 0.06)
        : alpha(theme.palette.background.paper, 0.34),
    cursor: isInteractive ? 'pointer' : 'default',
    transition: 'border-color 0.15s ease, background-color 0.15s ease',
    boxShadow: 'none',
    '&:hover': isInteractive
      ? {
          borderColor: isPlayed ? theme.palette.success.light : theme.palette.primary.light,
          backgroundColor: isPlayed
            ? alpha(theme.palette.success.main, 0.11)
            : alpha(theme.palette.primary.main, 0.08),
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
