import { Box, ButtonBase, Collapse, Stack, Typography } from '@mui/material'
import { useState, type ReactNode } from 'react'
import { huntInsetSurfaceSx } from '../../../shared/theme/surface-sx.ts'

interface CollapsibleToolGroupProps {
  panelId: string
  title: string
  description?: string
  expandLabel: string
  collapseLabel: string
  defaultExpanded?: boolean
  children: ReactNode
}

export function CollapsibleToolGroup({
  panelId,
  title,
  description,
  expandLabel,
  collapseLabel,
  defaultExpanded = false,
  children,
}: CollapsibleToolGroupProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded)

  return (
    <Box
      sx={[
        (theme) => huntInsetSurfaceSx(theme),
        (theme) => ({
          borderRadius: 1,
          p: 1.25,
          borderLeft: `3px solid ${theme.palette.primary.main}`,
        }),
      ]}
    >
      <ButtonBase
        onClick={() => setIsExpanded((expanded) => !expanded)}
        aria-expanded={isExpanded}
        aria-controls={panelId}
        aria-label={isExpanded ? collapseLabel : expandLabel}
        sx={{
          width: '100%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          position: 'relative',
          gap: 1,
          borderRadius: 0.5,
          textAlign: 'center',
        }}
      >
        <Typography variant="button" color="primary" sx={{ textAlign: 'center' }}>
          {title}
        </Typography>
        <Typography
          component="span"
          aria-hidden
          sx={{
            position: 'absolute',
            right: 0,
            color: 'text.secondary',
            fontSize: 12,
            flexShrink: 0,
          }}
        >
          {isExpanded ? '▾' : '▸'}
        </Typography>
      </ButtonBase>

      <Collapse id={panelId} in={isExpanded}>
        {description ? (
          <Typography
            variant="body2"
            sx={{
              display: 'block',
              mt: 0.75,
              mb: 1,
              fontWeight: 600,
              color: 'text.primary',
              textAlign: 'center',
            }}
          >
            {description}
          </Typography>
        ) : null}
        <Stack spacing={0.75}>{children}</Stack>
      </Collapse>
    </Box>
  )
}
