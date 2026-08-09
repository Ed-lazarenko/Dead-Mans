import type { components } from '../../../shared/api/contracts/generated'

export type GameBoardCellPlayResultRound = components['schemas']['GameHistoryRoundItemDto']

export type GameBoardCellPlayResult = Pick<
  GameBoardCellPlayResultRound,
  | 'roundId'
  | 'cellId'
  | 'teamName'
  | 'teamSlotIndex'
  | 'finalScore'
  | 'baseScore'
  | 'emptyCardPenaltyApplied'
  | 'scoreDetails'
  | 'killsCount'
  | 'bountyCount'
  | 'participants'
  | 'modifiers'
  | 'finishedAtUtc'
  | 'status'
>

export function buildGameBoardCellPlayResultMap(
  rounds: readonly GameBoardCellPlayResultRound[],
  allowedCellIds?: ReadonlySet<string>,
) {
  const resultsByCellId = new Map<string, GameBoardCellPlayResult>()
  const latestTimeByCellId = new Map<string, number>()

  for (const round of rounds) {
    if (
      !isFinalizedGameBoardCellPlayResult(round) ||
      (allowedCellIds && !allowedCellIds.has(round.cellId))
    ) {
      continue
    }

    const roundTime = getGameBoardCellPlayResultTime(round)
    const latestTime = latestTimeByCellId.get(round.cellId) ?? Number.NEGATIVE_INFINITY
    if (roundTime <= latestTime) {
      continue
    }

    latestTimeByCellId.set(round.cellId, roundTime)
    resultsByCellId.set(round.cellId, round)
  }

  return resultsByCellId
}

export function findLatestGameBoardCellPlayResult(
  rounds: readonly GameBoardCellPlayResultRound[],
  cellId: string,
) {
  return buildGameBoardCellPlayResultMap(rounds, new Set([cellId])).get(cellId) ?? null
}

function isFinalizedGameBoardCellPlayResult(round: GameBoardCellPlayResultRound) {
  return (
    round.finishedAtUtc !== null &&
    round.finishedAtUtc !== undefined &&
    (round.status === 'completed' || round.status === 'cancelled')
  )
}

function getGameBoardCellPlayResultTime(round: GameBoardCellPlayResultRound) {
  const time = Date.parse(round.finishedAtUtc ?? '')
  return Number.isNaN(time) ? 0 : time
}
