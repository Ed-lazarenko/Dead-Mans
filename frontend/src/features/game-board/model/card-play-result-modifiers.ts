import type { components } from '../../../shared/api/contracts/generated'

type CardPlayResultModifier = components['schemas']['GameHistoryRoundModifierItemDto']

export interface GroupedCardPlayResultModifier {
  modifierId: string
  modifierName: string
  count: number
  scoreDelta: number
  killDelta: number
  outcomeStatuses: readonly ModifierOutcomeStatusSummary[]
  multiplierAppliedValues: readonly number[]
  calculation: CardPlayResultModifierCalculation | null
}

export interface ModifierOutcomeStatusSummary {
  status: string
  count: number
}

export interface CardPlayResultModifierCalculation {
  source: string | null
  effect: string | null
  conditionType: string | null
  conditionMet: boolean | null
  input: string | null
  countValue: number | null
  killDeltaValue: number | null
  multiplierDelta: number | null
  killsCount: number | null
  bountyCount: number | null
  activationCount: number | null
  perKillBonus: number | null
  failurePenaltyPoints: number | null
  formulaMode: string | null
  successExpression: string | null
  failureExpression: string | null
}

export function groupCardPlayResultModifiers(
  modifiers: readonly CardPlayResultModifier[],
): GroupedCardPlayResultModifier[] {
  const grouped = new Map<string, GroupedCardPlayResultModifier>()

  for (const modifier of modifiers) {
    const current = grouped.get(modifier.modifierId)
    if (!current) {
      grouped.set(modifier.modifierId, {
        modifierId: modifier.modifierId,
        modifierName: modifier.modifierName,
        count: 1,
        scoreDelta: modifier.scoreDelta,
        killDelta: modifier.killDelta,
        outcomeStatuses: [{ status: modifier.outcomeStatus, count: 1 }],
        multiplierAppliedValues:
          modifier.multiplierApplied === null || modifier.multiplierApplied === undefined
            ? []
            : [modifier.multiplierApplied],
        calculation: parseCardPlayResultModifierCalculation(modifier.resolutionDataJson),
      })
      continue
    }

    grouped.set(modifier.modifierId, {
      ...current,
      count: current.count + 1,
      scoreDelta: current.scoreDelta + modifier.scoreDelta,
      killDelta: current.killDelta + modifier.killDelta,
      outcomeStatuses: mergeOutcomeStatusSummaries(current.outcomeStatuses, modifier.outcomeStatus),
      multiplierAppliedValues: mergeMultiplierValues(
        current.multiplierAppliedValues,
        modifier.multiplierApplied,
      ),
      calculation:
        current.calculation ?? parseCardPlayResultModifierCalculation(modifier.resolutionDataJson),
    })
  }

  return Array.from(grouped.values()).map(normalizeGroupedModifierCalculation)
}

function mergeOutcomeStatusSummaries(
  statuses: readonly ModifierOutcomeStatusSummary[],
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

function mergeMultiplierValues(values: readonly number[], nextValue: number | null | undefined) {
  if (nextValue === null || nextValue === undefined || values.includes(nextValue)) {
    return values
  }

  return [...values, nextValue]
}

function parseCardPlayResultModifierCalculation(
  resolutionDataJson: string | null | undefined,
): CardPlayResultModifierCalculation | null {
  if (!resolutionDataJson) {
    return null
  }

  try {
    const data: unknown = JSON.parse(resolutionDataJson)
    if (!isRecord(data)) {
      return null
    }

    return {
      source: getString(data, 'source'),
      effect: getString(data, 'effect'),
      conditionType: getString(data, 'conditionType'),
      conditionMet: getBoolean(data, 'conditionMet'),
      input: getString(data, 'input'),
      countValue: getNumber(data, 'countValue'),
      killDeltaValue: getNumber(data, 'killDeltaValue'),
      multiplierDelta: getNumber(data, 'multiplierDelta'),
      killsCount: getNumber(data, 'killsCount'),
      bountyCount: getNumber(data, 'bountyCount'),
      activationCount: getNumber(data, 'activationCount'),
      perKillBonus: getNumber(data, 'perKillBonus'),
      failurePenaltyPoints: getNumber(data, 'failurePenaltyPoints'),
      formulaMode: getString(data, 'autoResultFormula'),
      successExpression: getString(data, 'autoResultSuccessExpression'),
      failureExpression: getString(data, 'autoResultFailureExpression'),
    }
  } catch {
    return null
  }
}

function normalizeGroupedModifierCalculation(
  modifier: GroupedCardPlayResultModifier,
): GroupedCardPlayResultModifier {
  if (!modifier.calculation) {
    return modifier
  }

  return {
    ...modifier,
    calculation: {
      ...modifier.calculation,
      activationCount:
        modifier.calculation.activationCount === null
          ? null
          : Math.max(modifier.calculation.activationCount, modifier.count),
    },
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function getString(record: Record<string, unknown>, key: string) {
  const value = record[key]
  return typeof value === 'string' && value.trim() ? value : null
}

function getNumber(record: Record<string, unknown>, key: string) {
  const value = record[key]
  return typeof value === 'number' && Number.isFinite(value) ? value : null
}

function getBoolean(record: Record<string, unknown>, key: string) {
  const value = record[key]
  return typeof value === 'boolean' ? value : null
}
