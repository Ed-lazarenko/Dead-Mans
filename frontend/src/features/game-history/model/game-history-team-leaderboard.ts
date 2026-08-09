import type { components } from '../../../shared/api/contracts/generated'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']
export type GameHistoryTeamLeaderboardEntry =
  components['schemas']['GameHistoryTeamLeaderboardEntryDto']

export function getRoundScore(round: GameHistoryRound) {
  return round.scoreDetails.finalScore
}

export function getRoundBonusDelta(round: GameHistoryRound) {
  return round.scoreDetails.bonusDelta
}
