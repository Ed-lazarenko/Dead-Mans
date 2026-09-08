import { Box, Stack, Typography } from '@mui/material'
import { alpha, type Theme } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameBoardSnapshot } from '../../../../shared/api/contracts/index.ts'
import { buildGameManagementFlow } from '../../model/game-management-flow.ts'
import type { GameRoundDetails } from '../../model/game-management-panel.ts'

type FlowStepState = ReturnType<typeof buildGameManagementFlow>['steps'][number]['state']

export function ManagementFlowPanel({
  snapshot,
  activeRound,
}: {
  snapshot: GameBoardSnapshot
  activeRound: GameRoundDetails | null
}) {
  const { t } = useTranslation()
  const flow = buildGameManagementFlow(snapshot, activeRound)

  return (
    <Stack spacing={0.65}>
      {flow.steps.map((step, index) => (
        <Box
          key={step.id}
          sx={(theme) => {
            const palette = getFlowStepPalette(theme, step.state)

            return {
              border: `1px solid ${palette.border}`,
              backgroundColor: palette.background,
              borderRadius: 1.4,
              px: 0.85,
              py: 0.75,
            }
          }}
        >
          <Stack direction="row" spacing={0.8} alignItems="flex-start">
            <Box
              sx={(theme) => {
                const palette = getFlowStepPalette(theme, step.state)

                return {
                  width: 22,
                  height: 22,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                  color: palette.accent,
                  border: `1px solid ${palette.border}`,
                  backgroundColor: alpha(theme.palette.common.black, 0.12),
                  fontSize: '0.75rem',
                  fontWeight: 850,
                }
              }}
            >
              {index + 1}
            </Box>

            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack
                direction="row"
                spacing={0.65}
                alignItems="center"
                justifyContent="space-between"
                flexWrap="wrap"
                useFlexGap
              >
                <Typography variant="body2" fontWeight={780}>
                  {t(step.titleKey)}
                </Typography>
                <FlowStateBadge state={step.state} />
              </Stack>

              <Typography variant="caption" color="text.secondary">
                {t(step.descriptionKey)}
              </Typography>
            </Box>
          </Stack>
        </Box>
      ))}
    </Stack>
  )
}

function FlowStateBadge({ state }: { state: FlowStepState }) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => {
        const palette = getFlowStepPalette(theme, state)

        return {
          border: `1px solid ${palette.border}`,
          backgroundColor: palette.badgeBackground,
          color: palette.accent,
          borderRadius: 999,
          px: 0.85,
          py: 0.2,
          fontSize: '0.7rem',
          fontWeight: 700,
          letterSpacing: '0.03em',
          lineHeight: 1.2,
        }
      }}
    >
      {t(`gameBoard.flowStepState.${state}`)}
    </Box>
  )
}

function getFlowStepPalette(theme: Theme, state: FlowStepState) {
  const accent =
    state === 'complete'
      ? theme.palette.success.main
      : state === 'current'
        ? theme.palette.info.main
        : state === 'ready'
          ? theme.palette.warning.main
          : state === 'blocked'
            ? theme.palette.grey[600]
            : theme.palette.divider

  return {
    accent,
    border: alpha(accent, state === 'blocked' ? 0.4 : 0.6),
    background: state === 'blocked' ? alpha(theme.palette.common.black, 0.14) : alpha(accent, 0.1),
    badgeBackground:
      state === 'blocked' ? alpha(theme.palette.common.black, 0.22) : alpha(accent, 0.16),
  }
}
