import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { SectionCard } from '../../../shared/ui/index.ts'
import { sortTeamLeaderboardEntries } from '../model/game-history-team-leaderboard.ts'
import {
  formatDateTime,
  formatOptionalDateTime,
  getGameStatusColor,
  isCountedRound,
  normalizeStatus,
} from '../model/game-history-view.ts'
import { CancelledRoundsSection } from './CancelledRoundsSection.tsx'
import { FinalResultSnapshot } from './GameHistoryFinalResult.tsx'
import { TeamLeaderboardRow } from './GameHistoryLeaderboard.tsx'
import { RoundHistoryRow } from './GameHistoryRoundRow.tsx'
import { CollapsibleSection, MetricChip } from './game-history-surfaces.tsx'
import { GameModifierHistorySummary } from './GameModifierHistorySummary.tsx'

type GameHistoryGameDetails = components['schemas']['GameHistoryGameDetailsDto']
type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function GameDetailsPanel({
  game,
  onPreviewCard,
}: {
  game: GameHistoryGameDetails | null
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t, i18n } = useTranslation()

  if (!game) {
    return null
  }
  const teamStats = sortTeamLeaderboardEntries(game.mainGame.teamStats)
  const finalResult = game.finalResult ?? null
  const completedRounds = game.mainGame.rounds.filter(isCountedRound)
  const cancelledRounds = game.mainGame.rounds.filter((round) => round.status === 'cancelled')

  return (
    <Stack spacing={2} sx={{ mt: 1.5 }}>
      <Box
        sx={(theme) => ({
          borderRadius: 2.5,
          border: `1px solid ${alpha(theme.palette.primary.main, 0.18)}`,
          background: `linear-gradient(180deg, ${alpha(theme.palette.primary.main, 0.08)} 0%, transparent 100%)`,
          px: { xs: 1.75, sm: 2 },
          py: { xs: 1.75, sm: 2 },
        })}
      >
        <Stack spacing={1.25}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Chip
                  label={t(`gameHistory.status.${normalizeStatus(game.gameStatus)}`)}
                  color={getGameStatusColor(game.gameStatus)}
                />
                <Chip
                  label={t('gameHistory.statusChipArchived')}
                  color="default"
                  variant="outlined"
                />
              </Stack>

              <Typography variant="h5" sx={{ mt: 1, fontWeight: 800 }}>
                {game.gameTitle}
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <MetricChip
              label={t('gameHistory.summary.createdAt')}
              value={formatDateTime(game.createdAtUtc, i18n.resolvedLanguage)}
            />
            <MetricChip
              label={t('gameHistory.summary.startedAt')}
              value={formatOptionalDateTime(game.startedAtUtc, t, i18n.resolvedLanguage)}
            />
            <MetricChip
              label={t('gameHistory.summary.finishedAt')}
              value={formatOptionalDateTime(game.finishedAtUtc, t, i18n.resolvedLanguage)}
            />
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <MetricChip
              label={t('gameHistory.summary.roundCount')}
              value={t('gameHistory.countValue', { count: completedRounds.length })}
            />
            <MetricChip
              label={t('common.entities.modifiers')}
              value={t('gameHistory.countValue', {
                count: game.mainGame.modifierActivations.length,
              })}
            />
            <MetricChip
              label={t('gameHistory.summary.quizCount')}
              value={t('gameHistory.countValue', { count: game.quiz.rounds.length })}
            />
            <MetricChip
              label={t('gameHistory.summary.quizPoints')}
              value={t('gameHistory.pointsValue', { points: game.quiz.totalPoints })}
            />
          </Stack>
        </Stack>
      </Box>

      {finalResult ? <FinalResultSnapshot summary={finalResult} /> : null}

      {!finalResult ? (
        <SectionCard inset sx={{ p: 0 }}>
          <CollapsibleSection
            title={t('gameHistory.summary.bestTeams')}
            description={t('gameHistory.summary.bestTeamsDescription')}
            countLabel={t('gameHistory.summary.teamCountShort', {
              count: teamStats.length,
            })}
            defaultExpanded
          >
            {teamStats.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameHistory.summary.noRounds')}
              </Typography>
            ) : (
              <Stack spacing={1}>
                {teamStats.map((entry, index) => (
                  <TeamLeaderboardRow
                    key={entry.teamId}
                    entry={entry}
                    rank={index + 1}
                    onPreviewCard={onPreviewCard}
                  />
                ))}
              </Stack>
            )}
          </CollapsibleSection>
        </SectionCard>
      ) : null}

      <GameModifierHistorySummary
        rounds={completedRounds}
        snapshots={game.modifierSnapshots}
        snapshotStatus={game.modifierSnapshotStatus}
      />

      <SectionCard inset sx={{ p: 0 }}>
        <CollapsibleSection
          title={t('gameHistory.summary.modifierTimeline')}
          description={t('gameHistory.summary.modifierTimelineDescription')}
          countLabel={t('gameHistory.summary.modifierCountShort', {
            count: game.mainGame.modifierActivations.length,
          })}
        >
          {game.mainGame.modifierActivations.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.summary.noModifiers')}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {game.mainGame.modifierActivations.map((activation) => (
                <Box
                  key={activation.activationId}
                  sx={(theme) => ({
                    borderRadius: 2,
                    border: `1px solid ${alpha(theme.palette.divider, 0.88)}`,
                    px: 1.5,
                    py: 1.25,
                  })}
                >
                  <Stack
                    direction={{ xs: 'column', md: 'row' }}
                    spacing={1}
                    alignItems="flex-start"
                  >
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Typography variant="body2" sx={{ fontWeight: 700 }}>
                        {activation.modifierName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {t('gameHistory.modifierActivatedBy', {
                          user: activation.activatedByDisplayName,
                        })}
                      </Typography>
                    </Box>
                    <Typography variant="caption" color="text.secondary">
                      {formatDateTime(activation.activatedAtUtc, i18n.resolvedLanguage)}
                    </Typography>
                  </Stack>
                </Box>
              ))}
            </Stack>
          )}
        </CollapsibleSection>
      </SectionCard>

      <SectionCard inset sx={{ p: 0 }}>
        <CollapsibleSection
          title={t('gameHistory.summary.roundHistory')}
          description={t('gameHistory.summary.roundHistoryDescription')}
          countLabel={t('gameHistory.summary.roundCountShort', {
            count: completedRounds.length,
          })}
        >
          {completedRounds.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.summary.noRounds')}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {completedRounds.map((round) => (
                <RoundHistoryRow key={round.roundId} round={round} onPreviewCard={onPreviewCard} />
              ))}
            </Stack>
          )}
        </CollapsibleSection>
      </SectionCard>

      <CancelledRoundsSection rounds={cancelledRounds} onPreviewCard={onPreviewCard} />
    </Stack>
  )
}
