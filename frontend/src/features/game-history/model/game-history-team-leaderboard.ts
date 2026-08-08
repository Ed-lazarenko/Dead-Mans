import type { components } from '../../../shared/api/contracts/generated'

export type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

export interface GameHistoryTeamLeaderboardEntry {
  teamId: string
  teamName?: string | null
  teamSlotIndex: number
  roundsPlayed: number
  bestScore: number
  bestRound: GameHistoryRound
  latestRound: GameHistoryRound
  rounds: GameHistoryRound[]
  totalScore: number
  averageScore: number
  totalBonusDelta: number
  totalKills: number
  totalBounties: number
  participantNames: string[]
  lastFinishedAtUtc: string
}

export function buildGameTeamLeaderboard(
  rounds: readonly GameHistoryRound[],
): GameHistoryTeamLeaderboardEntry[] {
  const groupedByTeam = new Map<string, GameHistoryRound[]>()

  for (const round of rounds) {
    if (!isCountedRound(round)) {
      continue
    }

    const existingRounds = groupedByTeam.get(round.teamId)
    if (existingRounds) {
      existingRounds.push(round)
    } else {
      groupedByTeam.set(round.teamId, [round])
    }
  }

  return [...groupedByTeam.values()]
    .map((teamRounds) => {
      const roundsByBestScore = [...teamRounds].sort(compareRoundsByScore)
      const bestRound = roundsByBestScore[0]!
      const roundsByTime = [...teamRounds].sort((left, right) =>
        getRoundSortTimestamp(right).localeCompare(getRoundSortTimestamp(left)),
      )
      const latestRound = roundsByTime[0]!
      const participantNames = [
        ...new Set(
          teamRounds
            .flatMap((round) => round.participants)
            .map((participant) => participant.displayName),
        ),
      ]
      const totalScore = teamRounds.reduce((sum, round) => sum + getRoundScore(round), 0)
      const totalKills = teamRounds.reduce((sum, round) => sum + round.killsCount, 0)
      const totalBounties = teamRounds.reduce((sum, round) => sum + round.bountyCount, 0)

      return {
        teamId: bestRound.teamId,
        teamName: bestRound.teamName,
        teamSlotIndex: bestRound.teamSlotIndex,
        roundsPlayed: teamRounds.length,
        bestScore: getRoundScore(bestRound),
        bestRound,
        latestRound,
        rounds: roundsByTime,
        totalScore,
        averageScore: Math.round(totalScore / teamRounds.length),
        totalBonusDelta: teamRounds.reduce((sum, round) => sum + getRoundBonusDelta(round), 0),
        totalKills,
        totalBounties,
        participantNames,
        lastFinishedAtUtc: getRoundSortTimestamp(latestRound),
      }
    })
    .sort((left, right) => {
      if (right.bestScore !== left.bestScore) {
        return right.bestScore - left.bestScore
      }

      if (right.totalScore !== left.totalScore) {
        return right.totalScore - left.totalScore
      }

      const lastRoundComparison = right.lastFinishedAtUtc.localeCompare(left.lastFinishedAtUtc)
      if (lastRoundComparison !== 0) {
        return lastRoundComparison
      }

      return left.teamSlotIndex - right.teamSlotIndex
    })
}

export function getRoundScore(round: GameHistoryRound) {
  return round.finalScore ?? round.baseScore
}

export function getRoundBonusDelta(round: GameHistoryRound) {
  if (round.emptyCardPenaltyApplied) {
    return getRoundScore(round)
  }

  return getRoundScore(round) - round.baseScore
}

function getRoundSortTimestamp(round: GameHistoryRound) {
  return round.finishedAtUtc ?? round.startedAtUtc
}

function isCountedRound(round: GameHistoryRound) {
  return round.status !== 'in_progress'
}

function compareRoundsByScore(left: GameHistoryRound, right: GameHistoryRound) {
  const scoreDifference = getRoundScore(right) - getRoundScore(left)
  if (scoreDifference !== 0) {
    return scoreDifference
  }

  const bonusDifference = getRoundBonusDelta(right) - getRoundBonusDelta(left)
  if (bonusDifference !== 0) {
    return bonusDifference
  }

  return getRoundSortTimestamp(right).localeCompare(getRoundSortTimestamp(left))
}
