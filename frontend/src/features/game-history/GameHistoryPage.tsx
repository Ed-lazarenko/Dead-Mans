import { Box, Stack } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import type { components } from '../../shared/api/contracts/generated'
import { AsyncSection, PageShell, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import {
  gameHistoryGameDetailsQueryOptions,
  gameHistoryGamesQueryOptions,
} from './api/game-history-queries.ts'
import {
  getTeamFinalScore,
  sortTeamLeaderboardEntries,
} from './model/game-history-team-leaderboard.ts'
import { isCountedRound, normalizeStatus } from './model/game-history-view.ts'
import { CurrentGameLeaderboard } from './ui/CurrentGameLeaderboard.tsx'
import { GameDetailsPanel } from './ui/GameHistoryDetailsPanel.tsx'
import {
  BoardSwitchCard,
  CurrentGameLeaderboardSummary,
  GameSummaryButton,
} from './ui/GameHistoryOverview.tsx'
import { CardPreviewDialog } from './ui/game-history-surfaces.tsx'

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
