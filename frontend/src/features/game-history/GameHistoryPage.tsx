import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import { alpha, type Theme } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../shared/api/contracts/generated'
import { resolveBackendMediaUrl } from '../../shared/api/media-url.ts'
import {
  AppButton,
  AppDialog,
  AsyncSection,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import {
  gameHistoryGameDetailsQueryOptions,
  gameHistoryGamesQueryOptions,
} from './api/game-history-queries.ts'
import {
  buildGameTeamLeaderboard,
  getRoundBonusDelta,
  getRoundScore,
  type GameHistoryTeamLeaderboardEntry,
} from './model/game-history-team-leaderboard.ts'

type GameHistoryGameSummary = components['schemas']['GameHistoryGameSummaryDto']
type GameHistoryGameDetails = components['schemas']['GameHistoryGameDetailsDto']
type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function GameHistoryPage() {
  const { t } = useTranslation()
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null)
  const [previewRound, setPreviewRound] = useState<GameHistoryRound | null>(null)
  const [activeBoard, setActiveBoard] = useState<'realtime' | 'history'>('realtime')

  const currentGameQuery = useQuery(currentGameBoardQueryOptions)
  const gamesQuery = useQuery(gameHistoryGamesQueryOptions)
  const currentGameId = currentGameQuery.data?.gameId ?? null
  const completedGames = (gamesQuery.data ?? []).filter(
    (game) => normalizeStatus(game.gameStatus) === 'finished',
  )
  const selectedCompletedGameId =
    selectedGameId && completedGames.some((game) => game.gameId === selectedGameId)
      ? selectedGameId
      : (completedGames[0]?.gameId ?? null)

  const currentGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(currentGameId ?? ''),
    enabled: currentGameId !== null,
  })
  const selectedGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(selectedCompletedGameId ?? ''),
    enabled: selectedCompletedGameId !== null,
  })

  const currentGameLeaderboard = buildGameTeamLeaderboard(
    currentGameDetailsQuery.data?.mainGame.rounds ?? [],
  )

  return (
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
      }}
    >
      <SectionHeader title={t('gameHistory.title')} description={t('gameHistory.description')} />

      <SectionCard sx={{ mt: 1.5 }}>
        <SectionHeader
          title={t('gameHistory.boardSwitcherTitle')}
          description={t('gameHistory.boardSwitcherDescription')}
        />

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25} sx={{ mt: 1.5 }}>
          <BoardSwitchCard
            title={t('gameHistory.realtimeTitle')}
            description={t('gameHistory.realtimeDescription')}
            isActive={activeBoard === 'realtime'}
            onClick={() => setActiveBoard('realtime')}
          />
          <BoardSwitchCard
            title={t('gameHistory.completedGamesTitle')}
            description={t('gameHistory.completedGamesDescription')}
            isActive={activeBoard === 'history'}
            onClick={() => setActiveBoard('history')}
          />
        </Stack>
      </SectionCard>

      {activeBoard === 'realtime' ? (
        <SectionCard sx={{ mt: 2 }}>
          <SectionHeader
            title={t('gameHistory.realtimeTitle')}
            description={t('gameHistory.realtimeDescription')}
          />

          <AsyncSection
            isLoading={
              currentGameQuery.isLoading ||
              (currentGameId !== null && currentGameDetailsQuery.isLoading)
            }
            isError={currentGameQuery.isError || currentGameDetailsQuery.isError}
            isEmpty={currentGameId === null}
            loadingMessage={t('gameHistory.loadingCurrentGame')}
            errorMessage={t('gameHistory.errorCurrentGame')}
            emptyMessage={t('gameHistory.currentGameMissing')}
          >
            <CurrentGameLeaderboard
              gameDetails={currentGameDetailsQuery.data ?? null}
              leaderboard={currentGameLeaderboard}
              onPreviewCard={setPreviewRound}
            />
          </AsyncSection>
        </SectionCard>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gap: 2,
            mt: 2,
            alignItems: 'start',
            gridTemplateColumns: {
              xs: '1fr',
              xl: '340px minmax(0, 1fr)',
            },
          }}
        >
          <SectionCard sx={{ minWidth: 0 }}>
            <SectionHeader
              title={t('gameHistory.completedGamesListTitle')}
              description={t('gameHistory.completedGamesListDescription')}
            />

            <AsyncSection
              isLoading={gamesQuery.isLoading}
              isError={gamesQuery.isError}
              isEmpty={completedGames.length === 0}
              loadingMessage={t('gameHistory.loadingGames')}
              errorMessage={t('gameHistory.errorGames')}
              emptyMessage={t('gameHistory.completedGamesEmpty')}
            >
              <Stack spacing={1.1} sx={{ mt: 1.5 }}>
                {completedGames.map((game) => (
                  <GameSummaryButton
                    key={game.gameId}
                    game={game}
                    isSelected={game.gameId === selectedCompletedGameId}
                    onClick={() => setSelectedGameId(game.gameId)}
                  />
                ))}
              </Stack>
            </AsyncSection>
          </SectionCard>

          <SectionCard sx={{ minWidth: 0 }}>
            <SectionHeader
              title={t('gameHistory.completedGamesTitle')}
              description={t('gameHistory.completedGamesDescription')}
            />

            <AsyncSection
              isLoading={selectedCompletedGameId !== null && selectedGameDetailsQuery.isLoading}
              isError={selectedGameDetailsQuery.isError}
              isEmpty={selectedCompletedGameId === null}
              loadingMessage={t('gameHistory.loadingGameDetails')}
              errorMessage={t('gameHistory.errorGameDetails')}
              emptyMessage={t('gameHistory.completedGameSelectPrompt')}
            >
              <GameDetailsPanel
                game={selectedGameDetailsQuery.data ?? null}
                onPreviewCard={setPreviewRound}
              />
            </AsyncSection>
          </SectionCard>
        </Box>
      )}

      <CardPreviewDialog round={previewRound} onClose={() => setPreviewRound(null)} />
    </PageShell>
  )
}

function BoardSwitchCard({
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

function CurrentGameLeaderboard({
  gameDetails,
  leaderboard,
  onPreviewCard,
}: {
  gameDetails: GameHistoryGameDetails | null
  leaderboard: GameHistoryTeamLeaderboardEntry[]
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()

  if (!gameDetails) {
    return null
  }

  const topEntry = leaderboard[0] ?? null

  return (
    <Stack spacing={2} sx={{ mt: 1.5 }}>
      <Box
        sx={(theme) => ({
          position: 'relative',
          overflow: 'hidden',
          borderRadius: 2.5,
          border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
          background: `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.18)} 0%, ${alpha(
            theme.palette.success.main,
            0.14,
          )} 45%, ${alpha(theme.palette.info.main, 0.18)} 100%)`,
          px: { xs: 1.5, sm: 1.8 },
          py: { xs: 1.5, sm: 1.75 },
          boxShadow: `0 14px 30px ${alpha(theme.palette.common.black, 0.18)}`,
        })}
      >
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} alignItems="stretch">
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Chip label={t('gameHistory.statusChipCurrentGame')} color="warning" />
              <Chip
                label={t(`gameHistory.status.${normalizeStatus(gameDetails.gameStatus)}`)}
                color={getGameStatusColor(gameDetails.gameStatus)}
                variant="outlined"
              />
            </Stack>

            <Typography variant="h5" sx={{ mt: 0.9, fontWeight: 800 }}>
              {gameDetails.gameTitle}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.45 }}>
              {t('gameHistory.currentLeaderboardRule')}
            </Typography>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap sx={{ mt: 1.15 }}>
              <MetricChip
                label={t('gameHistory.summary.runCount')}
                value={t('gameHistory.countValue', {
                  count: gameDetails.mainGame.rounds.length,
                })}
              />
              <MetricChip
                label={t('gameHistory.summary.modifierCount')}
                value={t('gameHistory.countValue', {
                  count: gameDetails.mainGame.modifierActivations.length,
                })}
              />
              <MetricChip
                label={t('gameHistory.summary.teamCount')}
                value={t('gameHistory.countValue', {
                  count: leaderboard.length,
                })}
              />
            </Stack>
          </Box>

          <Box
            sx={(theme) => ({
              width: { xs: '100%', lg: 290 },
              borderRadius: 2,
              border: `1px solid ${alpha(theme.palette.common.white, 0.1)}`,
              backgroundColor: alpha(theme.palette.common.black, 0.16),
              p: 1.5,
            })}
          >
            <Typography variant="overline" color="text.secondary">
              {t('gameHistory.currentLeaderTitle')}
            </Typography>

            {topEntry ? (
              <Stack spacing={1.2} sx={{ mt: 0.65 }}>
                <Typography variant="h6" sx={{ fontWeight: 800 }}>
                  {t('gameHistory.teamLabel', { slot: topEntry.teamSlotIndex })}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {topEntry.participantNames.length > 0
                    ? topEntry.participantNames.join(', ')
                    : t('gameHistory.noParticipants')}
                </Typography>
                <MetricRow
                  label={t('gameHistory.summary.bestScore')}
                  value={t('gameHistory.pointsValue', { points: topEntry.bestScore })}
                />
                <MetricRow
                  label={t('gameHistory.summary.bestCard')}
                  value={formatCardLabel(topEntry.bestRound, t)}
                />
              </Stack>
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {t('gameHistory.currentRoundsMissing')}
              </Typography>
            )}
          </Box>
        </Stack>
      </Box>

      {leaderboard.length > 0 ? (
        <Stack spacing={1}>
          {leaderboard.map((entry, index) => (
            <TeamLeaderboardRow
              key={entry.teamId}
              entry={entry}
              rank={index + 1}
              onPreviewCard={onPreviewCard}
            />
          ))}
        </Stack>
      ) : null}
    </Stack>
  )
}

function GameSummaryButton({
  game,
  isSelected,
  onClick,
}: {
  game: GameHistoryGameSummary
  isSelected: boolean
  onClick: () => void
}) {
  const { t } = useTranslation()

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
          {formatGameTimeLabel(game, t)}
        </Typography>

        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          <MiniMetricChip
            label={t('gameHistory.summary.roundCountShort', {
              count: game.mainGameRunCount,
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

function GameDetailsPanel({
  game,
  onPreviewCard,
}: {
  game: GameHistoryGameDetails | null
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()

  if (!game) {
    return null
  }

  const leaderboard = buildGameTeamLeaderboard(game.mainGame.rounds)
  const quizPointsTotal = game.quiz.playerStats.reduce((sum, entry) => sum + entry.points, 0)

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
              value={formatDateTime(game.createdAtUtc)}
            />
            <MetricChip
              label={t('gameHistory.summary.startedAt')}
              value={formatOptionalDateTime(game.startedAtUtc, t)}
            />
            <MetricChip
              label={t('gameHistory.summary.finishedAt')}
              value={formatOptionalDateTime(game.finishedAtUtc, t)}
            />
          </Stack>

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <MetricChip
              label={t('gameHistory.summary.runCount')}
              value={t('gameHistory.countValue', { count: game.mainGame.rounds.length })}
            />
            <MetricChip
              label={t('gameHistory.summary.modifierCount')}
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
              value={t('gameHistory.pointsValue', { points: quizPointsTotal })}
            />
          </Stack>
        </Stack>
      </Box>

      <SectionCard inset sx={{ p: 0 }}>
        <CollapsibleSection
          title={t('gameHistory.summary.bestTeams')}
          description={t('gameHistory.summary.bestTeamsDescription')}
          countLabel={t('gameHistory.summary.teamCountShort', { count: leaderboard.length })}
          defaultExpanded
        >
          {leaderboard.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.summary.noRounds')}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {leaderboard.map((entry, index) => (
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
                      {formatDateTime(activation.activatedAtUtc)}
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
            count: game.mainGame.rounds.length,
          })}
        >
          {game.mainGame.rounds.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.summary.noRounds')}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {game.mainGame.rounds.map((round) => (
                <RoundHistoryRow key={round.roundId} round={round} onPreviewCard={onPreviewCard} />
              ))}
            </Stack>
          )}
        </CollapsibleSection>
      </SectionCard>
    </Stack>
  )
}

function TeamLeaderboardRow({
  entry,
  rank,
  onPreviewCard,
}: {
  entry: GameHistoryTeamLeaderboardEntry
  rank: number
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const recentRounds = entry.rounds.slice(0, 3)

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
                      {t('gameHistory.teamLabel', { slot: entry.teamSlotIndex })}
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
                  label={t('gameHistory.summary.bestScoreShort', { points: entry.bestScore })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.averageScoreShort', {
                    points: entry.averageScore,
                  })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.killsShort', { count: entry.totalKills })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.bountiesShort', {
                    count: entry.totalBounties,
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
              label={t('gameHistory.summary.bestScore')}
              value={t('gameHistory.pointsValue', { points: entry.bestScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.averageScore')}
              value={t('gameHistory.pointsValue', { points: entry.averageScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.totalScore')}
              value={t('gameHistory.pointsValue', { points: entry.totalScore })}
            />
            <MetricChip
              label={t('gameHistory.summary.latestResult')}
              value={t('gameHistory.pointsValue', {
                points: getRoundScore(entry.latestRound),
              })}
            />
            <MetricChip
              label={t('gameHistory.summary.bonusDelta')}
              value={formatSignedPoints(getRoundBonusDelta(entry.bestRound), t)}
            />
            <MetricChip
              label={t('gameHistory.summary.totalKills')}
              value={t('gameHistory.countValue', { count: entry.totalKills })}
            />
            <MetricChip
              label={t('gameHistory.summary.totalBounties')}
              value={t('gameHistory.countValue', { count: entry.totalBounties })}
            />
            <MetricChip
              label={t('gameHistory.summary.bestCard')}
              value={formatShortCardLabel(entry.bestRound, t)}
            />
          </Box>

          <CollapsibleSection
            title={t('gameHistory.summary.allRunsTitle')}
            description={t('gameHistory.summary.allRunsDescription')}
            countLabel={t('gameHistory.summary.roundCountShort', {
              count: entry.rounds.length,
            })}
            nested
            defaultExpanded={rank === 1}
          >
            <Stack spacing={1}>
              {entry.rounds.map((round) => (
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
          {isBestRound ? <MiniMetricChip label={t('gameHistory.summary.bestRoundChip')} /> : null}
        </Stack>
        <Typography variant="caption" color="text.secondary">
          {round.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('gameHistory.summary.killsShort', { count: round.killsCount })} ·{' '}
          {t('gameHistory.summary.bountiesShort', { count: round.bountyCount })}
        </Typography>
      </Stack>
    </Box>
  )
}

function LeaderboardRoundCard({
  round,
  isBestRound,
  onPreviewCard,
}: {
  round: GameHistoryRound
  isBestRound: boolean
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const participants = round.participants ?? []
  const modifiers = round.modifiers ?? []
  const modifierScoreDelta = modifiers.reduce((sum, modifier) => sum + modifier.scoreDelta, 0)

  return (
    <AccordionSurface defaultExpanded={isBestRound}>
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 1.25,
          py: 0.1,
          '& .MuiAccordionSummary-content': {
            my: 0.65,
          },
        }}
      >
        <Box sx={{ width: '100%' }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={0.75} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="body2" sx={{ fontWeight: 800 }}>
                  {round.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
                </Typography>
                {isBestRound ? (
                  <Chip
                    size="small"
                    color="warning"
                    label={t('gameHistory.summary.bestRoundChip')}
                  />
                ) : null}
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.pointsValue', { points: getRoundScore(round) })}
                />
              </Stack>

              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ mt: 0.5, display: 'block' }}
              >
                {formatCardLabel(round, t)}
              </Typography>
            </Box>

            <Stack direction="row" spacing={0.55} flexWrap="wrap" useFlexGap>
              <MiniMetricChip
                label={t('gameHistory.summary.killsShort', { count: round.killsCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bountiesShort', { count: round.bountyCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bonusShort', {
                  value: formatSignedNumber(getRoundBonusDelta(round)),
                })}
              />
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.25, pt: 0, pb: 1.25 }}>
        <Stack spacing={1.2}>
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <MiniMetricChip
              label={t('gameHistory.summary.baseScoreShort', {
                points: round.baseScore,
              })}
            />
            <MiniMetricChip
              label={t('gameHistory.summary.finalScoreShort', {
                points: getRoundScore(round),
              })}
            />
            {modifierScoreDelta !== 0 ? (
              <MiniMetricChip
                label={t('gameHistory.summary.modifierDeltaShort', {
                  value: formatSignedNumber(modifierScoreDelta),
                })}
              />
            ) : null}
          </Stack>

          {round.cellDescription ? (
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
              {round.cellDescription}
            </Typography>
          ) : null}

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.2} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="caption" color="text.secondary">
                {t('gameHistory.participantsLabel')}
              </Typography>
              <Typography variant="body2">
                {participants.length > 0
                  ? participants.map((participant) => participant.displayName).join(', ')
                  : t('gameHistory.noParticipants')}
              </Typography>
            </Box>

            <AppButton
              size="small"
              tone="secondary"
              onClick={() => onPreviewCard(round)}
              sx={{ flexShrink: 0 }}
            >
              {t('gameHistory.openCardAction')}
            </AppButton>
          </Stack>

          {modifiers.length > 0 ? (
            <CollapsibleSection
              title={t('gameHistory.modifiersLabel')}
              description={t('gameHistory.summary.roundModifierDescription')}
              countLabel={t('gameHistory.summary.modifierCountShort', {
                count: modifiers.length,
              })}
              nested
            >
              <Stack spacing={0.75}>
                {modifiers.map((modifier) => (
                  <Box
                    key={modifier.modifierResultId}
                    sx={(theme) => ({
                      minWidth: 0,
                      borderRadius: 1.5,
                      border: `1px solid ${alpha(theme.palette.divider, 0.86)}`,
                      px: 1,
                      py: 0.9,
                    })}
                  >
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={0.75}
                      alignItems="flex-start"
                    >
                      <Box sx={{ minWidth: 0, flex: 1 }}>
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          {modifier.modifierName}
                        </Typography>
                        {modifier.modifierDescription ? (
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{ display: 'block', mt: 0.25, whiteSpace: 'pre-line' }}
                          >
                            {modifier.modifierDescription}
                          </Typography>
                        ) : null}
                        <Typography variant="caption" color="text.secondary">
                          {t('gameHistory.summary.modifierImpactLabel', {
                            value: formatSignedNumber(modifier.scoreDelta),
                          })}
                        </Typography>
                      </Box>
                      <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                        <MiniMetricChip label={formatEnumLabel(modifier.outcomeStatus)} />
                        {modifier.killDelta !== 0 ? (
                          <MiniMetricChip
                            label={t('gameHistory.summary.killDeltaShort', {
                              value: formatSignedNumber(modifier.killDelta),
                            })}
                          />
                        ) : null}
                      </Stack>
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </CollapsibleSection>
          ) : null}

          {round.notes ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.notesLabel', { notes: round.notes })}
            </Typography>
          ) : null}
        </Stack>
      </AccordionDetails>
    </AccordionSurface>
  )
}

function RoundHistoryRow({
  round,
  onPreviewCard,
}: {
  round: GameHistoryRound
  onPreviewCard: (round: GameHistoryRound) => void
}) {
  const { t } = useTranslation()
  const participants = round.participants ?? []
  const modifiers = round.modifiers ?? []
  const modifierScoreDelta = modifiers.reduce((sum, modifier) => sum + modifier.scoreDelta, 0)

  return (
    <AccordionSurface>
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 1.75,
          py: 0.25,
          '& .MuiAccordionSummary-content': {
            my: 1,
          },
        }}
      >
        <Box sx={{ width: '100%' }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="body1" sx={{ fontWeight: 800 }}>
                  {t('gameHistory.teamLabel', { slot: round.teamSlotIndex })}
                </Typography>
                <Chip
                  size="small"
                  label={t(`gameHistory.roundStatus.${normalizeRoundStatus(round.status)}`)}
                  color={getRoundStatusColor(round.status)}
                />
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.pointsValue', { points: getRoundScore(round) })}
                />
              </Stack>

              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.55 }}>
                {formatCardLabel(round, t)}
              </Typography>
            </Box>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <MiniMetricChip
                label={t('gameHistory.summary.killsShort', { count: round.killsCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bountiesShort', { count: round.bountyCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bonusShort', {
                  value: formatSignedNumber(getRoundBonusDelta(round)),
                })}
              />
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.75, pt: 0, pb: 1.75 }}>
        <Stack spacing={1.2}>
          <Typography variant="caption" color="text.secondary">
            {formatOptionalDateTime(round.finishedAtUtc ?? round.startedAtUtc, t)}
          </Typography>

          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <MiniMetricChip
              label={t('gameHistory.summary.baseScoreShort', {
                points: round.baseScore,
              })}
            />
            {modifierScoreDelta !== 0 ? (
              <MiniMetricChip
                label={t('gameHistory.summary.modifierDeltaShort', {
                  value: formatSignedNumber(modifierScoreDelta),
                })}
              />
            ) : null}
          </Stack>

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.2} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="caption" color="text.secondary">
                {t('gameHistory.participantsLabel')}
              </Typography>
              <Typography variant="body2">
                {participants.length > 0
                  ? participants.map((participant) => participant.displayName).join(', ')
                  : t('gameHistory.noParticipants')}
              </Typography>
            </Box>

            <AppButton
              size="small"
              tone="secondary"
              onClick={() => onPreviewCard(round)}
              sx={{ flexShrink: 0 }}
            >
              {t('gameHistory.openCardAction')}
            </AppButton>
          </Stack>

          {modifiers.length > 0 ? (
            <CollapsibleSection
              title={t('gameHistory.modifiersLabel')}
              description={t('gameHistory.summary.roundModifierDescription')}
              countLabel={t('gameHistory.summary.modifierCountShort', {
                count: modifiers.length,
              })}
              nested
            >
              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                {modifiers.map((modifier) => (
                  <Box
                    key={modifier.modifierResultId}
                    sx={(theme) => ({
                      minWidth: 0,
                      borderRadius: 1.5,
                      border: `1px solid ${alpha(theme.palette.divider, 0.8)}`,
                      px: 1,
                      py: 0.8,
                    })}
                  >
                    <Typography variant="caption" sx={{ fontWeight: 700, display: 'block' }}>
                      {t('gameHistory.modifierChipLabel', {
                        modifier: modifier.modifierName,
                        value: formatSignedNumber(modifier.scoreDelta),
                      })}
                    </Typography>
                    {modifier.modifierDescription ? (
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ display: 'block', mt: 0.25, whiteSpace: 'pre-line' }}
                      >
                        {modifier.modifierDescription}
                      </Typography>
                    ) : null}
                  </Box>
                ))}
              </Stack>
            </CollapsibleSection>
          ) : null}

          {round.notes ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.notesLabel', { notes: round.notes })}
            </Typography>
          ) : null}
        </Stack>
      </AccordionDetails>
    </AccordionSurface>
  )
}

function CardPreviewDialog({
  round,
  onClose,
}: {
  round: GameHistoryRound | null
  onClose: () => void
}) {
  const { t } = useTranslation()
  const cellMedia = round?.cellMedia ?? []

  return (
    <AppDialog
      open={round !== null}
      onClose={onClose}
      maxWidth="md"
      title={
        round
          ? round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
          : t('gameHistory.cardDialogFallbackTitle')
      }
      description={
        round
          ? t('gameHistory.cardDialogDescription', {
              slot: round.teamSlotIndex,
            })
          : undefined
      }
      actions={
        <AppButton tone="secondary" onClick={onClose}>
          {t('gameHistory.closeAction')}
        </AppButton>
      }
    >
      {round ? (
        <Stack spacing={2}>
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <Chip label={t('gameHistory.teamLabel', { slot: round.teamSlotIndex })} />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardCoordinate', {
                row: round.cellRowIndex + 1,
                col: round.cellColIndex + 1,
              })}
            />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardCostLabel', { cost: round.cellCost })}
            />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardTypeLabel', { type: round.cellType })}
            />
          </Stack>

          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('gameHistory.cardDescriptionLabel')}
            </Typography>
            <Typography variant="body2" sx={{ mt: 0.7, whiteSpace: 'pre-line' }}>
              {round.cellDescription || t('gameHistory.cardDescriptionEmpty')}
            </Typography>
          </Box>

          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('gameHistory.cardMediaLabel')}
            </Typography>

            {cellMedia.length === 0 ? (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.7 }}>
                {t('gameHistory.cardMediaEmpty')}
              </Typography>
            ) : (
              <Box
                sx={{
                  display: 'grid',
                  gap: 1,
                  mt: 0.9,
                  gridTemplateColumns: {
                    xs: '1fr',
                    sm: 'repeat(2, minmax(0, 1fr))',
                  },
                }}
              >
                {cellMedia.map((media, index) => (
                  <Box
                    key={`${media.url}-${index}`}
                    component="img"
                    src={resolveBackendMediaUrl(media.url)}
                    alt={round.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
                    loading="lazy"
                    decoding="async"
                    sx={{
                      width: '100%',
                      borderRadius: 2,
                      border: '1px solid',
                      borderColor: 'divider',
                      objectFit: 'cover',
                      maxHeight: 280,
                    }}
                  />
                ))}
              </Box>
            )}
          </Box>
        </Stack>
      ) : null}
    </AppDialog>
  )
}

function AccordionSurface({
  children,
  defaultExpanded = false,
}: {
  children: ReactNode
  defaultExpanded?: boolean
}) {
  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      sx={(theme) => ({
        borderRadius: 2.5,
        border: `1px solid ${alpha(theme.palette.divider, 0.88)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.58),
        overflow: 'hidden',
        '&::before': {
          display: 'none',
        },
      })}
    >
      {children}
    </Accordion>
  )
}

function CollapsibleSection({
  title,
  description,
  countLabel,
  children,
  defaultExpanded = false,
  nested = false,
}: {
  title: string
  description: string
  countLabel?: string
  children: ReactNode
  defaultExpanded?: boolean
  nested?: boolean
}) {
  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      sx={(theme) => ({
        backgroundColor: 'transparent',
        '&::before': {
          display: 'none',
        },
        ...(nested
          ? {
              border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
              borderRadius: 2,
              overflow: 'hidden',
            }
          : {}),
      })}
    >
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 2,
          py: nested ? 0.15 : 0.35,
          '& .MuiAccordionSummary-content': {
            my: 1,
          },
        }}
      >
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
          >
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Typography variant="overline" color="text.secondary">
                {title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {description}
              </Typography>
            </Box>
            {countLabel ? <MiniMetricChip label={countLabel} /> : null}
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 2, pt: 0, pb: 2 }}>{children}</AccordionDetails>
    </Accordion>
  )
}

function ExpandGlyph() {
  return (
    <Typography variant="body2" fontWeight={800} color="text.secondary">
      ▾
    </Typography>
  )
}

function MetricChip({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: 999,
        border: `1px solid ${alpha(theme.palette.divider, 0.88)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.54),
        minWidth: 0,
        px: 1,
        py: 0.7,
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography
        variant="body2"
        sx={{
          fontWeight: 700,
          display: '-webkit-box',
          overflow: 'hidden',
          WebkitLineClamp: 2,
          WebkitBoxOrient: 'vertical',
        }}
      >
        {value}
      </Typography>
    </Box>
  )
}

function MetricRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" spacing={1} justifyContent="space-between">
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 700, textAlign: 'right' }}>
        {value}
      </Typography>
    </Stack>
  )
}

function MiniMetricChip({ label }: { label: string }) {
  return (
    <Chip
      size="small"
      variant="outlined"
      label={label}
      sx={{
        '& .MuiChip-label': {
          px: 1,
          fontSize: '0.73rem',
          fontWeight: 600,
        },
      }}
    />
  )
}

function formatCardLabel(
  round: Pick<GameHistoryRound, 'cellTitle' | 'cellCost' | 'cellRowIndex' | 'cellColIndex'>,
  t: ReturnType<typeof useTranslation>['t'],
) {
  const title = round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${t('gameHistory.cardCostLabel', { cost: round.cellCost })} · ${t(
    'gameHistory.cardCoordinate',
    {
      row: round.cellRowIndex + 1,
      col: round.cellColIndex + 1,
    },
  )}`
}

function formatShortCardLabel(
  round: Pick<GameHistoryRound, 'cellTitle' | 'cellCost' | 'cellRowIndex' | 'cellColIndex'>,
  t: ReturnType<typeof useTranslation>['t'],
) {
  const title = round.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${round.cellCost}`
}

function formatSignedPoints(value: number, t: ReturnType<typeof useTranslation>['t']) {
  return t('gameHistory.pointsValue', { points: formatSignedNumber(value) })
}

function formatSignedNumber(value: number) {
  return value > 0 ? `+${value}` : `${value}`
}

function formatEnumLabel(value: string) {
  return value.replace(/_/g, ' ')
}

function formatGameTimeLabel(
  game: Pick<GameHistoryGameSummary, 'startedAtUtc' | 'finishedAtUtc' | 'createdAtUtc'>,
  t: ReturnType<typeof useTranslation>['t'],
) {
  if (game.finishedAtUtc) {
    return t('gameHistory.gameTimeFinished', {
      date: formatDateTime(game.finishedAtUtc),
    })
  }

  if (game.startedAtUtc) {
    return t('gameHistory.gameTimeStarted', {
      date: formatDateTime(game.startedAtUtc),
    })
  }

  return t('gameHistory.gameTimeCreated', {
    date: formatDateTime(game.createdAtUtc),
  })
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString()
}

function formatOptionalDateTime(
  value: string | null | undefined,
  t: ReturnType<typeof useTranslation>['t'],
) {
  return value ? formatDateTime(value) : t('gameHistory.notAvailable')
}

function normalizeStatus(status: string) {
  return status.toLowerCase()
}

function normalizeRoundStatus(status: string) {
  return status.toLowerCase().replace(/\s+/g, '_')
}

function getGameStatusColor(status: string): 'default' | 'success' | 'warning' | 'info' {
  switch (normalizeStatus(status)) {
    case 'finished':
      return 'success'
    case 'active':
      return 'warning'
    case 'ready':
      return 'info'
    default:
      return 'default'
  }
}

function getRoundStatusColor(status: string): 'default' | 'success' | 'warning' | 'error' {
  switch (normalizeRoundStatus(status)) {
    case 'completed':
      return 'success'
    case 'cancelled':
    case 'failed':
      return 'error'
    case 'review':
      return 'warning'
    default:
      return 'default'
  }
}

function getRankColor(theme: Theme, rank: number) {
  if (rank === 1) {
    return theme.palette.warning.main
  }

  if (rank === 2) {
    return theme.palette.grey[500]
  }

  if (rank === 3) {
    return theme.palette.secondary.main
  }

  return theme.palette.primary.main
}
