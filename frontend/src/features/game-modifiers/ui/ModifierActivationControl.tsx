import { Box, Stack, Tooltip, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameModifierAvailability } from '../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../shared/ui/index.ts'

interface ModifierActivationControlProps {
  availability: GameModifierAvailability
  isBusy: boolean
  isPending: boolean
  blockedReasonLabel: string
  blockedReasonTooltip: string
  onActivate: (modifierId: string) => void
}

export function ModifierActivationControl({
  availability,
  isBusy,
  isPending,
  blockedReasonLabel,
  blockedReasonTooltip,
  onActivate,
}: ModifierActivationControlProps) {
  const { t } = useTranslation()

  return (
    <Box
      sx={{
        width: { xs: '100%', sm: 192 },
        flexShrink: 0,
        display: 'flex',
        justifyContent: { xs: 'stretch', sm: 'flex-end' },
      }}
    >
      {availability.canActivate ? (
        <AppButton
          tone="primary"
          size="small"
          fullWidth
          disabled={isBusy}
          onClick={() => onActivate(availability.modifier.id)}
          sx={{
            height: 32,
            minHeight: 32,
            borderRadius: '8px',
            fontSize: '0.75rem',
            lineHeight: 1.15,
          }}
        >
          {isPending ? t('gameModifiers.activatePending') : t('gameModifiers.activateAction')}
        </AppButton>
      ) : availability.blockedReason === 'ordering_closed' ? (
        <Tooltip
          title={blockedReasonTooltip}
          arrow
          describeChild
          enterDelay={150}
          enterTouchDelay={0}
        >
          <Box
            component="span"
            tabIndex={0}
            sx={{ display: 'block', width: '100%', cursor: 'help' }}
          >
            <AppButton
              tone="primary"
              size="small"
              fullWidth
              disabled
              sx={{
                height: 32,
                minHeight: 32,
                borderRadius: '8px',
                pointerEvents: 'none',
                fontSize: '0.75rem',
                lineHeight: 1.15,
              }}
            >
              <Stack
                component="span"
                alignItems="center"
                justifyContent="center"
                sx={{ position: 'relative', width: '100%', minHeight: 14 }}
              >
                <Box component="span" sx={{ width: '100%', textAlign: 'center' }}>
                  {blockedReasonLabel}
                </Box>
                <Box
                  component="span"
                  aria-hidden="true"
                  sx={{
                    position: 'absolute',
                    top: '50%',
                    left: 0,
                    transform: 'translateY(-50%)',
                    display: 'inline-flex',
                    width: 14,
                    height: 14,
                    alignItems: 'center',
                    justifyContent: 'center',
                    border: '1px solid currentColor',
                    borderRadius: '50%',
                    fontSize: '0.62rem',
                    fontWeight: 900,
                    lineHeight: 1,
                  }}
                >
                  ?
                </Box>
              </Stack>
            </AppButton>
          </Box>
        </Tooltip>
      ) : (
        <BlockedReasonPlaque
          blockedReason={availability.blockedReason}
          label={blockedReasonLabel}
          tooltip={blockedReasonTooltip}
        />
      )}
    </Box>
  )
}

function BlockedReasonPlaque({
  blockedReason,
  label,
  tooltip,
}: {
  blockedReason: GameModifierAvailability['blockedReason']
  label: string
  tooltip: string
}) {
  return (
    <Tooltip title={tooltip} arrow describeChild enterDelay={150} enterTouchDelay={0}>
      <Box
        role="status"
        aria-label={tooltip}
        tabIndex={0}
        sx={(theme) => {
          const accent =
            blockedReason === 'limit_reached' || blockedReason === 'active_team_member'
              ? theme.palette.error.main
              : blockedReason === 'insufficient_points'
                ? theme.palette.warning.main
                : theme.palette.info.main

          return {
            '--blocked-reason-accent': accent,
            width: '100%',
            height: 32,
            minHeight: 32,
            px: 0.7,
            borderRadius: '8px',
            border: `1px solid ${alpha(accent, 0.46)}`,
            backgroundColor: alpha(accent, 0.08),
            cursor: 'help',
            position: 'relative',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            '& .blocked-reason-help': {
              color: 'var(--blocked-reason-accent)',
              borderColor: alpha(accent, 0.72),
            },
          }
        }}
      >
        <Typography
          variant="caption"
          sx={{
            display: 'block',
            width: '100%',
            px: 2,
            textAlign: 'center',
            fontSize: '0.7rem',
            fontWeight: 700,
            lineHeight: 1.15,
          }}
        >
          {label}
        </Typography>
        <Box
          component="span"
          className="blocked-reason-help"
          aria-hidden="true"
          sx={{
            position: 'absolute',
            top: '50%',
            left: 6,
            transform: 'translateY(-50%)',
            display: 'inline-flex',
            width: 14,
            height: 14,
            alignItems: 'center',
            justifyContent: 'center',
            border: '1px solid',
            borderRadius: '50%',
            fontSize: '0.62rem',
            fontWeight: 900,
            lineHeight: 1,
          }}
        >
          ?
        </Box>
      </Box>
    </Tooltip>
  )
}
