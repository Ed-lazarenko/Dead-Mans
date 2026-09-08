import { Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { ParticipantNamesList } from '../../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'
import {
  buildModifierRuntimeUnits,
  calculateModifierRuntimeClock,
  formatRuntimeDuration,
} from '../../game-modifiers/model/modifier-runtime.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

export function GameRoundContext({ activeRound }: { activeRound: GameRoundDetails }) {
  const { t } = useTranslation()
  const gameplayDuration = getGameplayDurationSeconds(activeRound)
  const expiredTimers = buildModifierRuntimeUnits(activeRound).filter(
    (unit) =>
      unit.durationSeconds !== null &&
      calculateModifierRuntimeClock(
        activeRound,
        unit.durationSeconds,
        Date.parse(activeRound.serverNowUtc),
      ).state === 'expired',
  )

  return (
    <Stack direction="row" spacing={1} alignItems="flex-start" flexWrap="wrap" useFlexGap>
      <Chip
        size="small"
        variant="outlined"
        label={formatTeamNameWithFallback(
          activeRound.teamName,
          t('common.teamWithSlot', { slot: activeRound.teamSlotIndex }),
        )}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryRoundVersion', { version: activeRound.roundVersion })}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryCard', {
          card: activeRound.cellTitle ?? t('gameBoard.roundSummaryCardFallback'),
        })}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryFrozenCardValue', { value: activeRound.baseScore })}
      />
      {gameplayDuration !== null ? (
        <Chip
          size="small"
          variant="outlined"
          label={t('gameBoard.roundSummaryGameplayDuration', {
            duration: formatRuntimeDuration(gameplayDuration),
          })}
        />
      ) : null}
      {expiredTimers.map((timer) => (
        <Chip
          key={timer.key}
          size="small"
          color="warning"
          variant="outlined"
          label={t('gameBoard.roundSummaryExpiredTimer', { modifier: timer.modifierName })}
        />
      ))}
      <Stack spacing={0.35}>
        <Typography variant="caption" color="text.secondary">
          {t('common.entities.players')}
        </Typography>
        <ParticipantNamesList
          names={activeRound.participants.map((participant) => participant.displayName)}
          emptyLabel={t('gameBoard.roundSummaryNoParticipants')}
        />
      </Stack>
    </Stack>
  )
}

function getGameplayDurationSeconds(round: GameRoundDetails) {
  if (!round.gameplayStartedAtUtc) return null
  const startedAtMs = Date.parse(round.gameplayStartedAtUtc)
  const stoppedAtMs = Date.parse(round.reviewedAtUtc ?? round.finishedAtUtc ?? round.serverNowUtc)
  if (!Number.isFinite(startedAtMs) || !Number.isFinite(stoppedAtMs)) return null
  return Math.max(0, Math.floor((stoppedAtMs - startedAtMs) / 1_000))
}
