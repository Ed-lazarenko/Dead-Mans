import type { SxProps, Theme } from '@mui/material'
import { alpha } from '@mui/material/styles'

interface BoardCellSxOptions {
  isOpen: boolean
  isInteractive: boolean
  isPlayed?: boolean
  isActiveRound?: boolean
}

export function createBoardCellSx({
  isOpen,
  isInteractive,
  isPlayed = false,
  isActiveRound = false,
}: BoardCellSxOptions): SxProps<Theme> {
  return (theme) => ({
    border: isPlayed || isActiveRound ? '2px solid' : '1px solid',
    borderColor: isPlayed
      ? alpha(theme.palette.success.main, 0.82)
      : isActiveRound
        ? alpha(theme.palette.warning.main, 0.92)
        : isOpen
          ? alpha(theme.palette.primary.main, 0.58)
          : alpha(theme.palette.primary.main, 0.3),
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
    background: isPlayed
      ? `linear-gradient(160deg, ${alpha(theme.palette.success.main, 0.12)}, ${alpha(theme.palette.background.paper, 0.5)})`
      : isActiveRound
        ? `linear-gradient(160deg, ${alpha(theme.palette.warning.main, 0.2)}, ${alpha(theme.palette.background.paper, 0.62)})`
        : isOpen
          ? `linear-gradient(160deg, ${alpha(theme.palette.primary.main, 0.1)}, ${alpha(theme.palette.background.paper, 0.52)})`
          : alpha(theme.palette.primary.main, 0.065),
    cursor: isInteractive ? 'pointer' : 'default',
    transition: 'border-color 0.15s ease, transform 0.15s ease, box-shadow 0.15s ease',
    boxShadow: isActiveRound
      ? `0 0 0 3px ${alpha(theme.palette.warning.main, 0.16)}, 0 12px 28px ${alpha(theme.palette.common.black, 0.28)}, inset 0 1px 0 ${alpha(theme.palette.common.white, 0.12)}`
      : isOpen
        ? `0 8px 20px ${alpha(theme.palette.common.black, 0.18)}, inset 0 1px 0 ${alpha(theme.palette.common.white, 0.08)}`
        : `0 2px 8px ${alpha(theme.palette.common.black, 0.1)}`,
    '&:hover': isInteractive
      ? {
          borderColor: isPlayed ? theme.palette.success.light : theme.palette.primary.light,
          backgroundColor: isPlayed
            ? alpha(theme.palette.success.main, 0.11)
            : alpha(theme.palette.primary.main, 0.08),
          transform: isOpen ? 'translateY(-2px)' : 'translateY(-1px)',
          boxShadow: isOpen
            ? `0 12px 28px ${alpha(theme.palette.common.black, 0.26)}, inset 0 1px 0 ${alpha(theme.palette.common.white, 0.12)}`
            : `0 4px 10px ${alpha(theme.palette.common.black, 0.14)}`,
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
