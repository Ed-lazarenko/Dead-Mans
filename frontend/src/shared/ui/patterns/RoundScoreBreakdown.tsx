import { Box, Divider, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../api/contracts/generated'

type ScoreDetails = components['schemas']['GameRoundScoreDetailsDto']
type CalculationLine = components['schemas']['GameRoundScoreCalculationLineDto']

export function RoundScoreBreakdown({ score }: { score: ScoreDetails }) {
  const { t } = useTranslation()

  return (
    <Box
      data-testid="round-score-breakdown"
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.75)}`,
        borderRadius: 1.5,
        p: 1.25,
        backgroundColor: alpha(theme.palette.background.default, 0.24),
      })}
    >
      <Stack spacing={1}>
        <Typography variant="subtitle2" fontWeight={850}>
          {t('common.scoreBreakdown.title')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t('common.scoreBreakdown.authoritative')}
        </Typography>
        <Divider />
        {(score.calculationLines ?? []).map((line, index) => (
          <CalculationRow key={`${line.kind}-${line.modifierId ?? 'base'}-${index}`} line={line} />
        ))}
        <Divider />
        <Stack direction="row" spacing={1} justifyContent="space-between">
          <Typography variant="subtitle2" fontWeight={850}>
            {t('common.scoreBreakdown.final')}
          </Typography>
          <Typography variant="subtitle2" fontWeight={900}>
            {formatSigned(score.finalScore, false)}
          </Typography>
        </Stack>
      </Stack>
    </Box>
  )
}

function CalculationRow({ line }: { line: CalculationLine }) {
  const { t } = useTranslation()
  const values = Object.fromEntries(line.operands.map((item) => [item.code, item.value]))
  const title = line.modifierName
    ? t('common.scoreBreakdown.modifierTitle', {
        name: line.modifierName,
        count: line.activationCount,
      })
    : t(`common.scoreBreakdown.kind.${line.kind}`)
  const explanation = describeLine(line, values, t)

  return (
    <Stack spacing={0.25}>
      <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="baseline">
        <Typography variant="body2" fontWeight={750}>
          {title}
        </Typography>
        <Typography
          variant="body2"
          fontWeight={850}
          color={line.pointsDelta < 0 ? 'error.main' : 'text.primary'}
        >
          {formatSigned(line.pointsDelta)}
        </Typography>
      </Stack>
      <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
        {explanation}
      </Typography>
      <Typography variant="caption" color="text.secondary" textAlign="right">
        {t('common.scoreBreakdown.runningTotal', { value: line.runningTotal })}
      </Typography>
    </Stack>
  )
}

function describeLine(
  line: CalculationLine,
  value: Record<string, number>,
  t: ReturnType<typeof useTranslation>['t'],
) {
  if (line.kind === 'kills') {
    return t('common.scoreBreakdown.formula.units', {
      count: value.killsCount,
      unit: value.cardValue,
      result: line.pointsDelta,
    })
  }
  if (line.kind === 'bounties') {
    return t('common.scoreBreakdown.formula.units', {
      count: value.bountyCount,
      unit: value.cardValue,
      result: line.pointsDelta,
    })
  }
  if (line.kind === 'emptyCardPenalty') {
    return t('common.scoreBreakdown.formula.emptyPenalty', { cardValue: value.cardValue })
  }
  if (line.formulaCode === 'growing_kill_value') {
    if (value.killsCount === 0) {
      return t('common.scoreBreakdown.formula.growingZero', {
        penalty: value.zeroKillPenaltyPoints,
        activations: value.activationCount,
        result: line.pointsDelta,
      })
    }
    return t('common.scoreBreakdown.formula.growing', {
      increment: value.incrementPointsPerKill,
      kills: value.killsCount,
      activations: value.activationCount,
      bonusPerKill: value.bonusPerKill,
      cardValue: value.cardValue,
      adjustedKillValue: value.adjustedKillValue,
      adjustedKillsScore: value.adjustedKillsScore,
      baseKillsScore: value.baseKillsScore,
      result: line.pointsDelta,
    })
  }
  if (line.kind === 'modifierBonusKills') {
    return t('common.scoreBreakdown.formula.bonusKills', {
      bonusKills: value.bonusKills,
      cardValue: value.cardValue,
      result: line.pointsDelta,
    })
  }
  if (line.formulaCode === 'window_kill_bonus_points') {
    return t('common.scoreBreakdown.formula.windowBonus', {
      count: value.inputCount ?? 0,
      cardValue: value.cardValue,
      rate: (value.bonusRate ?? 0) * 100,
      result: line.pointsDelta,
    })
  }
  return t('common.scoreBreakdown.formula.delta', { result: line.pointsDelta })
}

function formatSigned(value: number, showPlus = true) {
  return `${showPlus && value > 0 ? '+' : ''}${value}`
}
