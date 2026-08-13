import { Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import type { components } from '../../shared/api/contracts/generated'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AsyncSection, PageShell, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import { gameHistoryGameDetailsQueryOptions } from '../game-history/api/game-history-queries.ts'
import { getQuizRoundParticipantDetails } from './model/quiz-round-participants.ts'

type QuizRound = components['schemas']['GameHistoryQuizRoundItemDto']
type ManualAward = components['schemas']['GameHistoryQuizManualAwardItemDto']
type RoundStatus = QuizRound['status']

type QuizHistoryItem =
  | {
      id: string
      kind: 'round'
      sortAtUtc: string
      round: QuizRound
    }
  | {
      id: string
      kind: 'manualAward'
      sortAtUtc: string
      award: ManualAward
    }

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
  const leaderboard =
    gameDetailsQuery.data?.quiz.playerStats.filter((entry) => entry.points > 0) ?? []
  const historyItems = getHistoryItems(
    gameDetailsQuery.data?.quiz.rounds ?? [],
    gameDetailsQuery.data?.quiz.manualAwards ?? [],
  )
  const isEmpty = !isLoading && !isError && snapshot == null

  return (
    <PageShell
      sx={{
        maxWidth: 'none',
        width: '100%',
        mx: 0,
        px: { xs: 0, sm: 0 },
      }}
    >
      <SectionHeader title={t('gameQuiz.title')} />

      <AsyncSection
        isLoading={isLoading}
        isError={isError}
        isEmpty={isEmpty}
        loadingMessage={t('gameQuiz.loading')}
        errorMessage={t('gameQuiz.errorLoading')}
        emptyMessage={t('gameQuiz.noGame')}
      >
        <Stack
          direction={{ xs: 'column', lg: 'row' }}
          spacing={2}
          alignItems="stretch"
          sx={{ mt: 1 }}
        >
          <SectionCard
            sx={{
              width: { xs: '100%', lg: 420, xl: 460 },
              minWidth: { lg: 420, xl: 460 },
              flexShrink: 0,
              p: 0,
            }}
          >
            <Stack spacing={0}>
              <Box sx={{ px: 2, pt: 2, pb: 1.25 }}>
                <Typography variant="overline" color="text.secondary">
                  {t('gameQuiz.leaderboardTitle')}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('gameQuiz.leaderboardDescription')}
                </Typography>
              </Box>

              {leaderboard.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ px: 2, pb: 2 }}>
                  {t('gameQuiz.noLeaderboardEntries')}
                </Typography>
              ) : (
                leaderboard.map((entry, index) => (
                  <Box key={entry.userId}>
                    {index > 0 ? <Divider /> : null}
                    <Stack
                      direction="row"
                      spacing={1.25}
                      alignItems="center"
                      sx={(theme) => ({
                        px: 2,
                        py: 1.25,
                        backgroundColor:
                          entry.userId === user?.id
                            ? alpha(theme.palette.primary.main, 0.08)
                            : 'transparent',
                      })}
                    >
                      <Typography
                        variant="caption"
                        color="text.secondary"
                        sx={{ minWidth: 26, fontWeight: 700 }}
                      >
                        {index + 1}
                      </Typography>

                      <Box sx={{ minWidth: 0, flex: 1 }}>
                        <Typography
                          variant="body2"
                          fontWeight={entry.userId === user?.id ? 700 : 500}
                          noWrap
                        >
                          {entry.displayName}
                        </Typography>
                        {entry.lastActivityAtUtc ? (
                          <Typography variant="caption" color="text.secondary" noWrap>
                            {t('gameQuiz.lastActivityAt', {
                              time: new Date(entry.lastActivityAtUtc).toLocaleTimeString(),
                            })}
                          </Typography>
                        ) : null}
                      </Box>

                      <Typography variant="body2" fontWeight={700} color="primary.main">
                        {t('gameQuiz.totalPoints', { points: entry.points })}
                      </Typography>
                    </Stack>
                  </Box>
                ))
              )}
            </Stack>
          </SectionCard>

          <SectionCard sx={{ flex: 1, minWidth: 0, p: 0 }}>
            <Stack spacing={0}>
              <Box sx={{ px: 2, pt: 2, pb: 1.25 }}>
                <Typography variant="overline" color="text.secondary">
                  {t('gameQuiz.historyTitle')}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('gameQuiz.historyDescription')}
                </Typography>
              </Box>

              {historyItems.length === 0 ? (
                <Typography variant="body2" color="text.secondary" sx={{ px: 2, pb: 2 }}>
                  {t('gameQuiz.noHistory')}
                </Typography>
              ) : (
                <Stack spacing={0}>
                  {historyItems.map((item, index) => (
                    <Box key={item.id}>
                      {index > 0 ? <Divider /> : null}
                      {item.kind === 'round' ? (
                        <QuizRoundHistoryItem round={item.round} currentUserId={user?.id ?? null} />
                      ) : (
                        <ManualAwardHistoryItem
                          award={item.award}
                          currentUserId={user?.id ?? null}
                        />
                      )}
                    </Box>
                  ))}
                </Stack>
              )}
            </Stack>
          </SectionCard>
        </Stack>
      </AsyncSection>
    </PageShell>
  )
}

function QuizRoundHistoryItem({
  round,
  currentUserId,
}: {
  round: QuizRound
  currentUserId: string | null
}) {
  const { t } = useTranslation()
  const participantDetails = getQuizRoundParticipantDetails(round)
  const isMyAnswer =
    currentUserId != null &&
    (round.answeredByUserId === currentUserId || round.answeredForUserId === currentUserId)

  return (
    <Box
      sx={(theme) => ({
        px: 2,
        py: 1.5,
        backgroundColor: isMyAnswer ? alpha(theme.palette.primary.main, 0.05) : 'transparent',
      })}
    >
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
            {t('gameQuiz.questionLabel', { order: round.askOrder })}
          </Typography>
          <Chip
            label={getStatusLabel(round.status, t)}
            color={getStatusColor(round.status)}
            size="small"
            sx={{ height: 20, fontSize: '0.68rem' }}
          />
          <Chip
            label={t('gameQuiz.rewardLabel', { reward: round.reward })}
            size="small"
            variant="outlined"
            sx={{ height: 20, fontSize: '0.68rem' }}
          />
          <Chip
            label={t('gameQuiz.categoryLabel', { category: round.categoryName })}
            size="small"
            variant="outlined"
            sx={{ height: 20, fontSize: '0.68rem' }}
          />
          <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
            {formatHistoryTime(round.answeredAtUtc ?? round.askedAtUtc)}
          </Typography>
        </Stack>

        <Typography variant="body2" fontWeight={600}>
          {round.questionText}
        </Typography>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
          <Typography variant="caption" color="text.secondary">
            {participantDetails.answeredByDisplayName != null
              ? t('gameQuiz.answeredBy', { name: participantDetails.answeredByDisplayName })
              : t('gameQuiz.notAnswered')}
          </Typography>
          {participantDetails.answeredForDisplayName != null ? (
            <Typography variant="caption" color="info.main" fontWeight={700}>
              {t('gameQuiz.answeredFor', { name: participantDetails.answeredForDisplayName })}
            </Typography>
          ) : null}
          {round.submittedAnswer ? (
            <Typography variant="caption" color="text.secondary">
              {t('gameQuiz.answerLabel', { answer: round.submittedAnswer })}
            </Typography>
          ) : null}
          {round.awardedPoints != null && round.awardedPoints > 0 ? (
            <Typography variant="caption" color="success.main" fontWeight={700}>
              {t('gameQuiz.pointsEarned', { points: round.awardedPoints })}
            </Typography>
          ) : null}
        </Stack>
      </Stack>
    </Box>
  )
}

function ManualAwardHistoryItem({
  award,
  currentUserId,
}: {
  award: ManualAward
  currentUserId: string | null
}) {
  const { t } = useTranslation()
  const isMyAward = award.awardedToUserId === currentUserId

  return (
    <Box
      sx={(theme) => ({
        px: 2,
        py: 1.5,
        backgroundColor: isMyAward ? alpha(theme.palette.primary.main, 0.05) : 'transparent',
      })}
    >
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
            {t('gameQuiz.manualAwardLabel')}
          </Typography>
          <Chip
            label={t('gameQuiz.pointsEarned', { points: award.awardedPoints })}
            color="success"
            size="small"
            sx={{ height: 20, fontSize: '0.68rem' }}
          />
          <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
            {formatHistoryTime(award.awardedAtUtc)}
          </Typography>
        </Stack>

        <Typography variant="body2">
          {t('gameQuiz.manualAwardDescription', {
            player: award.awardedToDisplayName,
            moderator: award.awardedByDisplayName,
          })}
        </Typography>
      </Stack>
    </Box>
  )
}

function getHistoryItems(rounds: QuizRound[], manualAwards: ManualAward[]): QuizHistoryItem[] {
  const items: QuizHistoryItem[] = [
    ...rounds.map((round) => ({
      id: `round-${round.roundId}`,
      kind: 'round' as const,
      sortAtUtc: round.answeredAtUtc ?? round.askedAtUtc,
      round,
    })),
    ...manualAwards.map((award) => ({
      id: `award-${award.awardId}`,
      kind: 'manualAward' as const,
      sortAtUtc: award.awardedAtUtc,
      award,
    })),
  ]

  return items.sort((left, right) => {
    const timeComparison = right.sortAtUtc.localeCompare(left.sortAtUtc)
    if (timeComparison !== 0) {
      return timeComparison
    }

    return right.id.localeCompare(left.id)
  })
}

function getStatusColor(status: RoundStatus): 'default' | 'success' | 'error' | 'warning' {
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

function getStatusLabel(status: RoundStatus, t: ReturnType<typeof useTranslation>['t']) {
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

function formatHistoryTime(value: string) {
  return new Date(value).toLocaleString()
}
