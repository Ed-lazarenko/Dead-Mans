import { Box, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameModifierAvailability } from '../../../shared/api/contracts/index.ts'

export function ModifierSectionHeading({ title, count }: { title: string; count?: number }) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={0.75}
      justifyContent="space-between"
      alignItems={{ xs: 'flex-start', sm: 'center' }}
    >
      <Typography variant="subtitle1">{title}</Typography>
      {count === undefined ? null : <ModifierCountBadge count={count} />}
    </Stack>
  )
}

export function ModifierCountBadge({ count }: { count: number }) {
  const { t } = useTranslation()

  return (
    <Box
      component="span"
      sx={(theme) => ({
        display: 'inline-flex',
        alignItems: 'center',
        minHeight: 28,
        borderRadius: '999px',
        border: `1px solid ${alpha(theme.palette.primary.main, 0.56)}`,
        backgroundColor: alpha(theme.palette.primary.main, 0.12),
        color: 'text.primary',
        px: 1,
        typography: 'caption',
        fontWeight: 850,
        whiteSpace: 'nowrap',
      })}
    >
      {t('gameModifiers.categoryCountLabel', { count })}
    </Box>
  )
}

export function ModifierIcon({ emoji }: { emoji: string | null | undefined }) {
  return (
    <Box
      aria-hidden="true"
      sx={(theme) => ({
        width: 32,
        height: 32,
        borderRadius: '8px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: alpha(theme.palette.background.paper, 0.52),
        border: `1px solid ${alpha(theme.palette.divider, 0.68)}`,
        flexShrink: 0,
      })}
    >
      {emoji ? <Typography sx={{ fontSize: '1rem', lineHeight: 1 }}>{emoji}</Typography> : null}
    </Box>
  )
}

export function InlineMetaPill({
  label,
  tone = 'default',
}: {
  label: string
  tone?: 'default' | 'success' | 'warning' | 'error'
}) {
  return (
    <Box
      component="span"
      sx={(theme) => {
        const accent =
          tone === 'success'
            ? theme.palette.success.main
            : tone === 'warning'
              ? theme.palette.warning.main
              : tone === 'error'
                ? theme.palette.error.main
                : theme.palette.divider

        return {
          display: 'inline-flex',
          alignItems: 'center',
          minWidth: 0,
          minHeight: 24,
          borderRadius: '999px',
          border: `1px solid ${alpha(accent, tone === 'default' ? 0.74 : 0.52)}`,
          backgroundColor:
            tone === 'default' ? alpha(theme.palette.background.paper, 0.42) : alpha(accent, 0.12),
          px: 0.75,
          typography: 'caption',
          color: tone === 'default' ? 'text.primary' : `${tone}.light`,
          fontWeight: 700,
          lineHeight: 1,
        }
      }}
    >
      {label}
    </Box>
  )
}

export function ModifierCategorySection({
  category,
  children,
}: {
  category: GameModifierAvailability['modifier']['category']
  children: ReactNode
}) {
  return (
    <Box
      component="section"
      aria-label={category}
      sx={(theme) => {
        const accent =
          category === 'preparation'
            ? theme.palette.info.main
            : category === 'round'
              ? theme.palette.success.main
              : theme.palette.warning.main

        return {
          border: `1px solid ${alpha(accent, 0.42)}`,
          borderLeftWidth: 3,
          borderRadius: '10px',
          backgroundColor: alpha(accent, 0.045),
          px: { xs: 0.9, sm: 1.1 },
          py: 0.9,
        }
      }}
    >
      {children}
    </Box>
  )
}
