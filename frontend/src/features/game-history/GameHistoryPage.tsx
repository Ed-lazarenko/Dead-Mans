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
import { useEffect, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../shared/api/contracts/generated'
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
  getCardRunBonusDelta,
  getCardRunScore,
  type GameHistoryTeamLeaderboardEntry,
} from './model/game-history-team-leaderboard.ts'

type GameHistoryGameSummary = components['schemas']['GameHistoryGameSummaryDto']
type GameHistoryGameDetails = components['schemas']['GameHistoryGameDetailsDto']
type GameHistoryCardRun = components['schemas']['GameHistoryCardRunItemDto']

export function GameHistoryPage() {
  const { t } = useTranslation()
  const [selectedGameId, setSelectedGameId] = useState<string | null>(null)
  const [previewCardRun, setPreviewCardRun] = useState<GameHistoryCardRun | null>(null)
  const [activeBoard, setActiveBoard] = useState<'realtime' | 'history'>('realtime')

  const currentGameQuery = useQuery(currentGameBoardQueryOptions)
  const gamesQuery = useQuery(gameHistoryGamesQueryOptions)
  const currentGameId = currentGameQuery.data?.gameId ?? null
  const completedGames = (gamesQuery.data ?? []).filter(
    (game) => normalizeStatus(game.gameStatus) === 'finished',
  )

  useEffect(() => {
    const availableCompletedGames = (gamesQuery.data ?? []).filter(
      (game) => normalizeStatus(game.gameStatus) === 'finished',
    )

    if (availableCompletedGames.length === 0) {
      if (selectedGameId !== null) {
        setSelectedGameId(null)
      }
      return
    }

    const hasSelectedGame = selectedGameId
      ? availableCompletedGames.some((game) => game.gameId === selectedGameId)
      : false
    if (hasSelectedGame) {
      return
    }

    setSelectedGameId(availableCompletedGames[0]!.gameId)
  }, [gamesQuery.data, selectedGameId])

  const currentGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(currentGameId ?? ''),
    enabled: currentGameId !== null,
  })
  const selectedGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(selectedGameId ?? ''),
    enabled: selectedGameId !== null,
  })

  const currentGameLeaderboard = buildGameTeamLeaderboard(
    currentGameDetailsQuery.data?.mainGame.cardRuns ?? [],
  )

  return (
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
      }}
    >
      <SectionHeader
        title={t('gameHistory.title')}
        description={t('gameHistory.description')}
      />

      <SectionCard sx={{ mt: 1.5 }}>
        <SectionHeader
          title={t('gameHistory.boardSwitcherTitle')}
          description={t('gameHistory.boardSwitcherDescription')}
        />

        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={1.25}
          sx={{ mt: 1.5 }}
        >
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
              onPreviewCard={setPreviewCardRun}
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
                    isSelected={game.gameId === selectedGameId}
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
              isLoading={selectedGameId !== null && selectedGameDetailsQuery.isLoading}
              isError={selectedGameDetailsQuery.isError}
              isEmpty={selectedGameId === null}
              loadingMessage={t('gameHistory.loadingGameDetails')}
              errorMessage={t('gameHistory.errorGameDetails')}
              emptyMessage={t('gameHistory.completedGameSelectPrompt')}
            >
              <GameDetailsPanel
                game={selectedGameDetailsQuery.data ?? null}
                onPreviewCard={setPreviewCardRun}
              />
            </AsyncSection>
          </SectionCard>
        </Box>
      )}

      <CardPreviewDialog cardRun={previewCardRun} onClose={() => setPreviewCardRun(null)} />
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
  onPreviewCard: (cardRun: GameHistoryCardRun) => void
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
          borderRadius: 3,
          border: `1px solid ${alpha(theme.palette.warning.main, 0.42)}`,
          background: `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.18)} 0%, ${alpha(
            theme.palette.success.main,
            0.14,
          )} 45%, ${alpha(theme.palette.info.main, 0.18)} 100%)`,
          px: { xs: 2, sm: 2.5 },
          py: { xs: 2, sm: 2.25 },
          boxShadow: `0 20px 44px ${alpha(theme.palette.common.black, 0.22)}`,
        })}
      >
        <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} alignItems="stretch">
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
              <Chip label={t('gameHistory.statusChipCurrentGame')} color="warning" />
              <Chip
                label={t(`gameHistory.status.${normalizeStatus(gameDetails.gameStatus)}`)}
                color={getGameStatusColor(gameDetails.gameStatus)}
                variant="outlined"
              />
            </Stack>

            <Typography variant="h5" sx={{ mt: 1.25, fontWeight: 800 }}>
              {gameDetails.gameTitle}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
              {t('gameHistory.currentLeaderboardRule')}
            </Typography>

            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 1.5 }}>
              <MetricChip
                label={t('gameHistory.summary.runCount')}
                value={t('gameHistory.countValue', {
                  count: gameDetails.mainGame.cardRuns.length,
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
              width: { xs: '100%', lg: 320 },
              borderRadius: 2.5,
              border: `1px solid ${alpha(theme.palette.common.white, 0.1)}`,
              backgroundColor: alpha(theme.palette.common.black, 0.16),
              p: 2,
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
                  value={formatCardLabel(topEntry.bestRun, t)}
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
        transition:
          'border-color 0.15s ease, background-color 0.15s ease, transform 0.15s ease',
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
            label={t('gameHistory.summary.runCountShort', {
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
  onPreviewCard: (cardRun: GameHistoryCardRun) => void
}) {
  const { t } = useTranslation()

  if (!game) {
    return null
  }

  const leaderboard = buildGameTeamLeaderboard(game.mainGame.cardRuns)
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
                <Chip label={t('gameHistory.statusChipArchived')} color="default" variant="outlined" />
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
              value={t('gameHistory.countValue', { count: game.mainGame.cardRuns.length })}
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
              {t('gameHistory.summary.noRuns')}
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
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} alignItems="flex-start">
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
          countLabel={t('gameHistory.summary.runCountShort', {
            count: game.mainGame.cardRuns.length,
          })}
        >
          {game.mainGame.cardRuns.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.summary.noRuns')}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {game.mainGame.cardRuns.map((run) => (
                <CardRunHistoryRow
                  key={run.cardRunId}
                  run={run}
                  onPreviewCard={onPreviewCard}
                />
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
  onPreviewCard: (cardRun: GameHistoryCardRun) => void
}) {
  const { t } = useTranslation()
  const recentRuns = entry.runs.slice(0, 3)

  return (
    <AccordionSurface defaultExpanded={rank <= 3}>
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
          <Stack spacing={1.25}>
            <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5} alignItems="flex-start">
              <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0, flex: 1 }}>
                <Box
                  sx={(theme) => ({
                    width: 38,
                    height: 38,
                    borderRadius: '50%',
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
                  <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
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
                      label={t('gameHistory.summary.bestCardShort')}
                    />
                  </Stack>

                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.45 }}>
                    {formatCardLabel(entry.bestRun, t)}
                  </Typography>
                </Box>
              </Stack>

              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                <MiniMetricChip
                  label={t('gameHistory.summary.bestScoreShort', { points: entry.bestScore })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.averageScoreShort', {
                    points: entry.averageScore,
                  })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.totalScoreShort', { points: entry.totalScore })}
                />
                <MiniMetricChip
                  label={t('gameHistory.summary.roundsPlayedShort', {
                    count: entry.roundsPlayed,
                  })}
                />
              </Stack>
            </Stack>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              {(entry.participantNames.length > 0
                ? entry.participantNames
                : [t('gameHistory.noParticipants')]).map((name) => (
                <Chip key={name} size="small" variant="outlined" label={name} />
              ))}
            </Stack>

            <Box
              sx={(theme) => ({
                display: 'grid',
                gap: 0.8,
                gridTemplateColumns: {
                  xs: 'repeat(2, minmax(0, 1fr))',
                  lg: 'repeat(5, minmax(0, 1fr))',
                },
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.divider, 0.78)}`,
                backgroundColor: alpha(theme.palette.background.paper, 0.46),
                p: 1,
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
                  points: getCardRunScore(entry.latestRun),
                })}
              />
              <MetricChip
                label={t('gameHistory.summary.runCount')}
                value={t('gameHistory.countValue', { count: entry.roundsPlayed })}
              />
            </Box>

            <Stack spacing={0.6}>
              <Typography variant="caption" color="text.secondary">
                {t('gameHistory.summary.recentRuns')}
              </Typography>
              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                {recentRuns.map((run) => (
                  <RecentRunPill
                    key={run.cardRunId}
                    run={run}
                    isBestRun={run.cardRunId === entry.bestRun.cardRunId}
                  />
                ))}
              </Stack>
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.75, pt: 0, pb: 1.75 }}>
        <Stack spacing={1.25}>
          <MetricRow
            label={t('gameHistory.summary.bestCard')}
            value={formatCardLabel(entry.bestRun, t)}
          />
          <MetricRow
            label={t('gameHistory.summary.bestScore')}
            value={t('gameHistory.pointsValue', { points: entry.bestScore })}
          />
          <MetricRow
            label={t('gameHistory.summary.averageScore')}
            value={t('gameHistory.pointsValue', { points: entry.averageScore })}
          />
          <MetricRow
            label={t('gameHistory.summary.bonusDelta')}
            value={formatSignedPoints(getCardRunBonusDelta(entry.bestRun), t)}
          />
          <MetricRow
            label={t('gameHistory.summary.totalScore')}
            value={t('gameHistory.pointsValue', { points: entry.totalScore })}
          />
          <MetricRow
            label={t('gameHistory.summary.totalKills')}
            value={t('gameHistory.countValue', { count: entry.totalKills })}
          />
          <MetricRow
            label={t('gameHistory.summary.totalBounties')}
            value={t('gameHistory.countValue', { count: entry.totalBounties })}
          />
          <MetricRow
            label={t('gameHistory.summary.latestResult')}
            value={t('gameHistory.pointsValue', {
              points: getCardRunScore(entry.latestRun),
            })}
          />

          <CollapsibleSection
            title={t('gameHistory.summary.allRunsTitle')}
            description={t('gameHistory.summary.allRunsDescription')}
            countLabel={t('gameHistory.summary.runCountShort', {
              count: entry.runs.length,
            })}
            nested
            defaultExpanded={rank === 1}
          >
            <Stack spacing={1}>
              {entry.runs.map((run) => (
                <LeaderboardRunCard
                  key={run.cardRunId}
                  run={run}
                  isBestRun={run.cardRunId === entry.bestRun.cardRunId}
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

function RecentRunPill({
  run,
  isBestRun,
}: {
  run: GameHistoryCardRun
  isBestRun: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 1.75,
        border: `1px solid ${
          isBestRun
            ? alpha(theme.palette.warning.main, 0.52)
            : alpha(theme.palette.divider, 0.82)
        }`,
        background: isBestRun
          ? `linear-gradient(135deg, ${alpha(theme.palette.warning.main, 0.15)}, ${alpha(
              theme.palette.background.paper,
              0.66,
            )})`
          : alpha(theme.palette.background.paper, 0.54),
        px: 1,
        py: 0.8,
      })}
    >
      <Stack spacing={0.35}>
        <Stack direction="row" spacing={0.6} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="caption" sx={{ fontWeight: 800 }}>
            {t('gameHistory.pointsValue', { points: getCardRunScore(run) })}
          </Typography>
          {isBestRun ? <MiniMetricChip label={t('gameHistory.summary.bestRunChip')} /> : null}
        </Stack>
        <Typography variant="caption" color="text.secondary">
          {run.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('gameHistory.summary.killsShort', { count: run.killsCount })} ·{' '}
          {t('gameHistory.summary.bountiesShort', { count: run.bountyCount })}
        </Typography>
      </Stack>
    </Box>
  )
}

function LeaderboardRunCard({
  run,
  isBestRun,
  onPreviewCard,
}: {
  run: GameHistoryCardRun
  isBestRun: boolean
  onPreviewCard: (cardRun: GameHistoryCardRun) => void
}) {
  const { t } = useTranslation()
  const participants = run.participants ?? []
  const modifiers = run.modifiers ?? []
  const modifierScoreDelta = modifiers.reduce((sum, modifier) => sum + modifier.scoreDelta, 0)

  return (
    <AccordionSurface defaultExpanded={isBestRun}>
      <AccordionSummary
        expandIcon={<ExpandGlyph />}
        sx={{
          px: 1.5,
          py: 0.2,
          '& .MuiAccordionSummary-content': {
            my: 0.9,
          },
        }}
      >
        <Box sx={{ width: '100%' }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} alignItems="flex-start">
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="body2" sx={{ fontWeight: 800 }}>
                  {run.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
                </Typography>
                {isBestRun ? (
                  <Chip size="small" color="warning" label={t('gameHistory.summary.bestRunChip')} />
                ) : null}
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.pointsValue', { points: getCardRunScore(run) })}
                />
              </Stack>

              <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
                {formatCardLabel(run, t)}
              </Typography>
            </Box>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <MiniMetricChip label={t('gameHistory.summary.killsShort', { count: run.killsCount })} />
              <MiniMetricChip
                label={t('gameHistory.summary.bountiesShort', { count: run.bountyCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bonusShort', {
                  value: formatSignedNumber(getCardRunBonusDelta(run)),
                })}
              />
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.5, pt: 0, pb: 1.5 }}>
        <Stack spacing={1.2}>
          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('gameHistory.cardDescriptionLabel')}
            </Typography>
            <Typography variant="body2" sx={{ mt: 0.55, whiteSpace: 'pre-line' }}>
              {run.cellDescription || t('gameHistory.cardDescriptionEmpty')}
            </Typography>
          </Box>

          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <MiniMetricChip
              label={t('gameHistory.summary.baseScoreShort', {
                points: run.baseScore,
              })}
            />
            <MiniMetricChip
              label={t('gameHistory.summary.finalScoreShort', {
                points: getCardRunScore(run),
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
              onClick={() => onPreviewCard(run)}
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
                      borderRadius: 1.5,
                      border: `1px solid ${alpha(theme.palette.divider, 0.86)}`,
                      px: 1,
                      py: 0.9,
                    })}
                  >
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={0.75} alignItems="flex-start">
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

          {run.notes ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.notesLabel', { notes: run.notes })}
            </Typography>
          ) : null}
        </Stack>
      </AccordionDetails>
    </AccordionSurface>
  )
}

function CardRunHistoryRow({
  run,
  onPreviewCard,
}: {
  run: GameHistoryCardRun
  onPreviewCard: (cardRun: GameHistoryCardRun) => void
}) {
  const { t } = useTranslation()
  const participants = run.participants ?? []
  const modifiers = run.modifiers ?? []
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
                  {t('gameHistory.teamLabel', { slot: run.teamSlotIndex })}
                </Typography>
                <Chip
                  size="small"
                  label={t(`gameHistory.runStatus.${normalizeRunStatus(run.status)}`)}
                  color={getRunStatusColor(run.status)}
                />
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.pointsValue', { points: getCardRunScore(run) })}
                />
              </Stack>

              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.55 }}>
                {formatCardLabel(run, t)}
              </Typography>
            </Box>

            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <MiniMetricChip label={t('gameHistory.summary.killsShort', { count: run.killsCount })} />
              <MiniMetricChip
                label={t('gameHistory.summary.bountiesShort', { count: run.bountyCount })}
              />
              <MiniMetricChip
                label={t('gameHistory.summary.bonusShort', {
                  value: formatSignedNumber(getCardRunBonusDelta(run)),
                })}
              />
            </Stack>
          </Stack>
        </Box>
      </AccordionSummary>

      <AccordionDetails sx={{ px: 1.75, pt: 0, pb: 1.75 }}>
        <Stack spacing={1.2}>
          <Typography variant="caption" color="text.secondary">
            {formatOptionalDateTime(run.finishedAtUtc ?? run.startedAtUtc, t)}
          </Typography>

          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <MiniMetricChip
              label={t('gameHistory.summary.baseScoreShort', {
                points: run.baseScore,
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
              onClick={() => onPreviewCard(run)}
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

          {run.notes ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameHistory.notesLabel', { notes: run.notes })}
            </Typography>
          ) : null}
        </Stack>
      </AccordionDetails>
    </AccordionSurface>
  )
}

function CardPreviewDialog({
  cardRun,
  onClose,
}: {
  cardRun: GameHistoryCardRun | null
  onClose: () => void
}) {
  const { t } = useTranslation()
  const cellMedia = cardRun?.cellMedia ?? []

  return (
    <AppDialog
      open={cardRun !== null}
      onClose={onClose}
      maxWidth="md"
      title={
        cardRun
          ? cardRun.cellTitle || t('gameHistory.cardDialogFallbackTitle')
          : t('gameHistory.cardDialogFallbackTitle')
      }
      description={
        cardRun
          ? t('gameHistory.cardDialogDescription', {
              slot: cardRun.teamSlotIndex,
            })
          : undefined
      }
      actions={
        <AppButton tone="secondary" onClick={onClose}>
          {t('gameHistory.closeAction')}
        </AppButton>
      }
    >
      {cardRun ? (
        <Stack spacing={2}>
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            <Chip label={t('gameHistory.teamLabel', { slot: cardRun.teamSlotIndex })} />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardCoordinate', {
                row: cardRun.cellRowIndex + 1,
                col: cardRun.cellColIndex + 1,
              })}
            />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardCostLabel', { cost: cardRun.cellCost })}
            />
            <Chip
              variant="outlined"
              label={t('gameHistory.cardTypeLabel', { type: cardRun.cellType })}
            />
          </Stack>

          <Box>
            <Typography variant="caption" color="text.secondary">
              {t('gameHistory.cardDescriptionLabel')}
            </Typography>
            <Typography variant="body2" sx={{ mt: 0.7, whiteSpace: 'pre-line' }}>
              {cardRun.cellDescription || t('gameHistory.cardDescriptionEmpty')}
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
                    src={media.url}
                    alt={cardRun.cellTitle || t('gameHistory.cardDialogFallbackTitle')}
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

function MetricChip({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <Box
      sx={(theme) => ({
        borderRadius: 999,
        border: `1px solid ${alpha(theme.palette.divider, 0.88)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.54),
        px: 1.2,
        py: 0.85,
      })}
    >
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 700 }}>
        {value}
      </Typography>
    </Box>
  )
}

function MetricRow({
  label,
  value,
}: {
  label: string
  value: string
}) {
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
  run: Pick<GameHistoryCardRun, 'cellTitle' | 'cellCost' | 'cellRowIndex' | 'cellColIndex'>,
  t: ReturnType<typeof useTranslation>['t'],
) {
  const title = run.cellTitle || t('gameHistory.cardDialogFallbackTitle')
  return `${title} · ${t('gameHistory.cardCostLabel', { cost: run.cellCost })} · ${t(
    'gameHistory.cardCoordinate',
    {
      row: run.cellRowIndex + 1,
      col: run.cellColIndex + 1,
    },
  )}`
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

function normalizeRunStatus(status: string) {
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

function getRunStatusColor(status: string): 'default' | 'success' | 'warning' | 'error' {
  switch (normalizeRunStatus(status)) {
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
