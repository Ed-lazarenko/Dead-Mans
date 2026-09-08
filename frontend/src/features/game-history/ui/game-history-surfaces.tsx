import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Stack,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import type { ReactNode } from 'react'
import type { components } from '../../../shared/api/contracts/generated'
import { PlayedCardPreviewDialog } from '../../../shared/ui/index.ts'
import { MiniMetricChip } from './game-history-display.tsx'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function CardPreviewDialog({
  round,
  onClose,
}: {
  round: GameHistoryRound | null
  onClose: () => void
}) {
  return <PlayedCardPreviewDialog card={null} round={round} onClose={onClose} />
}

export function AccordionSurface({
  children,
  defaultExpanded = false,
  highlighted = false,
}: {
  children: ReactNode
  defaultExpanded?: boolean
  highlighted?: boolean
}) {
  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      sx={(theme) => ({
        borderRadius: 2.5,
        border: `1px solid ${
          highlighted ? alpha(theme.palette.warning.main, 0.72) : alpha(theme.palette.divider, 0.88)
        }`,
        backgroundColor: highlighted
          ? alpha(theme.palette.warning.main, 0.08)
          : alpha(theme.palette.background.paper, 0.58),
        boxShadow: highlighted
          ? `inset 0 0 0 1px ${alpha(theme.palette.warning.main, 0.42)}`
          : 'none',
        overflow: 'hidden',
        '&::before': {
          display: 'none',
        },
      })}
    >
      {children}
    </Accordion>
  )
}

export function CollapsibleSection({
  title,
  description,
  countLabel,
  children,
  defaultExpanded = false,
  nested = false,
}: {
  title: string
  description: string
  countLabel?: string
  children: ReactNode
  defaultExpanded?: boolean
  nested?: boolean
}) {
  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      sx={(theme) => ({
        backgroundColor: 'transparent',
        '&::before': {
          display: 'none',
        },
        ...(nested
          ? {
              border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
              borderRadius: 2,
              overflow: 'hidden',
            }
          : {}),
      })}
    >
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 2,
          py: nested ? 0.15 : 0.35,
          '& .MuiAccordionSummary-content': {
            my: 1,
          },
        }}
      >
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
          >
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="overline" color="text.secondary">
                {title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {description}
              </Typography>
            </Box>
            {countLabel ? <MiniMetricChip label={countLabel} /> : null}
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 2, pt: 0, pb: 2 }}>{children}</AccordionDetails>
    </Accordion>
  )
}

export function ExpandGlyph() {
  return (
    <Typography variant="body2" fontWeight={800} color="text.secondary">
      ▾
    </Typography>
  )
}

export function MetricChip({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: 999,
        border: `1px solid ${alpha(theme.palette.divider, 0.88)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.54),
        minWidth: 0,
        px: 1,
        py: 0.7,
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography
        variant="body2"
        sx={{
          fontWeight: 700,
          display: '-webkit-box',
          overflow: 'hidden',
          WebkitLineClamp: 2,
          WebkitBoxOrient: 'vertical',
        }}
      >
        {value}
      </Typography>
    </Box>
  )
}
