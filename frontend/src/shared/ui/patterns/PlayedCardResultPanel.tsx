import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../api/contracts/generated'
import {
  formatPlayedCardModifierOutcomeStatus,
  getPlayedCardModifierOutcomeColor,
  normalizePlayedCardModifierOutcomeStatus,
} from '../../lib/played-card-formatters.ts'
import { ParticipantNamesList } from './ParticipantNamesList.tsx'
import { RoundScoreBreakdown } from './RoundScoreBreakdown.tsx'

type PlayedCardPreviewRound = components['schemas']['GameHistoryRoundItemDto']
type PlayedCardPreviewModifier = PlayedCardPreviewRound['modifiers'][number]

interface PlayedCardModifierGroup {
  groupKey: string
  modifierId: string
  modifierName: string
  modifierDescription: string
  count: number
  scoreDelta: number
  killDelta: number
  outcomeStatuses: readonly PlayedCardModifierOutcomeSummary[]
  multiplierAppliedValues: readonly number[]
  definitionRevision: number | null
  violationComments: readonly string[]
}

interface PlayedCardModifierOutcomeSummary {
  status: string
  count: number
}

export function PlayedCardResultPanel({
  cardCost,
  round,
  isLoading,
  isError,
}: {
  cardCost: number
  round: PlayedCardPreviewRound | null
  isLoading: boolean
  isError: boolean
}) {
  const { t } = useTranslation()
  const participants = round?.participants ?? []
  const modifiers = round ? groupPlayedCardModifiers(round.modifiers ?? []) : []
  const finalScore = round?.scoreDetails.finalScore ?? 0
  const penaltyTotal = round?.scoreDetails.penaltyTotal ?? 0

  return (
    <Box
      data-testid="played-card-result-panel"
      sx={(theme) => ({
        minWidth: 0,
        maxHeight: 'min(68vh, 720px)',
        overflowY: 'auto',
        overscrollBehavior: 'contain',
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        pl: 1.1,
        pr: { xs: 1.1, lg: 0.75 },
        py: 1,
      })}
    >
      <Stack spacing={1}>
        <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
          {t('gameHistory.cardPlayResultTitle')}
        </Typography>

        {isLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.cardPlayResultLoading')}
          </Typography>
        ) : isError ? (
          <Typography variant="body2" color="error.main">
            {t('gameHistory.cardPlayResultError')}
          </Typography>
        ) : !round ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameHistory.cardPlayResultEmpty')}
          </Typography>
        ) : (
          <>
            <Stack spacing={0.45}>
              <Typography variant="body2" sx={{ fontWeight: 800 }}>
                {formatPlayedCardTeamName(t, round.teamName, round.teamSlotIndex)}
              </Typography>
              <ParticipantNamesList
                names={participants.map((participant) => participant.displayName)}
                emptyLabel={t('gameHistory.noParticipants')}
                variant="caption"
              />
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gap: 0.65,
                gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
              }}
            >
              <PlayedCardResultMetric
                label={t('gameHistory.cardCostMetricLabel')}
                value={t('gameHistory.pointsValue', { points: cardCost })}
              />
              <PlayedCardResultMetric
                label={t('gameHistory.summary.finalScore')}
                value={t('gameHistory.pointsValue', { points: finalScore })}
              />
              {penaltyTotal > 0 ? (
                <PlayedCardResultMetric
                  label={t('gameHistory.cardPenaltyTotalLabel')}
                  value={t('gameHistory.pointsValue', { points: penaltyTotal })}
                />
              ) : null}
              <PlayedCardResultMetric
                label={t('gameHistory.summary.totalKills')}
                value={t('gameHistory.countValue', { count: round.scoreDetails.totalKillCount })}
              />
              <PlayedCardResultMetric
                label={t('gameHistory.summary.totalBounties')}
                value={t('gameHistory.countValue', { count: round.bountyCount })}
              />
            </Box>

            <RoundScoreBreakdown score={round.scoreDetails} />

            <Stack spacing={0.55}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>
                {t('common.entities.modifiers')}
              </Typography>
              {modifiers.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('gameHistory.cardPlayResultNoModifiers')}
                </Typography>
              ) : (
                <Stack spacing={0.65}>
                  {modifiers.map((modifier) => (
                    <PlayedCardModifierItem key={modifier.groupKey} modifier={modifier} />
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

function PlayedCardModifierItem({ modifier }: { modifier: PlayedCardModifierGroup }) {
  const { t } = useTranslation()
  const modifierTitle =
    modifier.count > 1 ? `${modifier.modifierName} x${modifier.count}` : modifier.modifierName

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
            {modifierTitle}
          </Typography>
          <Stack direction="row" spacing={0.35} flexWrap="wrap" useFlexGap>
            {modifier.definitionRevision ? (
              <Chip
                size="small"
                variant="outlined"
                label={t('gameHistory.modifierRevision', {
                  revision: modifier.definitionRevision,
                })}
              />
            ) : null}
            {modifier.outcomeStatuses.map((status) => (
              <Chip
                key={status.status}
                size="small"
                color={getPlayedCardModifierOutcomeColor(status.status)}
                variant="outlined"
                label={`${formatPlayedCardModifierOutcomeStatus(t, status.status)}${
                  status.count > 1 ? ` x${status.count}` : ''
                }`}
              />
            ))}
          </Stack>
        </Stack>

        <Stack direction="row" spacing={0.4} flexWrap="wrap" useFlexGap>
          {modifier.scoreDelta !== 0 ? (
            <Chip
              size="small"
              variant="filled"
              label={t('gameHistory.pointsValue', {
                points: formatSignedNumber(modifier.scoreDelta),
              })}
            />
          ) : null}
          {modifier.killDelta !== 0 ? (
            <Chip
              size="small"
              variant="outlined"
              label={t('gameHistory.summary.killDeltaShort', {
                value: formatSignedNumber(modifier.killDelta),
              })}
            />
          ) : null}
          {modifier.multiplierAppliedValues.map((value) => (
            <Chip
              key={value}
              size="small"
              variant="outlined"
              label={t('gameHistory.summary.multiplierShort', { value })}
            />
          ))}
        </Stack>

        {modifier.modifierDescription ? (
          <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
            {modifier.modifierDescription}
          </Typography>
        ) : null}
        {modifier.violationComments.map((comment, index) => (
          <Typography
            key={`${modifier.groupKey}-violation-${index}`}
            variant="caption"
            color="warning.main"
          >
            {t('gameHistory.modifierViolationComment', { comment })}
          </Typography>
        ))}
      </Stack>
    </Box>
  )
}

function groupPlayedCardModifiers(
  modifiers: readonly PlayedCardPreviewModifier[],
): PlayedCardModifierGroup[] {
  const grouped = new Map<string, PlayedCardModifierGroup>()

  for (const modifier of modifiers) {
    const groupKey = `${modifier.modifierId}:revision-${modifier.definitionRevision}`
    const current = grouped.get(groupKey)
    if (!current) {
      grouped.set(groupKey, {
        groupKey,
        modifierId: modifier.modifierId,
        modifierName: modifier.modifierName,
        modifierDescription: modifier.modifierDescription,
        count: 1,
        scoreDelta: modifier.scoreDelta,
        killDelta: modifier.killDelta,
        outcomeStatuses: [
          { status: normalizePlayedCardModifierOutcomeStatus(modifier.outcomeStatus), count: 1 },
        ],
        multiplierAppliedValues:
          modifier.multiplierApplied === null || modifier.multiplierApplied === undefined
            ? []
            : [modifier.multiplierApplied],
        definitionRevision: modifier.definitionRevision ?? null,
        violationComments: modifier.violationComment?.trim()
          ? [modifier.violationComment.trim()]
          : [],
      })
      continue
    }

    grouped.set(groupKey, {
      ...current,
      count: current.count + 1,
      scoreDelta: current.scoreDelta + modifier.scoreDelta,
      killDelta: current.killDelta + modifier.killDelta,
      outcomeStatuses: mergeModifierOutcomeStatuses(
        current.outcomeStatuses,
        normalizePlayedCardModifierOutcomeStatus(modifier.outcomeStatus),
      ),
      multiplierAppliedValues: mergeModifierMultiplierValues(
        current.multiplierAppliedValues,
        modifier.multiplierApplied,
      ),
      violationComments: modifier.violationComment?.trim()
        ? [...current.violationComments, modifier.violationComment.trim()]
        : current.violationComments,
    })
  }

  return Array.from(grouped.values())
}

function mergeModifierOutcomeStatuses(
  statuses: readonly PlayedCardModifierOutcomeSummary[],
  nextStatus: string,
) {
  const nextStatuses = [...statuses]
  const existingIndex = nextStatuses.findIndex((item) => item.status === nextStatus)

  if (existingIndex < 0) {
    nextStatuses.push({ status: nextStatus, count: 1 })
    return nextStatuses
  }

  nextStatuses[existingIndex] = {
    ...nextStatuses[existingIndex],
    count: nextStatuses[existingIndex].count + 1,
  }
  return nextStatuses
}

function mergeModifierMultiplierValues(
  values: readonly number[],
  nextValue: number | null | undefined,
) {
  if (nextValue === null || nextValue === undefined || values.includes(nextValue)) {
    return values
  }

  return [...values, nextValue]
}

function formatPlayedCardTeamName(
  t: ReturnType<typeof useTranslation>['t'],
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return teamName?.trim() || t('common.teamWithSlot', { slot: teamSlotIndex })
}

function formatSignedNumber(value: number) {
  return value > 0 ? `+${value}` : `${value}`
}

function PlayedCardResultMetric({ label, value }: { label: string; value: string }) {
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
