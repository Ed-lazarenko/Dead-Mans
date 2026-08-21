import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { formatPlayedCardModifierOutcomeStatus } from '../../../shared/lib/played-card-formatters.ts'
import { SectionCard } from '../../../shared/ui/index.ts'
import { buildGameHistoryModifierSummary } from '../model/game-history-modifier-summary.ts'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function GameModifierHistorySummary({ rounds }: { rounds: readonly GameHistoryRound[] }) {
  const { t } = useTranslation()
  const items = buildGameHistoryModifierSummary(rounds)
  if (items.length === 0) return null

  return (
    <SectionCard inset sx={{ p: 1.5 }}>
      <Stack spacing={1.1}>
        <Box>
          <Typography variant="subtitle1">{t('gameHistory.modifierSummary.title')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.modifierSummary.description')}
          </Typography>
        </Box>
        <Box
          sx={{
            display: 'grid',
            gap: 1,
            gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, minmax(0, 1fr))' },
          }}
        >
          {items.map((item) => (
            <Box
              key={item.key}
              sx={(theme) => ({
                borderRadius: 1.75,
                border: `1px solid ${alpha(theme.palette.divider, 0.76)}`,
                p: 1,
              })}
            >
              <Stack spacing={0.65}>
                <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                  <Typography variant="subtitle2" sx={{ flex: 1 }}>
                    {item.modifierName}
                  </Typography>
                  {item.definitionRevision ? (
                    <Chip
                      size="small"
                      variant="outlined"
                      label={t('gameHistory.modifierRevision', {
                        revision: item.definitionRevision,
                      })}
                    />
                  ) : null}
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  {item.modifierDescription}
                </Typography>
                <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                  <Chip
                    size="small"
                    label={t('gameHistory.modifierSummary.activations', {
                      count: item.activationCount,
                    })}
                  />
                  <Chip
                    size="small"
                    variant="outlined"
                    label={t('gameHistory.modifierSummary.rounds', { count: item.roundCount })}
                  />
                  <Chip
                    size="small"
                    variant="outlined"
                    label={t('gameHistory.modifierSummary.points', { points: item.pointsDelta })}
                  />
                  <Chip
                    size="small"
                    variant="outlined"
                    label={t('gameHistory.modifierSummary.bonusKills', {
                      kills: item.bonusKillsDelta,
                    })}
                  />
                  {item.outcomes.map((outcome) => (
                    <Chip
                      key={outcome.status}
                      size="small"
                      variant="outlined"
                      label={t('gameHistory.modifierSummary.outcome', {
                        outcome: formatPlayedCardModifierOutcomeStatus(t, outcome.status),
                        count: outcome.count,
                      })}
                    />
                  ))}
                </Stack>
              </Stack>
            </Box>
          ))}
        </Box>
      </Stack>
    </SectionCard>
  )
}
