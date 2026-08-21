import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { formatHistoryTeamName } from '../model/game-history-formatters.ts'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function CancelledRoundsSection({
  rounds,
  onPreviewCard,
}: {
  rounds: readonly GameHistoryRound[]
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  if (rounds.length === 0) return null

  return (
    <SectionCard inset sx={{ p: 1.5 }}>
      <Stack spacing={1.1}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={0.75}
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          justifyContent="space-between"
        >
          <Box>
            <Typography variant="subtitle1">{t('gameHistory.cancelledRounds.title')}</Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.cancelledRounds.description')}
            </Typography>
          </Box>
          <Chip
            size="small"
            color="error"
            variant="outlined"
            label={t('gameHistory.cancelledRounds.count', { count: rounds.length })}
          />
        </Stack>

        {rounds.map((round) => (
          <Box
            key={round.roundId}
            sx={(theme) => ({
              borderRadius: 1.75,
              border: `1px solid ${alpha(theme.palette.error.main, 0.42)}`,
              backgroundColor: alpha(theme.palette.error.main, 0.055),
              p: 1,
            })}
          >
            <Stack spacing={0.75}>
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={0.75}
                alignItems={{ xs: 'flex-start', sm: 'center' }}
              >
                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Typography variant="subtitle2">
                    {round.cellTitle?.trim() || t('gameHistory.cardDialogFallbackTitle')}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {formatHistoryTeamName(t, round.teamName, round.teamSlotIndex)}
                  </Typography>
                </Box>
                <AppButton size="small" tone="secondary" onClick={() => onPreviewCard(round)}>
                  {t('common.actions.openCard')}
                </AppButton>
              </Stack>
              <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.cancelledRounds.stage', {
                    stage: t(
                      `gameHistory.cancelledRounds.stageValue.${round.technicalCancellationStage ?? 'unknown'}`,
                    ),
                  })}
                />
                <Chip
                  size="small"
                  variant="outlined"
                  label={t(
                    `gameHistory.cancelledRounds.reason.${round.technicalCancellationReasonCode ?? 'unknown'}`,
                  )}
                />
                {round.purchasesRefunded ? (
                  <Chip
                    size="small"
                    color="success"
                    variant="outlined"
                    label={t('gameHistory.cancelledRounds.refunded')}
                  />
                ) : null}
              </Stack>
              {round.publicCancellationSummary ? (
                <Typography variant="body2" color="text.secondary">
                  {round.publicCancellationSummary}
                </Typography>
              ) : null}
            </Stack>
          </Box>
        ))}
      </Stack>
    </SectionCard>
  )
}
