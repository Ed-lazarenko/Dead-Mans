import { Box, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { huntBrassTitleSx } from '../../theme/surface-sx.ts'

interface SectionHeaderProps {
  title: ReactNode
  description?: ReactNode
  actions?: ReactNode
  textAlign?: 'left' | 'center'
}

export function SectionHeader({
  title,
  description,
  actions,
  textAlign = 'left',
}: SectionHeaderProps) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      justifyContent="space-between"
      alignItems={{ xs: 'stretch', sm: 'flex-start' }}
    >
      <Box sx={{ minWidth: 0, width: textAlign === 'center' ? '100%' : 'auto', textAlign }}>
        <Typography variant="subtitle1" sx={huntBrassTitleSx}>
          {title}
        </Typography>
        {description ? (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {description}
          </Typography>
        ) : null}
      </Box>
      {actions ? <Box>{actions}</Box> : null}
    </Stack>
  )
}
