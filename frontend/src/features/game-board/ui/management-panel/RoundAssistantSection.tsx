import { Box, Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { AppButton } from '../../../../shared/ui/index.ts'
import type { RoundActionModel } from '../../model/game-management-panel.ts'
import { ManagementControlSurface, ManagementSectionTitle } from './ManagementPanelSurfaces.tsx'

export function RoundAssistantSection({
  roundAction,
  isChangingRoundStage,
}: {
  roundAction: RoundActionModel
  isChangingRoundStage: boolean
}) {
  const { t } = useTranslation()

  return (
    <ManagementControlSurface accent={roundAction.statusTone}>
      <Stack spacing={1.05}>
        <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
          <ManagementSectionTitle
            title={t('gameBoard.managementRoundAssistantTitle')}
            tooltip={t('gameBoard.managementRoundAssistantTooltip')}
          />
          <Stack direction="row" spacing={0.55} alignItems="center" flexWrap="wrap" useFlexGap>
            {roundAction.stepNumber ? (
              <Chip
                size="small"
                variant="outlined"
                label={t('gameBoard.managementRoundStepProgress', {
                  current: roundAction.stepNumber,
                  total: 6,
                })}
              />
            ) : null}
            <Chip
              size="small"
              color={roundAction.statusTone}
              variant="filled"
              label={roundAction.statusLabel}
            />
          </Stack>
        </Stack>

        <Box>
          <Typography variant="subtitle1" fontWeight={850}>
            {roundAction.title}
          </Typography>
          {roundAction.description &&
          (roundAction.stepId !== 'select_team' || roundAction.actionLabel) ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
              {roundAction.description}
            </Typography>
          ) : null}
        </Box>

        {roundAction.actionLabel && roundAction.onAction ? (
          <AppButton
            tone={roundAction.actionTone}
            size="medium"
            fullWidth
            disabled={isChangingRoundStage}
            onClick={roundAction.onAction}
            sx={{ minHeight: 46, fontWeight: 850 }}
          >
            {roundAction.actionLabel}
          </AppButton>
        ) : null}
      </Stack>
    </ManagementControlSurface>
  )
}
