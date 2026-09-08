import { Box, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { SectionCard } from '../../../shared/ui/index.ts'
import { formatHistoryTeamName } from '../model/game-history-formatters.ts'
import { formatDateTime } from '../model/game-history-view.ts'
import { MiniMetricChip } from './game-history-display.tsx'

export function FinalResultSnapshot({
  summary,
}: {
  summary: components['schemas']['GameFinishSummaryDto']
}) {
  const { t, i18n } = useTranslation()

  return (
    <SectionCard inset>
      <Stack spacing={1.5}>
        <Stack spacing={0.35}>
          <Typography variant="subtitle1" fontWeight={850}>
            {t('gameHistory.finalResultTitle')}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.finalResultMeta', {
              admin: summary.finishedByDisplayName ?? t('gameHistory.unknownValue'),
              date: summary.finishedAtUtc
                ? formatDateTime(summary.finishedAtUtc, i18n.resolvedLanguage)
                : t('gameHistory.unknownValue'),
            })}
          </Typography>
        </Stack>

        {summary.publicNote ? (
          <Box
            sx={(theme) => ({
              borderRadius: 2,
              border: `1px solid ${alpha(theme.palette.info.main, 0.3)}`,
              backgroundColor: alpha(theme.palette.info.main, 0.08),
              px: 1.5,
              py: 1.25,
            })}
          >
            <Typography variant="caption" color="text.secondary">
              {t('gameHistory.finalResultNote')}
            </Typography>
            <Typography variant="body2" sx={{ mt: 0.4, whiteSpace: 'pre-wrap' }}>
              {summary.publicNote}
            </Typography>
          </Box>
        ) : null}

        <Stack spacing={0.8}>
          {summary.teams.map((team) => (
            <Box
              key={team.teamId}
              sx={(theme) => ({
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.divider, 0.85)}`,
                px: 1.25,
                py: 1.05,
              })}
            >
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={0.8}
                justifyContent="space-between"
              >
                <Box sx={{ minWidth: 0 }}>
                  <Typography fontWeight={800}>
                    {team.placement ? `${team.placement}. ` : ''}
                    {formatHistoryTeamName(t, team.teamName, team.teamSlotIndex)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {team.participantNames.length > 0
                      ? team.participantNames.join(', ')
                      : t('gameHistory.noParticipants')}
                  </Typography>
                </Box>
                <Stack direction="row" spacing={0.6} flexWrap="wrap" useFlexGap>
                  <MiniMetricChip
                    label={
                      team.finalScore == null
                        ? t('gameHistory.finalResultDidNotPlay')
                        : t('gameHistory.summary.finalScoreShort', { points: team.finalScore })
                    }
                  />
                  {team.bestScore != null ? (
                    <MiniMetricChip
                      label={t('gameHistory.summary.bestScoreShort', { points: team.bestScore })}
                    />
                  ) : null}
                  <MiniMetricChip
                    label={t('gameHistory.summary.penaltyTotalShort', {
                      points: team.penaltyTotal,
                    })}
                  />
                </Stack>
              </Stack>
            </Box>
          ))}
        </Stack>
      </Stack>
    </SectionCard>
  )
}
