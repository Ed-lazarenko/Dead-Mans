import { AccordionDetails, AccordionSummary, Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { AppButton, ParticipantNamesList } from '../../../shared/ui/index.ts'
import { getRoundBonusDelta, getRoundScore } from '../model/game-history-team-leaderboard.ts'
import { formatCardLabel, formatHistoryTeamName } from '../model/game-history-formatters.ts'
import {
  formatOptionalDateTime,
  formatSignedNumber,
  getRoundStatusColor,
  normalizeRoundStatus,
} from '../model/game-history-view.ts'
import { MiniMetricChip } from './game-history-display.tsx'
import { AccordionSurface, CollapsibleSection, ExpandGlyph } from './game-history-surfaces.tsx'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export function RoundHistoryRow({
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
