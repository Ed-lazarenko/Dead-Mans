import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell } from '../../../shared/api/contracts/index.ts'
import { resolveBackendMediaUrl } from '../../../shared/api/media-url.ts'
import { AppDialog } from '../../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'
import {
  groupCardPlayResultModifiers,
  type CardPlayResultModifierCalculation,
  type GroupedCardPlayResultModifier,
} from '../model/card-play-result-modifiers.ts'
import type { GameBoardCardPlayResultRound } from '../use-card-play-result.ts'

interface GameBoardCardPreviewDialogProps {
  cell: GameBoardCell | null
  playResult: {
    round: GameBoardCardPlayResultRound | null
    isLoading: boolean
    isError: boolean
  }
  onClose: () => void
}

export function GameBoardCardPreviewDialog({
  cell,
  playResult,
  onClose,
}: GameBoardCardPreviewDialogProps) {
  const { t } = useTranslation()

  return (
    <AppDialog
      open={cell !== null}
      onClose={onClose}
      maxWidth="lg"
      PaperProps={{
        sx: (theme) => ({
          borderRadius: 2.5,
          border: `1px solid ${alpha(theme.palette.divider, 0.82)}`,
          backgroundImage: 'none',
          boxShadow: `0 22px 70px ${alpha(theme.palette.common.black, 0.38)}`,
          overflow: 'hidden',
        }),
      }}
      title={
        cell ? (
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            justifyContent="space-between"
          >
            <Typography
              component="span"
              variant="h6"
              sx={{
                minWidth: 0,
                fontWeight: 850,
                lineHeight: 1.25,
              }}
            >
              {cell.title || t('gameBoard.cellMediaDialogTitle')}
            </Typography>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.costLabel', { cost: cell.cost })}
            />
          </Stack>
        ) : (
          t('gameBoard.cellMediaDialogTitle')
        )
      }
    >
      {cell ? (
        <Stack spacing={1.25}>
          {cell.description ? (
            <Box
              sx={(theme) => ({
                borderRadius: 1.5,
                border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
                backgroundColor: alpha(theme.palette.background.paper, 0.38),
                px: 1.15,
                py: 0.95,
              })}
            >
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                {cell.description}
              </Typography>
            </Box>
          ) : null}

          <Box
            sx={{
              display: 'grid',
              gap: 1.25,
              gridTemplateColumns: {
                xs: '1fr',
                lg: 'minmax(0, 1fr) 320px',
              },
              alignItems: 'start',
            }}
          >
            <Box
              sx={(theme) => ({
                display: 'grid',
                gap: 1,
                gridTemplateColumns: '1fr',
                justifyItems: 'center',
                alignItems: 'center',
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
                background: `linear-gradient(180deg, ${alpha(
                  theme.palette.background.paper,
                  0.42,
                )}, ${alpha(theme.palette.common.black, 0.1)})`,
                boxShadow: `inset 0 1px 0 ${alpha(theme.palette.common.white, 0.06)}`,
                px: { xs: 0.75, sm: 1.1 },
                py: { xs: 0.75, sm: 1.1 },
                minHeight: { xs: 220, sm: 280 },
              })}
            >
              {cell.media.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.cellMediaEmpty')}
                </Typography>
              ) : (
                cell.media.map((media, index) => (
                  <Box
                    key={`${media.url}-${index}`}
                    component="img"
                    src={resolveBackendMediaUrl(media.url)}
                    alt={cell.title || t('gameBoard.cellMediaDialogTitle')}
                    loading="lazy"
                    decoding="async"
                    sx={{
                      display: 'block',
                      width: 'auto',
                      maxWidth: '100%',
                      height: 'auto',
                      maxHeight: { xs: '48vh', sm: '54vh', md: '58vh' },
                      borderRadius: 1.5,
                      boxShadow: (theme) =>
                        `0 14px 34px ${alpha(theme.palette.common.black, 0.28)}`,
                      objectFit: 'contain',
                      backgroundColor: 'background.default',
                    }}
                  />
                ))
              )}
            </Box>

            <CardPlayResultPanel
              round={playResult.round}
              isLoading={playResult.isLoading}
              isError={playResult.isError}
            />
          </Box>
        </Stack>
      ) : null}
    </AppDialog>
  )
}

function CardPlayResultPanel({
  round,
  isLoading,
  isError,
}: {
  round: GameBoardCardPlayResultRound | null
  isLoading: boolean
  isError: boolean
}) {
  const { t } = useTranslation()
  const groupedModifiers = round ? groupCardPlayResultModifiers(round.modifiers) : []
  const finalScore = round?.scoreDetails.finalScore ?? 0
  const penaltyTotal = round?.scoreDetails.penaltyTotal ?? 0
  const emptyCardPenaltyScore = round?.scoreDetails.emptyCardPenaltyScore ?? 0

  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        px: 1.1,
        py: 1,
      })}
    >
      <Stack spacing={1}>
        <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
          {t('gameBoard.cardPlayResultTitle')}
        </Typography>

        {isLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.cardPlayResultLoading')}
          </Typography>
        ) : isError ? (
          <Typography variant="body2" color="error.main">
            {t('gameBoard.cardPlayResultError')}
          </Typography>
        ) : !round ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.cardPlayResultEmpty')}
          </Typography>
        ) : (
          <>
            <Stack spacing={0.45}>
              <Typography variant="body2" sx={{ fontWeight: 800 }}>
                {formatGameBoardTeamName(t, round.teamName, round.teamSlotIndex)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {round.participants.length > 0
                  ? round.participants.map((participant) => participant.displayName).join(', ')
                  : t('gameBoard.roundSummaryNoParticipants')}
              </Typography>
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gap: 0.65,
                gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
              }}
            >
              <CardResultMetric
                label={t('gameBoard.roundSummaryFinalScore')}
                value={formatCardPlayResultScoreValue(t, finalScore)}
              />
              {penaltyTotal > 0 ? (
                <CardResultMetric
                  label={t('gameBoard.cardPlayResultTotalPenalty')}
                  value={t('gameBoard.cardPlayResultPenaltyValue', {
                    value: penaltyTotal,
                  })}
                />
              ) : null}
              <CardResultMetric
                label={t('gameBoard.roundSummaryScoreUnit')}
                value={t('gameBoard.roundSummaryScoreValue', { value: round.cellCost })}
              />
              {emptyCardPenaltyScore ? (
                <CardResultMetric
                  label={t('gameBoard.roundSummaryEmptyCardPenalty')}
                  value={t('gameBoard.roundSummaryScoreValue', {
                    value: emptyCardPenaltyScore,
                  })}
                />
              ) : null}
              <CardResultMetric
                label={t('gameBoard.roundSummaryKills')}
                value={String(round.killsCount)}
              />
              <CardResultMetric
                label={t('gameBoard.roundSummaryBounties')}
                value={String(round.bountyCount)}
              />
            </Box>

            <Stack spacing={0.55}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>
                {t('gameBoard.cardPlayResultModifiers')}
              </Typography>
              {groupedModifiers.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('gameBoard.cardPlayResultNoModifiers')}
                </Typography>
              ) : (
                <Stack spacing={0.65}>
                  {groupedModifiers.map((modifier) => (
                    <CardResultModifierItem key={modifier.modifierId} modifier={modifier} />
                  ))}
                </Stack>
              )}
            </Stack>
          </>
        )}
      </Stack>
    </Box>
  )
}

function CardResultModifierItem({ modifier }: { modifier: GroupedCardPlayResultModifier }) {
  const { t } = useTranslation()
  const calculationFacts = buildModifierCalculationFacts(t, modifier.calculation)

  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 1.5,
        border: `1px solid ${alpha(theme.palette.divider, 0.66)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.3),
        px: 0.85,
        py: 0.75,
      })}
    >
      <Stack spacing={0.55}>
        <Stack
          direction="row"
          spacing={0.7}
          alignItems="center"
          justifyContent="space-between"
          flexWrap="wrap"
          useFlexGap
        >
          <Typography variant="body2" sx={{ minWidth: 0, fontWeight: 820 }} noWrap>
            {modifier.count > 1
              ? `${modifier.modifierName} x${modifier.count}`
              : modifier.modifierName}
          </Typography>
          <Stack direction="row" spacing={0.35} flexWrap="wrap" useFlexGap>
            {modifier.outcomeStatuses.map((status) => (
              <Chip
                key={status.status}
                size="small"
                color={getModifierOutcomeColor(status.status)}
                variant="outlined"
                label={`${formatModifierOutcomeStatus(t, status.status)}${
                  status.count > 1 ? ` x${status.count}` : ''
                }`}
              />
            ))}
          </Stack>
        </Stack>

        <Stack direction="row" spacing={0.4} flexWrap="wrap" useFlexGap>
          {modifier.scoreDeltas.map((scoreDelta, index) => (
            <Chip
              key={`score-${index}-${scoreDelta}`}
              size="small"
              variant="filled"
              label={t('gameBoard.roundSummaryScoreValue', {
                value: formatSignedNumber(scoreDelta),
              })}
            />
          ))}
          {modifier.killDeltas.map((killDelta, index) =>
            killDelta !== 0 ? (
              <Chip
                key={`kill-${index}-${killDelta}`}
                size="small"
                variant="outlined"
                label={t('gameBoard.cardPlayResultModifierKillDelta', {
                  value: formatSignedNumber(killDelta),
                })}
              />
            ) : null,
          )}
          {modifier.multiplierAppliedValues.map((value) => (
            <Chip
              key={value}
              size="small"
              variant="outlined"
              label={t('gameBoard.cardPlayResultModifierMultiplier', { value })}
            />
          ))}
        </Stack>

        {calculationFacts.length > 0 ? (
          <Typography variant="caption" color="text.secondary" sx={{ wordBreak: 'break-word' }}>
            {t('gameBoard.cardPlayResultModifierCalculation', {
              details: calculationFacts.join(' · '),
            })}
          </Typography>
        ) : null}

        {modifier.calculation?.successExpression ? (
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontFamily: 'monospace', wordBreak: 'break-word' }}
          >
            {t('gameBoard.cardPlayResultModifierExpression', {
              label: t('gameBoard.cardPlayResultModifierSuccessExpression'),
              expression: modifier.calculation.successExpression,
            })}
          </Typography>
        ) : null}

        {modifier.calculation?.failureExpression ? (
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontFamily: 'monospace', wordBreak: 'break-word' }}
          >
            {t('gameBoard.cardPlayResultModifierExpression', {
              label: t('gameBoard.cardPlayResultModifierFailureExpression'),
              expression: modifier.calculation.failureExpression,
            })}
          </Typography>
        ) : null}
      </Stack>
    </Box>
  )
}

function formatGameBoardTeamName(
  t: ReturnType<typeof useTranslation>['t'],
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(
    teamName,
    t('gameBoard.teamQueueTeamTitle', { slot: teamSlotIndex }),
  )
}

function buildModifierCalculationFacts(
  t: ReturnType<typeof useTranslation>['t'],
  calculation: CardPlayResultModifierCalculation | null,
) {
  if (!calculation) {
    return []
  }

  const facts: string[] = []

  if (calculation.conditionMet !== null) {
    facts.push(
      t(
        calculation.conditionMet
          ? 'gameBoard.cardPlayResultModifierConditionMet'
          : 'gameBoard.cardPlayResultModifierConditionMissed',
      ),
    )
  }

  if (calculation.conditionType) {
    facts.push(
      t('gameBoard.cardPlayResultModifierConditionType', {
        type: calculation.conditionType,
      }),
    )
  }

  if (calculation.input && calculation.countValue !== null) {
    facts.push(
      t('gameBoard.cardPlayResultModifierInputValue', {
        input: formatModifierCalculationInput(t, calculation.input),
        count: calculation.countValue,
      }),
    )
  } else if (calculation.countValue !== null) {
    facts.push(t('gameBoard.cardPlayResultModifierCountValue', { count: calculation.countValue }))
  }

  if (calculation.killDeltaValue !== null && calculation.killDeltaValue !== 0) {
    facts.push(
      t('gameBoard.cardPlayResultModifierKillDeltaValue', {
        value: formatSignedNumber(calculation.killDeltaValue),
      }),
    )
  }

  if (calculation.multiplierDelta !== null && calculation.multiplierDelta !== 0) {
    facts.push(
      t('gameBoard.cardPlayResultModifierMultiplierDelta', {
        value: formatSignedNumber(calculation.multiplierDelta),
      }),
    )
  }

  if (calculation.killsCount !== null) {
    facts.push(t('gameBoard.cardPlayResultModifierKillsCount', { count: calculation.killsCount }))
  }

  if (calculation.bountyCount !== null) {
    facts.push(t('gameBoard.cardPlayResultModifierBountyCount', { count: calculation.bountyCount }))
  }

  if (calculation.activationCount !== null) {
    facts.push(
      t('gameBoard.cardPlayResultModifierActivationCount', {
        count: calculation.activationCount,
      }),
    )
  }

  if (calculation.perKillBonus !== null && calculation.perKillBonus !== 0) {
    facts.push(
      t('gameBoard.cardPlayResultModifierPerKillBonus', {
        value: formatSignedNumber(calculation.perKillBonus),
      }),
    )
  }

  if (calculation.failurePenaltyPoints !== null && calculation.failurePenaltyPoints !== 0) {
    facts.push(
      t('gameBoard.cardPlayResultModifierFailurePenalty', {
        value: formatSignedNumber(-1 * Math.abs(calculation.failurePenaltyPoints)),
      }),
    )
  }

  if (calculation.formulaMode) {
    facts.push(
      t('gameBoard.cardPlayResultModifierFormulaMode', {
        mode: formatModifierFormulaMode(t, calculation.formulaMode),
      }),
    )
  }

  return facts
}

function formatModifierCalculationInput(t: ReturnType<typeof useTranslation>['t'], input: string) {
  switch (input) {
    case 'bonusKills':
      return t('gameBoard.cardPlayResultModifierInputBonusKills')
    case 'mentorKills':
      return t('gameBoard.cardPlayResultModifierInputMentorKills')
    case 'killsDuringWindow':
      return t('gameBoard.cardPlayResultModifierInputKillsDuringWindow')
    default:
      return input
  }
}

function formatModifierOutcomeStatus(t: ReturnType<typeof useTranslation>['t'], status: string) {
  switch (status) {
    case 'completed':
      return t('gameBoard.roundSummaryModifierStatusOption.completed')
    case 'failed':
      return t('gameBoard.roundSummaryModifierStatusOption.failed')
    case 'cancelled':
      return t('gameBoard.roundSummaryModifierStatusOption.cancelled')
    default:
      return status
  }
}

function getModifierOutcomeColor(status: string): 'default' | 'success' | 'warning' {
  switch (status) {
    case 'completed':
      return 'success'
    case 'failed':
      return 'warning'
    default:
      return 'default'
  }
}

function formatModifierFormulaMode(t: ReturnType<typeof useTranslation>['t'], mode: string) {
  switch (mode) {
    case 'flat_per_kill':
      return t('gameBoard.cardPlayResultModifierFormulaModeFlatPerKill')
    case 'stacking_per_kill_bonus':
      return t('gameBoard.cardPlayResultModifierFormulaModeStackingPerKillBonus')
    case 'custom_expression':
      return t('gameBoard.cardPlayResultModifierFormulaModeCustomExpression')
    default:
      return mode
  }
}

function formatSignedNumber(value: number) {
  return value > 0 ? `+${value}` : String(value)
}

function formatCardPlayResultScoreValue(t: ReturnType<typeof useTranslation>['t'], score: number) {
  if (score < 0) {
    return t('gameBoard.cardPlayResultPenaltyValue', { value: Math.abs(score) })
  }

  return t('gameBoard.roundSummaryScoreValue', { value: score })
}

function CardResultMetric({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 1.4,
        border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.34),
        px: 0.8,
        py: 0.65,
      })}
    >
      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 850 }} noWrap>
        {value}
      </Typography>
    </Box>
  )
}
