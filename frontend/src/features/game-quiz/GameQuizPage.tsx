import { Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { gameHistoryGameDetailsQueryOptions } from '../game-history/api/game-history-queries.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AsyncSection, PageShell, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import type { components } from '../../shared/api/contracts/generated'

type QuestionRound = components['schemas']['GameHistoryQuizRoundItemDto']
type LeaderboardEntry = components['schemas']['GameHistoryPlayerSummaryDto']
type RoundStatus = QuestionRound['status']

export function GameQuizPage() {
  const { t } = useTranslation()
  const { user } = useAuth()

  const snapshotQuery = useQuery(currentGameBoardQueryOptions)
  const gameId = snapshotQuery.data?.gameId ?? ''

  const gameDetailsQuery = useQuery({
    ...gameHistoryGameDetailsQueryOptions(gameId),
    enabled: gameId !== '',
  })

  const isLoading =
    snapshotQuery.isLoading || (snapshotQuery.data != null && gameDetailsQuery.isLoading)
  const isError = snapshotQuery.isError || gameDetailsQuery.isError
  const snapshot = snapshotQuery.data ?? null
  const leaderboard: LeaderboardEntry[] = gameDetailsQuery.data?.quiz.playerStats ?? []
  const rounds: QuestionRound[] = gameDetailsQuery.data?.quiz.rounds ?? []
  const isEmpty = !isLoading && !isError && snapshot == null

  function statusColor(status: RoundStatus): 'default' | 'success' | 'error' | 'warning' {
    switch (status) {
      case 'answered_correct':
        return 'success'
      case 'answered_wrong':
        return 'error'
      case 'timeout':
      case 'skipped':
        return 'warning'
      default:
        return 'default'
    }
  }

  function statusLabel(status: RoundStatus): string {
    switch (status) {
      case 'asked':
        return t('gameQuiz.statusAsked')
      case 'answered_correct':
        return t('gameQuiz.statusAnsweredCorrect')
      case 'answered_wrong':
        return t('gameQuiz.statusAnsweredWrong')
      case 'timeout':
        return t('gameQuiz.statusTimeout')
      case 'skipped':
        return t('gameQuiz.statusSkipped')
    }
  }

  return (
    <PageShell>
      <SectionHeader title={t('gameQuiz.title')} />

      <AsyncSection
        isLoading={isLoading}
        isError={isError}
        isEmpty={isEmpty}
        loadingMessage={t('gameQuiz.loading')}
        errorMessage={t('gameQuiz.errorLoading')}
        emptyMessage={t('gameQuiz.noGame')}
      >
        <Stack spacing={3} sx={{ mt: 1 }}>
          {/* Leaderboard */}
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
              {t('gameQuiz.leaderboardTitle')}
            </Typography>
            <SectionCard sx={{ p: 0, overflow: 'hidden' }}>
              {leaderboard.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
                  {t('gameQuiz.noLeaderboardEntries')}
                </Typography>
              ) : (
                leaderboard.map((entry, index) => (
                  <Box key={entry.userId}>
                    {index > 0 ? <Divider /> : null}
                    <Stack
                      direction="row"
                      alignItems="center"
                      spacing={1.5}
                      sx={(theme) => ({
                        px: 2,
                        py: 1.25,
                        backgroundColor:
                          entry.userId === user?.id
                            ? alpha(theme.palette.primary.main, 0.06)
                            : 'transparent',
                      })}
                    >
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ minWidth: 28, fontWeight: 700 }}
                      >
                        {t('gameQuiz.rank', { rank: index + 1 })}
                      </Typography>
                      <Typography
                        variant="body2"
                        fontWeight={entry.userId === user?.id ? 700 : 400}
                        sx={{ flex: 1 }}
                      >
                        {entry.displayName}
                      </Typography>
                      <Typography variant="body2" fontWeight={700} color="primary.main">
                        {t('gameQuiz.totalPoints', { points: entry.points })}
                      </Typography>
                    </Stack>
                  </Box>
                ))
              )}
            </SectionCard>
          </Box>

          {/* Question history */}
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
              {t('gameQuiz.historyTitle')}
            </Typography>
            {gameDetailsQuery.isLoading ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameQuiz.loading')}
              </Typography>
            ) : rounds.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameQuiz.noHistory')}
              </Typography>
            ) : (
              <Stack spacing={1.5}>
                {rounds.map((round) => {
                  const isMyAnswer =
                    user != null &&
                    (round.answeredByUserId === user.id || round.answeredForUserId === user.id)

                  return (
                    <SectionCard
                      key={round.roundId}
                      sx={(theme) => ({
                        borderColor: isMyAnswer
                          ? alpha(theme.palette.primary.main, 0.4)
                          : undefined,
                      })}
                    >
                      <Stack spacing={1}>
                        <Stack
                          direction="row"
                          spacing={1}
                          alignItems="center"
                          flexWrap="wrap"
                          useFlexGap
                        >
                          <Typography
                            variant="caption"
                            color="text.secondary"
                            sx={{ fontWeight: 700, whiteSpace: 'nowrap' }}
                          >
                            {t('gameQuiz.questionLabel', { order: round.askOrder })}
                          </Typography>
                          <Chip
                            label={statusLabel(round.status)}
                            color={statusColor(round.status)}
                            size="small"
                            sx={{ height: 20, fontSize: '0.68rem' }}
                          />
                          <Chip
                            label={t('gameQuiz.categoryLabel', { category: round.categoryName })}
                            size="small"
                            variant="outlined"
                            sx={{ height: 20, fontSize: '0.68rem' }}
                          />
                          <Chip
                            label={t('gameQuiz.rewardLabel', { reward: round.reward })}
                            size="small"
                            variant="outlined"
                            sx={{ height: 20, fontSize: '0.68rem' }}
                          />
                        </Stack>

                        <Typography variant="body2">{round.questionText}</Typography>

                        <Stack direction="row" spacing={2} alignItems="center">
                          {round.answeredByDisplayName != null ? (
                            <Typography variant="caption" color="text.secondary">
                              {t('gameQuiz.answeredBy', { name: round.answeredByDisplayName })}
                            </Typography>
                          ) : (
                            <Typography variant="caption" color="text.secondary">
                              {t('gameQuiz.notAnswered')}
                            </Typography>
                          )}
                          {round.awardedPoints != null && round.awardedPoints > 0 ? (
                            <Typography variant="caption" color="success.main" fontWeight={700}>
                              {t('gameQuiz.pointsEarned', { points: round.awardedPoints })}
                            </Typography>
                          ) : null}
                        </Stack>
                      </Stack>
                    </SectionCard>
                  )
                })}
              </Stack>
            )}
          </Box>
        </Stack>
      </AsyncSection>
    </PageShell>
  )
}
