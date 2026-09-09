import { Box, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { type GameHistoryTeamLeaderboardEntry } from '../model/game-history-team-leaderboard.ts'
import { CancelledRoundsSection } from './CancelledRoundsSection.tsx'
import { CurrentLeaderboardTable } from './CurrentLeaderboardTable.tsx'
import { CurrentLeaderboardTeamDetails } from './CurrentLeaderboardTeamDetails.tsx'
import { GameModifierHistorySummary } from './GameModifierHistorySummary.tsx'

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
  const cancelledRounds = gameDetails.mainGame.rounds.filter(
    (round) => round.status === 'cancelled',
  )

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
      <GameModifierHistorySummary
        rounds={gameDetails.mainGame.rounds}
        snapshots={gameDetails.modifierSnapshots}
        snapshotStatus={gameDetails.modifierSnapshotStatus}
      />
      <CancelledRoundsSection rounds={cancelledRounds} onPreviewCard={onPreviewCard} />
    </Stack>
  )
}
