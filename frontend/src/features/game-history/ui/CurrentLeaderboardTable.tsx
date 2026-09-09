import { Box, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import { formatHistoryTeamName } from '../model/game-history-formatters.ts'
import {
  getTeamBestScore,
  getTeamFinalScore,
  getTeamPenaltyTotal,
  getTeamTotalBounties,
  getTeamTotalKills,
  type GameHistoryTeamLeaderboardEntry,
} from '../model/game-history-team-leaderboard.ts'
import { ColumnLabel, MiniMetricChip, RankBadge, TableValue } from './game-history-display.tsx'

export function CurrentLeaderboardTable({
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
