import { Alert, CircularProgress, Divider, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { RoundScoreBreakdown, SectionCard } from '../../../shared/ui/index.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type ScorePreview = components['schemas']['GameRoundScorePreviewDto']
export type GameRoundPreviewStatus =
  'incomplete' | 'debouncing' | 'loading' | 'success' | 'error' | 'stale'

export interface GameRoundPreviewState {
  status: GameRoundPreviewStatus
  data: ScorePreview | null
  inputKey: string | null
  errorCode: string | null
}

export function GameRoundPreviewSection({
  state,
  score,
}: {
  state: GameRoundPreviewState
  score: GameRoundDetails['scoreDetails'] | null | undefined
}) {
  const { t } = useTranslation()

  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.roundSummaryScoreTitle')}</Typography>
        <Divider />
        {state.status === 'incomplete' ? (
          <Alert severity="warning" variant="outlined">
            {t('gameBoard.roundSummaryPreviewIncomplete')}
          </Alert>
        ) : null}
        {state.status === 'debouncing' || state.status === 'loading' ? (
          <Alert severity="info" variant="outlined" icon={<CircularProgress size={18} />}>
            {t(
              state.status === 'debouncing'
                ? 'gameBoard.roundSummaryPreviewWaiting'
                : 'gameBoard.roundSummaryPreviewLoading',
            )}
          </Alert>
        ) : null}
        {state.status === 'error' ? (
          <Alert severity="error" variant="outlined">
            {t('gameBoard.roundSummaryPreviewFailed', {
              reason: state.errorCode ?? t('gameBoard.roundSummaryPreviewFailedFallback'),
            })}
          </Alert>
        ) : null}
        {state.status === 'stale' ? (
          <Alert severity="error" variant="outlined">
            {t('gameBoard.roundSummaryPreviewStale')}
          </Alert>
        ) : null}
        {state.status === 'success' && score ? (
          <>
            <SummaryMetric
              label={t('gameBoard.roundSummaryScoreUnit')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.scoreUnit })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryKillsScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.killsScore })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryBountiesScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.bountyScore })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryModifierKills')}
              value={t('gameBoard.roundSummaryModifierKillsValue', {
                kills: score.modifierKillDelta,
                score: score.modifierKillScore,
              })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryModifierPoints')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.modifierScoreDelta })}
            />
            {score.emptyCardPenaltyScore ? (
              <SummaryMetric
                label={t('gameBoard.roundSummaryEmptyCardPenalty')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: score.emptyCardPenaltyScore,
                })}
              />
            ) : null}
            <SummaryMetric
              label={t('gameBoard.roundSummaryTotalKills')}
              value={String(score.totalKillCount)}
              emphasize
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryFinalScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.finalScore })}
              emphasize
            />
            <RoundScoreBreakdown score={score} />
            {state.data?.calculationTrace.length ? (
              <Stack spacing={0.75}>
                <Typography variant="caption" color="text.secondary">
                  {t('gameBoard.roundSummaryTraceTitle')}
                </Typography>
                {state.data.calculationTrace.map((trace) => (
                  <Stack
                    key={trace.modifierResultId}
                    direction="row"
                    spacing={1}
                    justifyContent="space-between"
                  >
                    <Typography variant="caption">
                      {trace.formulaCode ?? trace.resolutionKind}
                    </Typography>
                    <Typography variant="caption" fontWeight={700}>
                      {t('gameBoard.roundSummaryTraceDelta', {
                        points: trace.pointsDelta,
                        kills: trace.bonusKillsDelta,
                      })}
                    </Typography>
                  </Stack>
                ))}
              </Stack>
            ) : null}
          </>
        ) : null}
      </Stack>
    </SectionCard>
  )
}

function SummaryMetric({
  label,
  value,
  emphasize = false,
}: {
  label: string
  value: string
  emphasize?: boolean
}) {
  return (
    <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant={emphasize ? 'subtitle2' : 'body2'} fontWeight={emphasize ? 700 : 500}>
        {value}
      </Typography>
    </Stack>
  )
}
