import type { components } from '../../../shared/api/contracts/generated'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type ModifierResult = GameRoundDetails['modifierResults'][number]

type ModifierRuntimeState = 'scheduled' | 'running' | 'expired' | 'stopped'

interface ModifierRuntimeUnit {
  key: string
  modifierId: string
  modifierName: string
  rule: string
  performer: 'activeTeam' | 'mentor'
  requiresHostMonitoring: boolean
  activationCount: number
  durationSeconds: number | null
}

interface ModifierRuntimeClock {
  state: ModifierRuntimeState
  remainingSeconds: number | null
}

export function buildModifierRuntimeUnits(round: GameRoundDetails): ModifierRuntimeUnit[] {
  const grouped = new Map<string, ModifierRuntimeUnit>()

  for (const result of round.modifierResults) {
    const behavior = result.runtimeBehavior
    const durationSeconds = normalizeDuration(result)
    if (!behavior || (!behavior.requiresHostMonitoring && durationSeconds === null)) {
      continue
    }

    const key =
      behavior.stackingPolicy === 'aggregateParameters'
        ? `${result.modifierId}:${result.resolutionGroupId ?? 'aggregate'}`
        : result.modifierResultId
    const current = grouped.get(key)
    if (current) {
      current.activationCount += 1
      current.durationSeconds = sumOptionalDurations(current.durationSeconds, durationSeconds)
      continue
    }

    grouped.set(key, {
      key,
      modifierId: result.modifierId,
      modifierName: result.modifierName,
      rule: behavior.rule,
      performer: behavior.performer,
      requiresHostMonitoring: behavior.requiresHostMonitoring,
      activationCount: 1,
      durationSeconds,
    })
  }

  return Array.from(grouped.values())
}

export function calculateModifierRuntimeClock(
  round: Pick<
    GameRoundDetails,
    'status' | 'gameplayStartedAtUtc' | 'reviewedAtUtc' | 'finishedAtUtc'
  >,
  durationSeconds: number | null,
  serverNowMs: number,
): ModifierRuntimeClock {
  if (durationSeconds === null) {
    return { state: round.status === 'in_progress' ? 'running' : 'stopped', remainingSeconds: null }
  }
  if (!round.gameplayStartedAtUtc) {
    return { state: 'scheduled', remainingSeconds: durationSeconds }
  }

  const startedAtMs = Date.parse(round.gameplayStartedAtUtc)
  if (!Number.isFinite(startedAtMs)) {
    return { state: 'scheduled', remainingSeconds: durationSeconds }
  }
  const effectiveNowMs = getEffectiveRuntimeNow(round, serverNowMs)
  const elapsedSeconds = Math.max(0, Math.floor((effectiveNowMs - startedAtMs) / 1_000))
  const remainingSeconds = Math.max(0, durationSeconds - elapsedSeconds)

  if (remainingSeconds === 0) {
    return { state: 'expired', remainingSeconds: 0 }
  }
  return {
    state: round.status === 'in_progress' ? 'running' : 'stopped',
    remainingSeconds,
  }
}

export function createServerClockOffset(serverNowUtc: string, clientReceivedAtMs: number) {
  const serverNowMs = Date.parse(serverNowUtc)
  return Number.isFinite(serverNowMs) ? serverNowMs - clientReceivedAtMs : 0
}

export function formatRuntimeDuration(remainingSeconds: number) {
  const clamped = Math.max(0, Math.floor(remainingSeconds))
  const minutes = Math.floor(clamped / 60)
  const seconds = clamped % 60
  return `${minutes}:${String(seconds).padStart(2, '0')}`
}

function normalizeDuration(result: ModifierResult) {
  const duration = result.runtimeBehavior?.durationSecondsPerActivation
  return typeof duration === 'number' && Number.isFinite(duration) && duration > 0
    ? Math.floor(duration)
    : null
}

function sumOptionalDurations(left: number | null, right: number | null) {
  if (left === null) return right
  if (right === null) return left
  return left + right
}

function getEffectiveRuntimeNow(
  round: Pick<GameRoundDetails, 'status' | 'reviewedAtUtc' | 'finishedAtUtc'>,
  serverNowMs: number,
) {
  if (round.status === 'reviewing_results' && round.reviewedAtUtc) {
    return Date.parse(round.reviewedAtUtc)
  }
  if ((round.status === 'completed' || round.status === 'cancelled') && round.finishedAtUtc) {
    return Date.parse(round.finishedAtUtc)
  }
  return serverNowMs
}
