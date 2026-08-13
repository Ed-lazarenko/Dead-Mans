import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../api/contracts/generated'
import { resolveBackendMediaUrl } from '../../api/media-url.ts'
import {
  formatPlayedCardModifierOutcomeStatus,
  getPlayedCardModifierOutcomeColor,
  normalizePlayedCardModifierOutcomeStatus,
} from '../../lib/played-card-formatters.ts'
import { AppDialog } from '../feedback/AppDialog.tsx'
import { ParticipantNamesList } from './ParticipantNamesList.tsx'

type PlayedCardPreviewRound = components['schemas']['GameHistoryRoundItemDto']
type PlayedCardPreviewModifier = PlayedCardPreviewRound['modifiers'][number]

interface PlayedCardPreviewCard {
  title?: string | null
  description?: string | null
  cost: number
  media: readonly { url: string }[]
}

interface PlayedCardPreviewDialogProps {
  card: PlayedCardPreviewCard | null
  round: PlayedCardPreviewRound | null
  isLoading?: boolean
  isError?: boolean
  onClose: () => void
}

interface PlayedCardModifierGroup {
  modifierId: string
  modifierName: string
  modifierDescription: string
  count: number
  scoreDeltas: readonly number[]
  killDeltas: readonly number[]
  outcomeStatuses: readonly PlayedCardModifierOutcomeSummary[]
  multiplierAppliedValues: readonly number[]
}

interface PlayedCardModifierOutcomeSummary {
  status: string
  count: number
}

export function PlayedCardPreviewDialog({
  card,
  round,
  isLoading = false,
  isError = false,
  onClose,
}: PlayedCardPreviewDialogProps) {
  const { t } = useTranslation()
  const previewCard = round ? getCardFromRound(round) : card
  const media = previewCard?.media ?? []

  return (
    <AppDialog
      open={previewCard !== null}
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
        previewCard ? (
          <Typography
            component="span"
            variant="h6"
            sx={{ minWidth: 0, fontWeight: 850, lineHeight: 1.25 }}
          >
            {previewCard.title || t('gameHistory.cardDialogFallbackTitle')}
          </Typography>
        ) : (
          t('gameHistory.cardDialogFallbackTitle')
        )
      }
    >
      {previewCard ? (
        <Stack spacing={1.25}>
          {previewCard.description ? (
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
                {previewCard.description}
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
              {media.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameHistory.cardMediaEmpty')}
                </Typography>
              ) : (
                media.map((item, index) => (
                  <Box
                    key={`${item.url}-${index}`}
                    component="img"
                    src={resolveBackendMediaUrl(item.url)}
                    alt={previewCard.title || t('gameHistory.cardDialogFallbackTitle')}
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

            <PlayedCardResultPanel
              cardCost={previewCard.cost}
              round={round}
              isLoading={isLoading}
              isError={isError}
            />
          </Box>
        </Stack>
      ) : null}
    </AppDialog>
  )
}

function PlayedCardResultPanel({
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

            <Stack spacing={0.55}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>
                {t('gameHistory.modifiersLabel')}
              </Typography>
              {modifiers.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('gameHistory.cardPlayResultNoModifiers')}
                </Typography>
              ) : (
                <Stack spacing={0.65}>
                  {modifiers.map((modifier) => (
                    <PlayedCardModifierItem key={modifier.modifierId} modifier={modifier} />
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
          {modifier.scoreDeltas.map((scoreDelta, index) =>
            scoreDelta !== 0 ? (
              <Chip
                key={`score-${index}-${scoreDelta}`}
                size="small"
                variant="filled"
                label={t('gameHistory.pointsValue', {
                  points: formatSignedNumber(scoreDelta),
                })}
              />
            ) : null,
          )}
          {modifier.killDeltas.map((killDelta, index) =>
            killDelta !== 0 ? (
              <Chip
                key={`kill-${index}-${killDelta}`}
                size="small"
                variant="outlined"
                label={t('gameHistory.summary.killDeltaShort', {
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
              label={t('gameHistory.summary.multiplierShort', { value })}
            />
          ))}
        </Stack>

        {modifier.modifierDescription ? (
          <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
            {modifier.modifierDescription}
          </Typography>
        ) : null}
      </Stack>
    </Box>
  )
}

function groupPlayedCardModifiers(
  modifiers: readonly PlayedCardPreviewModifier[],
): PlayedCardModifierGroup[] {
  const grouped = new Map<string, PlayedCardModifierGroup>()

  for (const modifier of modifiers) {
    const current = grouped.get(modifier.modifierId)
    if (!current) {
      grouped.set(modifier.modifierId, {
        modifierId: modifier.modifierId,
        modifierName: modifier.modifierName,
        modifierDescription: modifier.modifierDescription,
        count: 1,
        scoreDeltas: [modifier.scoreDelta],
        killDeltas: [modifier.killDelta],
        outcomeStatuses: [
          { status: normalizePlayedCardModifierOutcomeStatus(modifier.outcomeStatus), count: 1 },
        ],
        multiplierAppliedValues:
          modifier.multiplierApplied === null || modifier.multiplierApplied === undefined
            ? []
            : [modifier.multiplierApplied],
      })
      continue
    }

    grouped.set(modifier.modifierId, {
      ...current,
      count: current.count + 1,
      scoreDeltas: [...current.scoreDeltas, modifier.scoreDelta],
      killDeltas: [...current.killDeltas, modifier.killDelta],
      outcomeStatuses: mergeModifierOutcomeStatuses(
        current.outcomeStatuses,
        normalizePlayedCardModifierOutcomeStatus(modifier.outcomeStatus),
      ),
      multiplierAppliedValues: mergeModifierMultiplierValues(
        current.multiplierAppliedValues,
        modifier.multiplierApplied,
      ),
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
  return teamName?.trim() || t('gameHistory.teamLabel', { slot: teamSlotIndex })
}

function getCardFromRound(round: PlayedCardPreviewRound): PlayedCardPreviewCard {
  return {
    title: round.cellTitle,
    description: round.cellDescription,
    cost: round.cellCost,
    media: round.cellMedia,
  }
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
