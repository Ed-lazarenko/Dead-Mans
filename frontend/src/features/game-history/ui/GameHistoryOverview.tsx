import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { CompactMetric, MiniMetricChip } from './game-history-display.tsx'
import {
  formatGameTimeLabel,
  getGameStatusColor,
  normalizeStatus,
} from '../model/game-history-view.ts'

type GameHistoryGameSummary = components['schemas']['GameHistoryGameSummaryDto']

export function BoardSwitchCard({
  title,
  description,
  isActive,
  onClick,
}: {
  title: string
  description: string
  isActive: boolean
  onClick: () => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      component="button"
      type="button"
      onClick={onClick}
      sx={(theme) => ({
        flex: 1,
        minWidth: 0,
        textAlign: 'left',
        borderRadius: 2.5,
        border: `1px solid ${
          isActive ? theme.palette.primary.main : alpha(theme.palette.divider, 0.9)
        }`,
        background: isActive
          ? `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.16)} 0%, ${alpha(theme.palette.info.main, 0.14)} 100%)`
          : alpha(theme.palette.background.paper, 0.68),
        px: 1.6,
        py: 1.5,
        cursor: 'pointer',
        transition: 'border-color 0.15s ease, transform 0.15s ease, background-color 0.15s ease',
        '&:hover': {
          borderColor: theme.palette.primary.light,
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
          transform: 'translateY(-1px)',
        },
      })}
    >
      <Stack spacing={0.7}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
            {title}
          </Typography>
          {isActive ? <MiniMetricChip label={t('gameHistory.boardActiveChip')} /> : null}
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>
      </Stack>
    </Box>
  )
}

export function CurrentGameLeaderboardSummary({
  title,
  status,
  playedTeamCount,
  playedRoundCount,
  activatedModifierCount,
  quizPoints,
  totalKills,
  totalTokens,
  penaltyTotal,
  teamFinalScoreTotal,
}: {
  title: string
  status: string
  playedTeamCount: number | null
  playedRoundCount: number | null
  activatedModifierCount: number | null
  quizPoints: number | null
  totalKills: number | null
  totalTokens: number | null
  penaltyTotal: number | null
  teamFinalScoreTotal: number | null
}) {
  const { t } = useTranslation()
  const formatCount = (value: number | null) =>
    value === null ? '-' : t('gameHistory.countValue', { count: value })
  const formatPoints = (value: number | null) =>
    value === null ? '-' : t('gameHistory.pointsValue', { points: value })

  return (
    <Stack spacing={1}>
      <Box
        sx={(theme) => ({
          borderRadius: 2,
          border: `1px solid ${alpha(theme.palette.primary.main, 0.24)}`,
          background: `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.14)} 0%, ${alpha(
            theme.palette.background.paper,
            0.66,
          )} 100%)`,
          px: { xs: 1.4, sm: 1.75 },
          py: { xs: 1.2, sm: 1.45 },
          textAlign: 'center',
        })}
      >
        <Stack spacing={0.85} alignItems="center" sx={{ minWidth: 0 }}>
          <Stack
            direction="row"
            spacing={0.75}
            alignItems="center"
            justifyContent="center"
            flexWrap="wrap"
            useFlexGap
          >
            <Chip label={t('gameHistory.currentGameSummaryTitle')} color="warning" size="small" />
            <Chip
              label={t(`gameHistory.status.${normalizeStatus(status)}`)}
              color={getGameStatusColor(status)}
              variant="outlined"
              size="small"
            />
          </Stack>

          <Typography
            component="p"
            variant="h5"
            sx={{ maxWidth: 920, fontWeight: 900, lineHeight: 1.16 }}
          >
            {title}
          </Typography>
        </Stack>
      </Box>

      <Box
        sx={(theme) => ({
          borderRadius: 2,
          border: `1px solid ${alpha(theme.palette.warning.main, 0.24)}`,
          backgroundColor: alpha(theme.palette.background.paper, 0.58),
          px: { xs: 1.1, sm: 1.25 },
          py: { xs: 1.05, sm: 1.15 },
        })}
      >
        <Box
          sx={{
            display: 'grid',
            gap: 0.8,
            gridTemplateColumns: {
              xs: 'repeat(2, minmax(0, 1fr))',
              sm: 'repeat(4, minmax(0, 1fr))',
            },
          }}
        >
          <CompactMetric
            label={t('gameHistory.summary.playedTeamCount')}
            value={formatCount(playedTeamCount)}
          />
          <CompactMetric
            label={t('gameHistory.summary.playedRoundCount')}
            value={formatCount(playedRoundCount)}
          />
          <CompactMetric
            label={t('gameHistory.summary.activatedModifierCount')}
            value={formatCount(activatedModifierCount)}
          />
          <CompactMetric
            label={t('gameHistory.summary.quizPointsEarned')}
            value={formatPoints(quizPoints)}
          />
          <CompactMetric
            label={t('gameHistory.summary.totalKills')}
            value={formatCount(totalKills)}
          />
          <CompactMetric
            label={t('gameHistory.summary.totalBounties')}
            value={formatCount(totalTokens)}
          />
          <CompactMetric
            label={t('gameHistory.summary.penaltyTotal')}
            value={formatPoints(penaltyTotal)}
          />
          <CompactMetric
            label={t('gameHistory.summary.teamFinalScoreTotal')}
            value={formatPoints(teamFinalScoreTotal)}
          />
        </Box>
      </Box>
    </Stack>
  )
}

export function GameSummaryButton({
  game,
  isSelected,
  onClick,
}: {
  game: GameHistoryGameSummary
  isSelected: boolean
  onClick: () => void
}) {
  const { t, i18n } = useTranslation()

  return (
    <Box
      component="button"
      type="button"
      onClick={onClick}
      sx={(theme) => ({
        width: '100%',
        textAlign: 'left',
        border: `1px solid ${
          isSelected ? theme.palette.primary.main : alpha(theme.palette.divider, 0.9)
        }`,
        backgroundColor: isSelected
          ? alpha(theme.palette.primary.main, 0.1)
          : alpha(theme.palette.background.paper, 0.72),
        borderRadius: 2,
        px: 1.5,
        py: 1.4,
        cursor: 'pointer',
        transition: 'border-color 0.15s ease, background-color 0.15s ease, transform 0.15s ease',
        '&:hover': {
          borderColor: theme.palette.primary.light,
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
          transform: 'translateY(-1px)',
        },
      })}
    >
      <Stack spacing={0.9}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="body2" sx={{ fontWeight: 700, minWidth: 0, flex: 1 }}>
            {game.gameTitle}
          </Typography>
          <Chip
            size="small"
            label={t(`gameHistory.status.${normalizeStatus(game.gameStatus)}`)}
            color={getGameStatusColor(game.gameStatus)}
          />
        </Stack>

        <Typography variant="caption" color="text.secondary">
          {formatGameTimeLabel(game, t, i18n.resolvedLanguage)}
        </Typography>

        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          <MiniMetricChip
            label={t('gameHistory.summary.roundCountShort', {
              count: game.mainGameRoundCount,
            })}
          />
          <MiniMetricChip
            label={t('gameHistory.summary.quizCountShort', {
              count: game.quizRoundCount,
            })}
          />
          <MiniMetricChip
            label={t('gameHistory.summary.playerCountShort', {
              count: game.uniquePlayerCount,
            })}
          />
        </Stack>
      </Stack>
    </Box>
  )
}
