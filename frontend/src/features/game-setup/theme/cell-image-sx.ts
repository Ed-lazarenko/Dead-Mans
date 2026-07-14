import type { SxProps, Theme } from '@mui/material'
import type { SystemStyleObject } from '@mui/system'
import { alpha } from '@mui/material/styles'

interface SetupCellDropzoneSxOptions {
  showDragOver: boolean
}

export function createSetupCellDropzoneSx({
  showDragOver,
}: SetupCellDropzoneSxOptions): SxProps<Theme> {
  return (theme) => ({
    flex: 1,
    borderRadius: theme.shape.borderRadius,
    border: '1px dashed',
    borderColor: showDragOver
      ? theme.palette.primary.main
      : alpha(theme.palette.primary.main, 0.24),
    backgroundColor: showDragOver
      ? alpha(theme.palette.primary.main, 0.1)
      : alpha(theme.palette.common.black, 0.12),
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'stretch',
    justifyContent: 'center',
    textAlign: 'center',
    color: 'text.secondary',
    overflow: 'hidden',
    position: 'relative',
    minHeight: 96,
    transition: 'border-color 160ms ease, background-color 160ms ease',
  })
}

export const setupCellImagePreviewSx: SystemStyleObject<Theme> = {
  position: 'absolute',
  inset: 0,
  width: '100%',
  height: '100%',
  objectFit: 'cover',
  transition: 'opacity 160ms ease',
}

export const setupCellImageLabelSx: SystemStyleObject<Theme> = {
  position: 'absolute',
  inset: 0,
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 0.75,
  textAlign: 'center',
}

export const setupCellDragOverlaySx: SxProps<Theme> = (theme) => ({
  position: 'absolute',
  inset: 0,
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  backgroundColor: alpha(theme.palette.primary.main, 0.16),
  border: '2px dashed',
  borderColor: theme.palette.primary.main,
  zIndex: 2,
})

export const setupCellBusyOverlaySx: SxProps<Theme> = (theme) => ({
  position: 'absolute',
  inset: 0,
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 0.75,
  backgroundColor: alpha(theme.palette.common.black, 0.55),
  zIndex: 3,
})

export const setupCellMediaActionsSx: SystemStyleObject<Theme> = {
  position: 'absolute',
  left: 4,
  right: 4,
  bottom: 4,
  zIndex: 4,
}
