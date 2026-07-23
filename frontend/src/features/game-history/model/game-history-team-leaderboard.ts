import type { components } from '../../../shared/api/contracts/generated'

export type GameHistoryCardRun = components['schemas']['GameHistoryCardRunItemDto']

export interface GameHistoryTeamLeaderboardEntry {
  teamId: string
  teamSlotIndex: number
  roundsPlayed: number
  bestScore: number
  bestRun: GameHistoryCardRun
  latestRun: GameHistoryCardRun
  runs: GameHistoryCardRun[]
  totalScore: number
  averageScore: number
  totalBonusDelta: number
  totalKills: number
  totalBounties: number
  participantNames: string[]
  lastFinishedAtUtc: string
}

export function buildGameTeamLeaderboard(
  cardRuns: readonly GameHistoryCardRun[],
): GameHistoryTeamLeaderboardEntry[] {
  const groupedByTeam = new Map<string, GameHistoryCardRun[]>()

  for (const run of cardRuns) {
    if (!isCountedCardRun(run)) {
      continue
    }

    const existingRuns = groupedByTeam.get(run.teamId)
    if (existingRuns) {
      existingRuns.push(run)
    } else {
      groupedByTeam.set(run.teamId, [run])
    }
  }

  return [...groupedByTeam.values()]
    .map((teamRuns) => {
      const runsByBestScore = [...teamRuns].sort(compareCardRunsByScore)
      const bestRun = runsByBestScore[0]!
      const runsByTime = [...teamRuns].sort((left, right) =>
        getCardRunSortTimestamp(right).localeCompare(getCardRunSortTimestamp(left)),
      )
      const latestRun = runsByTime[0]!
      const participantNames = [
        ...new Set(
          teamRuns.flatMap((run) => run.participants).map((participant) => participant.displayName),
        ),
      ]
      const totalScore = teamRuns.reduce((sum, run) => sum + getCardRunScore(run), 0)
      const totalKills = teamRuns.reduce((sum, run) => sum + run.killsCount, 0)
      const totalBounties = teamRuns.reduce((sum, run) => sum + run.bountyCount, 0)

      return {
        teamId: bestRun.teamId,
        teamSlotIndex: bestRun.teamSlotIndex,
        roundsPlayed: teamRuns.length,
        bestScore: getCardRunScore(bestRun),
        bestRun,
        latestRun,
        runs: runsByTime,
        totalScore,
        averageScore: Math.round(totalScore / teamRuns.length),
        totalBonusDelta: teamRuns.reduce((sum, run) => sum + getCardRunBonusDelta(run), 0),
        totalKills,
        totalBounties,
        participantNames,
        lastFinishedAtUtc: getCardRunSortTimestamp(latestRun),
      }
    })
    .sort((left, right) => {
      if (right.bestScore !== left.bestScore) {
        return right.bestScore - left.bestScore
      }

      if (right.totalScore !== left.totalScore) {
        return right.totalScore - left.totalScore
      }

      const lastRunComparison = right.lastFinishedAtUtc.localeCompare(left.lastFinishedAtUtc)
      if (lastRunComparison !== 0) {
        return lastRunComparison
      }

      return left.teamSlotIndex - right.teamSlotIndex
    })
}

export function getCardRunScore(run: GameHistoryCardRun) {
  return run.finalScore ?? run.baseScore
}

export function getCardRunBonusDelta(run: GameHistoryCardRun) {
  return getCardRunScore(run) - run.baseScore
}

export function getCardRunSortTimestamp(run: GameHistoryCardRun) {
  return run.finishedAtUtc ?? run.startedAtUtc
}

function isCountedCardRun(run: GameHistoryCardRun) {
  return run.status !== 'in_progress'
}

function compareCardRunsByScore(left: GameHistoryCardRun, right: GameHistoryCardRun) {
  const scoreDifference = getCardRunScore(right) - getCardRunScore(left)
  if (scoreDifference !== 0) {
    return scoreDifference
  }

  const bonusDifference = getCardRunBonusDelta(right) - getCardRunBonusDelta(left)
  if (bonusDifference !== 0) {
    return bonusDifference
  }

  return getCardRunSortTimestamp(right).localeCompare(getCardRunSortTimestamp(left))
}
