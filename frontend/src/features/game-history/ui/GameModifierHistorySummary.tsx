import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink } from 'react-router-dom'
import { modifierHistoryRoute } from '../../../routes/app-routes.ts'
import type { components } from '../../../shared/api/contracts/generated'
import { formatPlayedCardModifierOutcomeStatus } from '../../../shared/lib/played-card-formatters.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { buildGameHistoryModifierSummary } from '../model/game-history-modifier-summary.ts'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']
type ModifierSnapshot = components['schemas']['GameHistoryModifierSnapshotDto']

export function GameModifierHistorySummary({
  rounds,
  snapshots = [],
  snapshotStatus = 'complete',
}: {
  rounds: readonly GameHistoryRound[]
  snapshots?: readonly ModifierSnapshot[]
  snapshotStatus?: 'complete' | 'legacy_unavailable'
}) {
  const { t } = useTranslation()
  const items = buildGameHistoryModifierSummary(rounds)
  if (items.length === 0 && snapshots.length === 0 && snapshotStatus === 'complete') return null

  return (
    <SectionCard inset sx={{ p: 1.5 }}>
      <Stack spacing={1.1}>
        <Box>
          <Typography variant="subtitle1">{t('gameHistory.modifierSummary.title')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.modifierSummary.description')}
          </Typography>
        </Box>
        {snapshotStatus === 'legacy_unavailable' ? (
          <Typography variant="body2" color="warning.main">
            {t('modifierHistory.legacy')}
          </Typography>
        ) : null}
        {snapshots.length > 0 ? (
          <Box
            sx={{
              display: 'grid',
              gap: 1,
              gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, minmax(0, 1fr))' },
            }}
          >
            {snapshots.map((snapshot) => (
              <Box
                key={snapshot.versionId}
                sx={{ border: 1, borderColor: 'divider', borderRadius: 1.75, p: 1 }}
              >
                <AppButton
                  component={RouterLink}
                  to={`${modifierHistoryRoute.fullPath}?modifierId=${snapshot.modifierId}&revision=${snapshot.revision}`}
                  tone="ghost"
                  size="small"
                >
                  {snapshot.iconEmoji ? `${snapshot.iconEmoji} ` : ''}
                  {t('gameHistory.modifierSnapshotLabel', {
                    name: snapshot.name,
                    revision: snapshot.revision,
                  })}
                </AppButton>
                <Stack direction="row" gap={0.5} flexWrap="wrap" sx={{ mt: 0.75 }}>
                  <Chip
                    size="small"
                    color={snapshot.successfulActivationsCount > 0 ? 'success' : 'default'}
                    label={t(
                      snapshot.successfulActivationsCount > 0
                        ? 'modifierHistory.activated'
                        : 'modifierHistory.notActivated',
                    )}
                  />
                  {snapshot.cancelledActivationsCount > 0 ? (
                    <Chip
                      size="small"
                      color="warning"
                      label={t('modifierHistory.cancelled', {
                        count: snapshot.cancelledActivationsCount,
                      })}
                    />
                  ) : null}
                  {snapshot.resultsCount > 0 ? (
                    <Chip
                      size="small"
                      color="info"
                      label={t('modifierHistory.results', { count: snapshot.resultsCount })}
                    />
                  ) : null}
                  {snapshot.isEmergencyDisabled ? (
                    <Chip size="small" color="error" label={t('modifierHistory.emergency')} />
                  ) : null}
                </Stack>
              </Box>
            ))}
          </Box>
        ) : null}
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
