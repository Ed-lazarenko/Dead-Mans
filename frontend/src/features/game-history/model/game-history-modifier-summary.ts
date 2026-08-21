import type { components } from '../../../shared/api/contracts/generated'

type GameHistoryRound = components['schemas']['GameHistoryRoundItemDto']

interface GameHistoryModifierSummaryItem {
  key: string
  modifierId: string
  modifierName: string
  modifierDescription: string
  definitionRevision: number | null
  activationCount: number
  roundCount: number
  pointsDelta: number
  bonusKillsDelta: number
  outcomes: readonly { status: string; count: number }[]
}

export function buildGameHistoryModifierSummary(
  rounds: readonly GameHistoryRound[],
): GameHistoryModifierSummaryItem[] {
  const grouped = new Map<
    string,
    GameHistoryModifierSummaryItem & { roundIds: Set<string>; outcomeCounts: Map<string, number> }
  >()

  for (const round of rounds) {
    if (round.status !== 'completed') continue
    for (const modifier of round.modifiers) {
      const key = `${modifier.modifierId}:revision-${modifier.definitionRevision}`
      const current = grouped.get(key)
      if (current) {
        current.activationCount += 1
        current.roundIds.add(round.roundId)
        current.pointsDelta += modifier.scoreDelta
        current.bonusKillsDelta += modifier.killDelta
        current.outcomeCounts.set(
          modifier.outcomeStatus,
          (current.outcomeCounts.get(modifier.outcomeStatus) ?? 0) + 1,
        )
        continue
      }

      grouped.set(key, {
        key,
        modifierId: modifier.modifierId,
        modifierName: modifier.modifierName,
        modifierDescription: modifier.modifierDescription,
        definitionRevision: modifier.definitionRevision ?? null,
        activationCount: 1,
        roundCount: 1,
        roundIds: new Set([round.roundId]),
        pointsDelta: modifier.scoreDelta,
        bonusKillsDelta: modifier.killDelta,
        outcomes: [],
        outcomeCounts: new Map([[modifier.outcomeStatus, 1]]),
      })
    }
  }

  return Array.from(grouped.values())
    .map(({ roundIds, outcomeCounts, ...item }) => ({
      ...item,
      roundCount: roundIds.size,
      outcomes: Array.from(outcomeCounts, ([status, count]) => ({ status, count })),
    }))
    .sort((left, right) => left.modifierName.localeCompare(right.modifierName))
}
