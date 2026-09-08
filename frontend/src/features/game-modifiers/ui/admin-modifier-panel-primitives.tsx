import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import type { ReactNode } from 'react'

export function AdminModifierBlock({
  sectionId,
  step,
  title,
  tooltip,
  children,
}: {
  sectionId: string
  step: string
  title: string
  tooltip: string
  children: ReactNode
}) {
  const headerId = `modifier-management-${sectionId}-header`
  const contentId = `modifier-management-${sectionId}-content`

  return (
    <Accordion
      defaultExpanded
      disableGutters
      elevation={0}
      aria-labelledby={headerId}
      sx={(theme) => ({
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        overflow: 'hidden',
        '&::before': { display: 'none' },
      })}
    >
      <AccordionSummary
        id={headerId}
        aria-controls={contentId}
        aria-description={tooltip}
        expandIcon={
          <Box component="span" aria-hidden sx={{ fontSize: 18, lineHeight: 1 }}>
            ⌄
          </Box>
        }
        sx={{
          px: { xs: 1.25, sm: 1.5 },
          minHeight: 52,
          '& .MuiAccordionSummary-content': { my: 0.75 },
        }}
      >
        <Stack spacing={0.2}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontWeight: 750, letterSpacing: '0.025em' }}
          >
            {step}
          </Typography>
          <Stack direction="row" spacing={0.5} alignItems="center">
            <Typography component="span" variant="subtitle1" sx={{ fontWeight: 850 }}>
              {title}
            </Typography>
            <Box
              component="span"
              aria-hidden
              sx={(theme) => ({
                width: 18,
                height: 18,
                borderRadius: '999px',
                display: 'inline-flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
                color: 'text.secondary',
                fontSize: '0.7rem',
              })}
            >
              ?
            </Box>
          </Stack>
        </Stack>
      </AccordionSummary>
      <AccordionDetails id={contentId} sx={{ px: { xs: 1.25, sm: 1.5 }, pt: 0, pb: 1.5 }}>
        {children}
      </AccordionDetails>
    </Accordion>
  )
}

export function AdminModifierHint({ title }: { title: string }) {
  return (
    <Tooltip title={title} arrow placement="top">
      <IconButton
        size="small"
        aria-label={title}
        sx={(theme) => ({
          width: 44,
          height: 44,
          color: 'text.secondary',
          '&:hover': {
            color: 'primary.main',
            backgroundColor: alpha(theme.palette.primary.main, 0.08),
          },
        })}
      >
        <Box
          component="span"
          aria-hidden="true"
          sx={(theme) => ({
            width: 18,
            height: 18,
            borderRadius: '999px',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
            fontSize: '0.7rem',
            cursor: 'help',
          })}
        >
          ?
        </Box>
      </IconButton>
    </Tooltip>
  )
}

export function AdminModifierMetric({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.48)}`,
        borderRadius: 1.5,
        px: 1,
        py: 0.9,
        minWidth: 0,
      })}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="subtitle2" fontWeight={700}>
        {value}
      </Typography>
    </Box>
  )
}

export function AdminModifierStateNotice({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.warning.main, 0.45)}`,
        backgroundColor: alpha(theme.palette.warning.main, 0.12),
        borderRadius: 1.5,
        px: 1,
        py: 0.85,
      })}
    >
      <Typography variant="body2">{children}</Typography>
    </Box>
  )
}
