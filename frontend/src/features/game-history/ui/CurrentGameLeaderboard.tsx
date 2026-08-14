import { Box, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { AppButton } from '../../../shared/ui/index.ts'
import { formatHistoryTeamName, formatShortCardLabel } from '../model/game-history-formatters.ts'
import {
  getRoundScore,
  getTeamBestScore,
  getTeamFinalScore,
  getTeamTotalBounties,
  getTeamPenaltyTotal,
  getTeamTotalKills,
  sortRoundsByPlaySequence,
  type GameHistoryTeamLeaderboardEntry,
} from '../model/game-history-team-leaderboard.ts'
import {
  ColumnLabel,
  CompactMetric,
  MiniMetricChip,
  RankBadge,
  TableValue,
} from './game-history-display.tsx'

type GameHistoryGameDetails = components['schemas']['GameHistoryGameDetailsDto']
type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function CurrentGameLeaderboard({
  gameDetails,
  leaderboard,
  onPreviewCard,
}: {
  gameDetails: GameHistoryGameDetails | null
  leaderboard: GameHistoryTeamLeaderboardEntry[]
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null)

  if (!gameDetails) {
    return null
  }

  const topEntry = leaderboard[0] ?? null
  const selectedEntry =
    leaderboard.find((entry) => entry.teamId === selectedTeamId) ?? topEntry ?? null

  return (
    <Stack spacing={1.5} sx={{ mt: 1.5 }}>
      {leaderboard.length === 0 ? (
        <Box
          sx={(theme) => ({
            borderRadius: 2,
            border: `1px dashed ${alpha(theme.palette.warning.main, 0.5)}`,
            backgroundColor: alpha(theme.palette.warning.main, 0.07),
            px: 1.5,
            py: 1.35,
          })}
        >
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.currentRoundsMissing')}
          </Typography>
        </Box>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gap: 1.5,
            gridTemplateColumns: {
              xs: '1fr',
              xl: 'minmax(0, 1.08fr) minmax(360px, 0.92fr)',
            },
            alignItems: 'start',
          }}
        >
          <CurrentLeaderboardTable
            entries={leaderboard}
            selectedTeamId={selectedEntry?.teamId ?? null}
            onSelectTeam={setSelectedTeamId}
          />

          <CurrentLeaderboardTeamDetails
            entry={selectedEntry}
            rank={
              selectedEntry
                ? leaderboard.findIndex((entry) => entry.teamId === selectedEntry.teamId) + 1
                : 0
            }
            onPreviewCard={onPreviewCard}
          />
        </Box>
      )}
    </Stack>
  )
}

function CurrentLeaderboardTable({
  entries,
  selectedTeamId,
  onSelectTeam,
}: {
  entries: readonly GameHistoryTeamLeaderboardEntry[]
  selectedTeamId: string | null
  onSelectTeam: (teamId: string) => void
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        overflow: 'hidden',
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.86)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.5),
      })}
    >
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={0.8}
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        justifyContent="space-between"
        sx={(theme) => ({
          px: 1.25,
          py: 1,
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.8)}`,
        })}
      >
        <Box sx={{ minWidth: 0 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
            {t('gameHistory.currentTableTitle')}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {t('gameHistory.currentTableDescription')}
          </Typography>
        </Box>
        <MiniMetricChip
          label={t('gameHistory.summary.teamCountShort', { count: entries.length })}
        />
      </Stack>

      <Box
        sx={(theme) => ({
          display: { xs: 'none', md: 'grid' },
          gridTemplateColumns: '70px minmax(180px, 1.4fr) repeat(6, minmax(82px, 0.55fr))',
          gap: 1,
          px: 1.25,
          py: 0.65,
          color: 'text.secondary',
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.7)}`,
        })}
      >
        <ColumnLabel>{t('gameHistory.table.rank')}</ColumnLabel>
        <ColumnLabel>{t('common.entities.team')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.final')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.penalties')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.best')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.rounds')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.kills')}</ColumnLabel>
        <ColumnLabel align="right">{t('gameHistory.table.bounties')}</ColumnLabel>
      </Box>

      <Stack>
        {entries.map((entry, index) => (
          <CurrentLeaderboardTableRow
            key={entry.teamId}
            entry={entry}
            rank={index + 1}
            isSelected={entry.teamId === selectedTeamId}
            onSelect={() => onSelectTeam(entry.teamId)}
          />
        ))}
      </Stack>
    </Box>
  )
}

function CurrentLeaderboardTableRow({
  entry,
  rank,
  isSelected,
  onSelect,
}: {
  entry: GameHistoryTeamLeaderboardEntry
  rank: number
  isSelected: boolean
  onSelect: () => void
}) {
  const { t } = useTranslation()
  const bestScore = getTeamBestScore(entry)
  const finalScore = getTeamFinalScore(entry)
  const penaltyTotal = getTeamPenaltyTotal(entry)
  const totalKills = getTeamTotalKills(entry)
  const totalBounties = getTeamTotalBounties(entry)

  return (
    <Box
      component="button"
      type="button"
      onClick={onSelect}
      sx={(theme) => ({
        width: '100%',
        minWidth: 0,
        border: 0,
        borderBottom: `1px solid ${alpha(theme.palette.divider, 0.62)}`,
        backgroundColor: isSelected
          ? alpha(theme.palette.primary.main, 0.12)
          : alpha(theme.palette.background.paper, 0),
        color: 'inherit',
        cursor: 'pointer',
        textAlign: 'left',
        px: 1.25,
        py: 0.85,
        transition: 'background-color 0.15s ease',
        '&:hover': {
          backgroundColor: alpha(theme.palette.primary.main, 0.08),
        },
        '&:last-of-type': {
          borderBottom: 0,
        },
      })}
    >
      <Box
        sx={{
          display: 'grid',
          gap: { xs: 0.65, md: 1 },
          gridTemplateColumns: {
            xs: '42px minmax(0, 1fr) auto',
            md: '70px minmax(180px, 1.4fr) repeat(6, minmax(82px, 0.55fr))',
          },
          alignItems: 'center',
        }}
      >
        <RankBadge rank={rank} compact />

        <Box sx={{ minWidth: 0 }}>
          <Typography variant="body2" sx={{ fontWeight: 800 }} noWrap>
            {formatHistoryTeamName(t, entry.teamName, entry.teamSlotIndex)}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {entry.participantNames.length > 0
              ? entry.participantNames.join(', ')
              : t('gameHistory.noParticipants')}
          </Typography>
          <Typography
            variant="caption"
            color="text.secondary"
            noWrap
            sx={{ display: { xs: 'block', md: 'none' } }}
          >
            {t('gameHistory.summary.bestAndPenaltyShort', {
              best: bestScore,
              penalty: penaltyTotal,
            })}
          </Typography>
        </Box>

        <TableValue strong>{finalScore}</TableValue>
        <TableValue hideOnMobile>{penaltyTotal}</TableValue>
        <TableValue strong hideOnMobile>
          {bestScore}
        </TableValue>
        <TableValue hideOnMobile>{entry.roundsPlayed}</TableValue>
        <TableValue hideOnMobile>{totalKills}</TableValue>
        <TableValue hideOnMobile>{totalBounties}</TableValue>
      </Box>
    </Box>
  )
}

function CurrentLeaderboardTeamDetails({
  entry,
  rank,
  onPreviewCard,
}: {
  entry: GameHistoryTeamLeaderboardEntry | null
  rank: number
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()

  if (!entry) {
    return null
  }
  const bestScore = getTeamBestScore(entry)
  const finalScore = getTeamFinalScore(entry)
  const penaltyTotal = getTeamPenaltyTotal(entry)
  const totalKills = getTeamTotalKills(entry)
  const totalBounties = getTeamTotalBounties(entry)
  const roundsByPlaySequence = sortRoundsByPlaySequence(entry.rounds)

  return (
    <Box
      sx={(theme) => ({
        position: { xl: 'sticky' },
        top: { xl: 92 },
        minWidth: 0,
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.primary.main, 0.24)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.58),
        overflow: 'hidden',
      })}
    >
      <Box
        sx={(theme) => ({
          px: 1.35,
          py: 1.2,
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
          background: `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.1)}, ${alpha(
            theme.palette.background.paper,
            0.35,
          )})`,
        })}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <RankBadge rank={rank} />
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 900 }} noWrap>
              {formatHistoryTeamName(t, entry.teamName, entry.teamSlotIndex)}
            </Typography>
            <Typography variant="body2" color="text.secondary" noWrap>
              {entry.participantNames.length > 0
                ? entry.participantNames.join(', ')
                : t('gameHistory.noParticipants')}
            </Typography>
          </Box>
        </Stack>
      </Box>

      <Stack spacing={1.15} sx={{ p: 1.35 }}>
        <Box
          sx={{
            display: 'grid',
            gap: 0.75,
            gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
          }}
        >
          <CompactMetric
            label={t('gameHistory.summary.finalScore')}
            value={t('gameHistory.pointsValue', { points: finalScore })}
          />
          <CompactMetric
            label={t('gameHistory.summary.penaltyTotal')}
            value={t('gameHistory.pointsValue', { points: penaltyTotal })}
          />
          <CompactMetric
            label={t('gameHistory.summary.bestScore')}
            value={t('gameHistory.pointsValue', { points: bestScore })}
          />
          <CompactMetric
            label={t('gameHistory.summary.averageScore')}
            value={t('gameHistory.pointsValue', { points: entry.averageScore })}
          />
          <CompactMetric
            label={t('gameHistory.summary.totalKills')}
            value={t('gameHistory.countValue', { count: totalKills })}
          />
          <CompactMetric
            label={t('gameHistory.summary.totalBounties')}
            value={t('gameHistory.countValue', { count: totalBounties })}
          />
        </Box>

        <Box
          sx={(theme) => ({
            borderRadius: 1.5,
            border: `1px solid ${alpha(theme.palette.warning.main, 0.28)}`,
            backgroundColor: alpha(theme.palette.warning.main, 0.08),
            px: 1,
            py: 0.9,
          })}
        >
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
            {t('gameHistory.summary.bestCard')}
          </Typography>
          <Typography variant="body2" sx={{ fontWeight: 800, mt: 0.25 }}>
            {formatShortCardLabel(entry.bestRound, t)}
          </Typography>
        </Box>

        <Stack spacing={0.7}>
          <Typography variant="overline" color="text.secondary">
            {t('gameHistory.summary.recentRounds')}
          </Typography>
          {roundsByPlaySequence.map((round) => (
            <CurrentLeaderboardRoundRow
              key={round.roundId}
              round={round}
              isBestRound={round.roundId === entry.bestRound.roundId}
              onPreviewCard={onPreviewCard}
            />
          ))}
        </Stack>
      </Stack>
    </Box>
  )
}

function CurrentLeaderboardRoundRow({
  round,
  isBestRound,
  onPreviewCard,
}: {
  round: GameHistoryRound
  isBestRound: boolean
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const modifiersCount = round.modifiers?.length ?? 0

  return (
    <Box
      sx={(theme) => ({
        borderRadius: 1.5,
        border: `1px solid ${
          isBestRound ? alpha(theme.palette.warning.main, 0.72) : alpha(theme.palette.divider, 0.78)
        }`,
        backgroundColor: isBestRound
          ? alpha(theme.palette.warning.main, 0.1)
          : alpha(theme.palette.background.paper, 0.45),
        boxShadow: isBestRound
          ? `inset 0 0 0 1px ${alpha(theme.palette.warning.main, 0.48)}`
          : 'none',
        px: 1,
        py: 0.85,
      })}
    >
      <Stack spacing={0.7}>
        <Stack direction="row" spacing={0.8} alignItems="flex-start">
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 800 }} noWrap>
              {round.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
            </Typography>
          </Box>
          <Typography variant="body2" sx={{ fontWeight: 900, flexShrink: 0 }}>
            {t('gameHistory.pointsValue', { points: getRoundScore(round) })}
          </Typography>
        </Stack>

        <Box
          sx={{
            display: 'flex',
            gap: 0.75,
            alignItems: 'flex-end',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
          }}
        >
          <Stack direction="row" spacing={0.55} alignItems="center" flexWrap="wrap" useFlexGap>
            <MiniMetricChip label={t('gameHistory.cardCostLabel', { cost: round.cellCost })} />
            <MiniMetricChip
              label={t('gameHistory.summary.killsShort', {
                count: round.scoreDetails.totalKillCount,
              })}
            />
            <MiniMetricChip
              label={t('gameHistory.summary.bountiesShort', { count: round.bountyCount })}
            />
            <MiniMetricChip
              label={t('gameHistory.summary.modifierCountShort', { count: modifiersCount })}
            />
          </Stack>
          <AppButton
            size="small"
            tone="secondary"
            onClick={() => onPreviewCard(round)}
            sx={{ ml: 'auto', flexShrink: 0 }}
          >
            {t('gameHistory.openCardAction')}
          </AppButton>
        </Box>
      </Stack>
    </Box>
  )
}
