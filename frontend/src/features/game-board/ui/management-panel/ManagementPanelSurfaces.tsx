import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { SectionCard } from '../../../../shared/ui/index.ts'

export function ManagementControlSurface({
  accent,
  children,
}: {
  accent: 'info' | 'warning' | 'success'
  children: ReactNode
}) {
  return (
    <SectionCard
      sx={(theme) => ({
        p: 1.15,
        borderRadius: 2,
        border: `1px solid ${alpha(
          accent === 'success'
            ? theme.palette.success.main
            : accent === 'warning'
              ? theme.palette.warning.main
              : theme.palette.info.main,
          0.3,
        )}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.5),
      })}
    >
      {children}
    </SectionCard>
  )
}

export function SecondaryManagementSection({
  sectionId,
  title,
  tooltip,
  children,
  defaultExpanded = false,
}: {
  sectionId: string
  title: string
  tooltip: string
  children: ReactNode
  defaultExpanded?: boolean
}) {
  const headerId = `management-${sectionId}-header`
  const contentId = `management-${sectionId}-content`

  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
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
        expandIcon={
          <Box component="span" aria-hidden sx={{ fontSize: 18, lineHeight: 1 }}>
            ▾
          </Box>
        }
        sx={{
          px: 1.15,
          py: 0,
          minHeight: 46,
          '& .MuiAccordionSummary-content': { my: 0.65 },
        }}
      >
        <ManagementSectionTitle title={title} tooltip={tooltip} />
      </AccordionSummary>
      <AccordionDetails id={contentId} sx={{ px: 1.15, pt: 0, pb: 1.15 }}>
        {children}
      </AccordionDetails>
    </Accordion>
  )
}

export function ManagementSectionTitle({ title, tooltip }: { title: string; tooltip: string }) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center" sx={{ minWidth: 0 }}>
      <Typography variant="subtitle2" fontWeight={850} noWrap>
        {title}
      </Typography>
      <Tooltip title={tooltip} arrow placement="top">
        <Box
          component="span"
          role="img"
          tabIndex={0}
          aria-label={tooltip}
          sx={(theme) => ({
            width: 18,
            height: 18,
            borderRadius: '50%',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
            color: 'text.secondary',
            fontSize: '0.7rem',
            cursor: 'help',
            flexShrink: 0,
            '&:focus-visible': {
              outline: '2px solid',
              outlineColor: theme.palette.primary.main,
              outlineOffset: 2,
            },
          })}
        >
          ?
        </Box>
      </Tooltip>
    </Stack>
  )
}

export function ManagementStateNotice({
  children,
  tone = 'warning',
}: {
  children: ReactNode
  tone?: 'warning' | 'error' | 'info' | 'success'
}) {
  return (
    <Alert severity={tone} variant="outlined" sx={{ borderRadius: 1.5, m: 0 }}>
      {children}
    </Alert>
  )
}
