import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import type { components } from '../../shared/api/contracts/generated'
import { formatPlayedCardModifierOutcomeStatus } from '../../shared/lib/played-card-formatters.ts'
import {
  AppButton,
  AsyncSection,
  PageShell,
  ParticipantNamesList,
  PlayedCardPreviewDialog,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import {
  gameHistoryGameDetailsQueryOptions,
  gameHistoryGamesQueryOptions,
} from './api/game-history-queries.ts'
import {
  getRoundBonusDelta,
  getRoundScore,
  getTeamBestScore,
  getTeamFinalScore,
  getTeamTotalBounties,
  getTeamPenaltyTotal,
  getTeamTotalKills,
  sortRoundsByPlaySequence,
  sortTeamLeaderboardEntries,
  type GameHistoryTeamLeaderboardEntry,
} from './model/game-history-team-leaderboard.ts'
import {
  formatCardLabel,
  formatHistoryTeamName,
  formatShortCardLabel,
  getRankColor,
} from './model/game-history-formatters.ts'
import { CompactMetric, MiniMetricChip } from './ui/game-history-display.tsx'
import { CurrentGameLeaderboard } from './ui/CurrentGameLeaderboard.tsx'
import { CancelledRoundsSection } from './ui/CancelledRoundsSection.tsx'
import { GameModifierHistorySummary } from './ui/GameModifierHistorySummary.tsx'

type GameHistoryGameSummary = components['schemas']['GameHistoryGameSummaryDto']
type GameHistoryGameDetails = components['schemas']['GameHistoryGameDetailsDto']
type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']
type GameHistoryBoard = 'realtime' | 'history'

interface GameHistoryPageProps {
  initialBoard?: GameHistoryBoard
  lockedBoard?: GameHistoryBoard
}

export function CurrentGameLeaderboardPage() {
  return <GameHistoryPage initialBoard="realtime" lockedBoard="realtime" />
}

export function GameHistoryPage({
  initialBoard = 'history',
  lockedBoard = 'history',
}: GameHistoryPageProps = {}) {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedGameId = searchParams.get('gameId')
  const [previewRound, setPreviewRound] = useState<GameHistoryRound | null>(null)
  const [activeBoardState, setActiveBoardState] = useState<GameHistoryBoard>(initialBoard)
  const activeBoard = lockedBoard ?? activeBoardState
  const isBoardSwitcherVisible = lockedBoard == null

  const currentGameQuery = useQuery(currentGameBoardQueryOptions)
  const gamesQuery = useQuery(gameHistoryGamesQueryOptions)
  const currentGameId = currentGameQuery.data?.gameId ?? null
  const completedGames = (gamesQuery.data ?? []).filter(
    (game) => normalizeStatus(game.gameStatus) === 'finished',
  )
  const selectedCompletedGameId =
    requestedGameId && completedGames.some((game) => game.gameId === requestedGameId)
      ? requestedGameId
      : (completedGames[0]?.gameId ?? null)

  const currentGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(currentGameId ?? ''),
    enabled: currentGameId !== null,
  })
  const selectedGameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(selectedCompletedGameId ?? ''),
    enabled: selectedCompletedGameId !== null,
  })

  const currentGameLeaderboard = sortTeamLeaderboardEntries(
    currentGameDetailsQuery.data?.mainGame.teamStats ?? [],
  )
  const currentGameDetails = currentGameDetailsQuery.data ?? null
  const currentGamePlayedRounds = (currentGameDetails?.mainGame.rounds ?? []).filter(isCountedRound)
  const currentGameSummary =
    activeBoard === 'realtime' && currentGameQuery.data
      ? {
          title: currentGameDetails?.gameTitle ?? currentGameQuery.data.title,
          status: currentGameDetails?.gameStatus ?? currentGameQuery.data.status,
          playedTeamCount: currentGameDetails ? currentGameLeaderboard.length : null,
          playedRoundCount: currentGameDetails ? currentGamePlayedRounds.length : null,
          activatedModifierCount: currentGameDetails?.mainGame.modifierActivations.length ?? null,
          quizPoints: currentGameDetails?.quiz.totalPoints ?? null,
          totalKills:
            currentGameDetails === null
              ? null
              : currentGamePlayedRounds.reduce(
                  (total, round) => total + round.scoreDetails.totalKillCount,
                  0,
                ),
          totalTokens:
            currentGameDetails === null
              ? null
              : currentGamePlayedRounds.reduce((total, round) => total + round.bountyCount, 0),
          penaltyTotal:
            currentGameDetails === null
              ? null
              : currentGamePlayedRounds.reduce(
                  (total, round) => total + round.scoreDetails.penaltyTotal,
                  0,
                ),
          teamFinalScoreTotal: currentGameDetails
            ? currentGameLeaderboard.reduce((total, entry) => total + getTeamFinalScore(entry), 0)
            : null,
        }
      : null

  return (
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
      }}
    >
      {activeBoard === 'history' ? (
        <SectionHeader
          title={t('gameHistory.archivePageTitle')}
          description={t('gameHistory.archivePageDescription')}
        />
      ) : null}

      {isBoardSwitcherVisible ? (
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
              onClick={() => setActiveBoardState('realtime')}
            />
            <BoardSwitchCard
              title={t('gameHistory.completedGamesTitle')}
              description={t('gameHistory.completedGamesDescription')}
              isActive={activeBoard === 'history'}
              onClick={() => setActiveBoardState('history')}
            />
          </Stack>
        </SectionCard>
      ) : null}

      {activeBoard === 'realtime' ? (
        <>
          {currentGameSummary ? (
            <CurrentGameLeaderboardSummary
              title={currentGameSummary.title}
              status={currentGameSummary.status}
              playedTeamCount={currentGameSummary.playedTeamCount}
              playedRoundCount={currentGameSummary.playedRoundCount}
              activatedModifierCount={currentGameSummary.activatedModifierCount}
              quizPoints={currentGameSummary.quizPoints}
              totalKills={currentGameSummary.totalKills}
              totalTokens={currentGameSummary.totalTokens}
              penaltyTotal={currentGameSummary.penaltyTotal}
              teamFinalScoreTotal={currentGameSummary.teamFinalScoreTotal}
            />
          ) : null}

          <SectionCard sx={{ mt: currentGameSummary ? 1.5 : 0 }}>
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
        </>
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
                    onClick={() => {
                      setSearchParams({ gameId: game.gameId }, { replace: true })
                    }}
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

function CurrentGameLeaderboardSummary({
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

function GameSummaryButton({
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

function GameDetailsPanel({
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

      <GameModifierHistorySummary rounds={completedRounds} />

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

function FinalResultSnapshot({
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
  const modifierScoreDelta = round.scoreDetails.modifierScoreDelta

  return (
    <AccordionSurface defaultExpanded={isBestRound} highlighted={isBestRound}>
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
                <Chip
                  size="small"
                  variant="outlined"
                  label={t('gameHistory.pointsValue', { points: getRoundScore(round) })}
                />
              </Stack>
            </Box>

            <Stack direction="row" spacing={0.55} flexWrap="wrap" useFlexGap>
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
              <ParticipantNamesList
                names={participants.map((participant) => participant.displayName)}
                emptyLabel={t('gameHistory.noParticipants')}
              />
            </Box>

            <AppButton
              size="small"
              tone="secondary"
              onClick={() => onPreviewCard(round)}
              sx={{ flexShrink: 0 }}
            >
              {t('common.actions.openCard')}
            </AppButton>
          </Stack>

          {modifiers.length > 0 ? (
            <CollapsibleSection
              title={t('common.entities.modifiers')}
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
                        <MiniMetricChip
                          label={formatPlayedCardModifierOutcomeStatus(t, modifier.outcomeStatus)}
                        />
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
  const { t, i18n } = useTranslation()
  const participants = round.participants ?? []
  const modifiers = round.modifiers ?? []
  const modifierScoreDelta = round.scoreDetails.modifierScoreDelta

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
                  {formatHistoryTeamName(t, round.teamName, round.teamSlotIndex)}
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
            {formatOptionalDateTime(
              round.finishedAtUtc ?? round.startedAtUtc,
              t,
              i18n.resolvedLanguage,
            )}
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
              <ParticipantNamesList
                names={participants.map((participant) => participant.displayName)}
                emptyLabel={t('gameHistory.noParticipants')}
              />
            </Box>

            <AppButton
              size="small"
              tone="secondary"
              onClick={() => onPreviewCard(round)}
              sx={{ flexShrink: 0 }}
            >
              {t('common.actions.openCard')}
            </AppButton>
          </Stack>

          {modifiers.length > 0 ? (
            <CollapsibleSection
              title={t('common.entities.modifiers')}
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
  return <PlayedCardPreviewDialog card={null} round={round} onClose={onClose} />
}

function AccordionSurface({
  children,
  defaultExpanded = false,
  highlighted = false,
}: {
  children: ReactNode
  defaultExpanded?: boolean
  highlighted?: boolean
}) {
  return (
    <Accordion
      disableGutters
      elevation={0}
      defaultExpanded={defaultExpanded}
      sx={(theme) => ({
        borderRadius: 2.5,
        border: `1px solid ${
          highlighted ? alpha(theme.palette.warning.main, 0.72) : alpha(theme.palette.divider, 0.88)
        }`,
        backgroundColor: highlighted
          ? alpha(theme.palette.warning.main, 0.08)
          : alpha(theme.palette.background.paper, 0.58),
        boxShadow: highlighted
          ? `inset 0 0 0 1px ${alpha(theme.palette.warning.main, 0.42)}`
          : 'none',
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

function formatSignedNumber(value: number) {
  return value > 0 ? `+${value}` : `${value}`
}

function formatGameTimeLabel(
  game: Pick<GameHistoryGameSummary, 'startedAtUtc' | 'finishedAtUtc' | 'createdAtUtc'>,
  t: ReturnType<typeof useTranslation>['t'],
  locale?: string,
) {
  if (game.finishedAtUtc) {
    return t('gameHistory.gameTimeFinished', {
      date: formatDateTime(game.finishedAtUtc, locale),
    })
  }

  if (game.startedAtUtc) {
    return t('gameHistory.gameTimeStarted', {
      date: formatDateTime(game.startedAtUtc, locale),
    })
  }

  return t('gameHistory.gameTimeCreated', {
    date: formatDateTime(game.createdAtUtc, locale),
  })
}

function formatDateTime(value: string, locale?: string) {
  return new Date(value).toLocaleString(locale)
}

function formatOptionalDateTime(
  value: string | null | undefined,
  t: ReturnType<typeof useTranslation>['t'],
  locale?: string,
) {
  return value ? formatDateTime(value, locale) : t('gameHistory.notAvailable')
}

function normalizeStatus(status: string) {
  return status.toLowerCase()
}

function normalizeRoundStatus(status: string) {
  return status.toLowerCase().replace(/\s+/g, '_')
}

function isCountedRound(round: GameHistoryRound) {
  return normalizeRoundStatus(round.status) === 'completed'
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
