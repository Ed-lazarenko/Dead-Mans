import { AccordionDetails, AccordionSummary, Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import {
  getRoundScore,
  getTeamBestScore,
  getTeamFinalScore,
  getTeamPenaltyTotal,
  getTeamTotalBounties,
  getTeamTotalKills,
  sortRoundsByPlaySequence,
  type GameHistoryTeamLeaderboardEntry,
} from '../model/game-history-team-leaderboard.ts'
import {
  formatHistoryTeamName,
  formatShortCardLabel,
  getRankColor,
} from '../model/game-history-formatters.ts'
import { MiniMetricChip } from './game-history-display.tsx'
import { LeaderboardRoundCard } from './GameHistoryLeaderboardRound.tsx'
import {
  AccordionSurface,
  CollapsibleSection,
  ExpandGlyph,
  MetricChip,
} from './game-history-surfaces.tsx'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function TeamLeaderboardRow({
  entry,
  rank,
  onPreviewCard,
}: {
  entry: GameHistoryTeamLeaderboardEntry
  rank: number
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const roundsByPlaySequence = sortRoundsByPlaySequence(entry.rounds)
  const recentRounds = roundsByPlaySequence.slice(-3)
  const bestScore = getTeamBestScore(entry)
  const finalScore = getTeamFinalScore(entry)
  const penaltyTotal = getTeamPenaltyTotal(entry)
  const totalKills = getTeamTotalKills(entry)
  const totalBounties = getTeamTotalBounties(entry)

  return (
    <AccordionSurface defaultExpanded={rank <= 3}>
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 1.4,
          py: 0.1,
          '& .MuiAccordionSummary-content': {
            my: 0.8,
          },
        }}
      >
        <Box sx={{ width: '100%' }}>
          <Stack spacing={0.9}>
            <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1} alignItems="flex-start">
              <Stack direction="row" spacing={1} alignItems="center" sx={{ minWidth: 0, flex: 1 }}>
                <Box
                  sx={(theme) => ({
                    minWidth: 34,
                    height: 34,
                    borderRadius: 1.5,
                    display: 'grid',
                    placeItems: 'center',
                    fontWeight: 800,
                    color: theme.palette.common.white,
                    backgroundColor: getRankColor(theme, rank),
                    flexShrink: 0,
                    boxShadow: `0 10px 18px ${alpha(theme.palette.common.black, 0.18)}`,
                  })}
                >
                  {rank}
                </Box>

                <Box sx={{ minWidth: 0, flex: 1 }}>
                  <Stack
                    direction="row"
                    spacing={0.75}
                    alignItems="center"
                    flexWrap="wrap"
                    useFlexGap
                  >
                    <Typography variant="body1" sx={{ fontWeight: 800 }}>
                      {formatHistoryTeamName(t, entry.teamName, entry.teamSlotIndex)}
                    </Typography>
                    {rank <= 3 ? (
                      <Chip
                        size="small"
                        color={rank === 1 ? 'warning' : rank === 2 ? 'default' : 'secondary'}
                        label={t(`gameHistory.rank.${rank}`)}
                      />
                    ) : null}
                    <MiniMetricChip
                      label={t('gameHistory.summary.roundCountShort', {
                        count: entry.roundsPlayed,
                      })}
                    />
                  </Stack>

                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{
                      mt: 0.35,
                      display: '-webkit-box',
                      overflow: 'hidden',
                      WebkitLineClamp: 1,
                      WebkitBoxOrient: 'vertical',
                    }}
                  >
                    {entry.participantNames.length > 0
                      ? entry.participantNames.join(', ')
                      : t('gameHistory.noParticipants')}
                  </Typography>
                </Box>
              </Stack>

              <Stack direction="row" spacing={0.6} flexWrap="wrap" useFlexGap>
                <MiniMetricChip
                  label={t('gameHistory.summary.finalScoreShort', { points: finalScore })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.penaltyTotalShort', {
                    points: penaltyTotal,
                  })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.bestScoreShort', { points: bestScore })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.averageScoreShort', {
                    points: entry.averageScore,
                  })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.killsShort', { count: totalKills })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.bountiesShort', {
                    count: totalBounties,
                  })}
                />
              </Stack>
            </Stack>

            <Stack spacing={0.6}>
              <Typography variant="caption" color="text.secondary">
                {t('gameHistory.summary.recentRounds')}
              </Typography>
              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                {recentRounds.map((round) => (
                  <RecentRoundPill
                    key={round.roundId}
                    round={round}
                    isBestRound={round.roundId === entry.bestRound.roundId}
                  />
                ))}
              </Stack>
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.4, pt: 0, pb: 1.4 }}>
        <Stack spacing={1.25}>
          <Box
            sx={(theme) => ({
              display: 'grid',
              gap: 0.75,
              gridTemplateColumns: {
                xs: 'repeat(2, minmax(0, 1fr))',
                lg: 'repeat(4, minmax(0, 1fr))',
              },
              borderRadius: 2,
              border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
              backgroundColor: alpha(theme.palette.background.paper, 0.42),
              p: 0.9,
            })}
          >
            <MetricChip
              label={t('gameHistory.summary.finalScore')}
              value={t('gameHistory.pointsValue', { points: finalScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.penaltyTotal')}
              value={t('gameHistory.pointsValue', { points: penaltyTotal })}
            />
            <MetricChip
              label={t('gameHistory.summary.bestScore')}
              value={t('gameHistory.pointsValue', { points: bestScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.averageScore')}
              value={t('gameHistory.pointsValue', { points: entry.averageScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.totalKills')}
              value={t('gameHistory.countValue', { count: totalKills })}
            />
            <MetricChip
              label={t('gameHistory.summary.totalBounties')}
              value={t('gameHistory.countValue', { count: totalBounties })}
            />
            <MetricChip
              label={t('gameHistory.summary.bestCard')}
              value={formatShortCardLabel(entry.bestRound, t)}
            />
          </Box>

          <CollapsibleSection
            title={t('gameHistory.summary.allRoundsTitle')}
            description={t('gameHistory.summary.allRoundsDescription')}
            countLabel={t('gameHistory.summary.roundCountShort', {
              count: entry.rounds.length,
            })}
            nested
            defaultExpanded={rank === 1}
          >
            <Stack spacing={1}>
              {roundsByPlaySequence.map((round) => (
                <LeaderboardRoundCard
                  key={round.roundId}
                  round={round}
                  isBestRound={round.roundId === entry.bestRound.roundId}
                  onPreviewCard={onPreviewCard}
                />
              ))}
            </Stack>
          </CollapsibleSection>
        </Stack>
      </AccordionDetails>
    </AccordionSurface>
  )
}

function RecentRoundPill({
  round,
  isBestRound,
}: {
  round: GameHistoryRound
  isBestRound: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 1.5,
        border: `1px solid ${
          isBestRound ? alpha(theme.palette.warning.main, 0.52) : alpha(theme.palette.divider, 0.82)
        }`,
        background: isBestRound
          ? `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.15)}, ${alpha(
              theme.palette.background.paper,
              0.66,
            )})`
          : alpha(theme.palette.background.paper, 0.54),
        px: 0.9,
        py: 0.65,
      })}
    >
      <Stack spacing={0.25}>
        <Stack direction="row" spacing={0.6} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="caption" sx={{ fontWeight: 800 }}>
            {t('gameHistory.pointsValue', { points: getRoundScore(round) })}
          </Typography>
        </Stack>
        <Typography variant="caption" color="text.secondary">
          {round.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('gameHistory.summary.killsShort', {
            count: round.scoreDetails.totalKillCount,
          })}{' '}
          · {t('gameHistory.summary.bountiesShort', { count: round.bountyCount })}
        </Typography>
      </Stack>
    </Box>
  )
}
