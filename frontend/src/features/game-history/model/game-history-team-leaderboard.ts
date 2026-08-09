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

function getRoundScoreBeforePenalty(round: GameHistoryRound) {
  return round.scoreDetails.finalScore + round.scoreDetails.penaltyTotal
}

export function getTeamBestScore(entry: GameHistoryTeamLeaderboardEntry) {
  if (hasServerTeamFinalScore(entry)) {
    return Math.max(0, entry.bestScore)
  }

  return entry.rounds.reduce(
    (bestScore, round) => Math.max(bestScore, getRoundScoreBeforePenalty(round)),
    0,
  )
}

export function getTeamPenaltyTotal(entry: GameHistoryTeamLeaderboardEntry) {
  if (hasServerTeamFinalScore(entry)) {
    return entry.penaltyTotal
  }

  return entry.rounds.reduce(
    (penaltyTotal, round) => penaltyTotal + round.scoreDetails.penaltyTotal,
    0,
  )
}

export function getTeamFinalScore(entry: GameHistoryTeamLeaderboardEntry) {
  if (hasServerTeamFinalScore(entry)) {
    return entry.finalScore
  }

  return getTeamBestScore(entry) - getTeamPenaltyTotal(entry)
}

export function getTeamTotalKills(entry: GameHistoryTeamLeaderboardEntry) {
  return entry.rounds.reduce(
    (totalKills, round) => totalKills + round.scoreDetails.totalKillCount,
    0,
  )
}

export function getTeamTotalBounties(entry: GameHistoryTeamLeaderboardEntry) {
  return entry.rounds.reduce((totalBounties, round) => totalBounties + round.bountyCount, 0)
}

export function sortRoundsByPlaySequence(rounds: readonly GameHistoryRound[]) {
  return [...rounds].sort((left, right) => {
    const startedDiff = Date.parse(left.startedAtUtc) - Date.parse(right.startedAtUtc)
    if (startedDiff !== 0) {
      return startedDiff
    }

    return (
      Date.parse(left.finishedAtUtc ?? left.startedAtUtc) -
      Date.parse(right.finishedAtUtc ?? right.startedAtUtc)
    )
  })
}

export function sortTeamLeaderboardEntries(entries: readonly GameHistoryTeamLeaderboardEntry[]) {
  return [...entries].sort((left, right) => {
    const finalScoreDiff = getTeamFinalScore(right) - getTeamFinalScore(left)
    if (finalScoreDiff !== 0) {
      return finalScoreDiff
    }

    const bestScoreDiff = getTeamBestScore(right) - getTeamBestScore(left)
    if (bestScoreDiff !== 0) {
      return bestScoreDiff
    }

    const totalScoreDiff = right.totalScore - left.totalScore
    if (totalScoreDiff !== 0) {
      return totalScoreDiff
    }

    const lastFinishedDiff =
      Date.parse(right.lastFinishedAtUtc) - Date.parse(left.lastFinishedAtUtc)
    if (lastFinishedDiff !== 0) {
      return lastFinishedDiff
    }

    return left.teamSlotIndex - right.teamSlotIndex
  })
}

function hasServerTeamFinalScore(entry: GameHistoryTeamLeaderboardEntry) {
  const maybeEntry = entry as Partial<GameHistoryTeamLeaderboardEntry>
  return typeof maybeEntry.finalScore === 'number' && typeof maybeEntry.penaltyTotal === 'number'
}
