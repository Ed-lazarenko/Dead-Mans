import type { ButtonProps } from '@mui/material'

export type AppButtonTone =
  'primary' | 'secondary' | 'danger' | 'dangerSecondary' | 'ghost' | 'warningGhost' | 'success'

export function resolveAppButtonTone(tone: AppButtonTone): Pick<ButtonProps, 'variant' | 'color'> {
  switch (tone) {
    case 'secondary':
      return { variant: 'outlined', color: 'primary' }
    case 'danger':
      return { variant: 'contained', color: 'error' }
    case 'dangerSecondary':
      return { variant: 'outlined', color: 'error' }
    case 'warningGhost':
      return { variant: 'text', color: 'warning' }
    case 'success':
      return { variant: 'contained', color: 'success' }
    case 'ghost':
      return { variant: 'text', color: 'inherit' }
    default:
      return { variant: 'contained', color: 'primary' }
  }
}
