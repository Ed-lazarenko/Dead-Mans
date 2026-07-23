import { z } from 'zod'
import type { components } from '../../../shared/api/contracts/generated'

type GameCardRunDetails = components['schemas']['GameCardRunDetailsDto']

export const gameCardRunModifierOutcomeStatuses = ['completed', 'failed', 'cancelled'] as const

const modifierSummarySchema = z.object({
  modifierResultId: z.string().min(1),
  modifierName: z.string().min(1),
  outcomeStatus: z.enum(gameCardRunModifierOutcomeStatuses),
  scoreDelta: z.coerce.number().int(),
  killDelta: z.coerce.number().int(),
})

export const gameCardRunSummaryFormSchema = z.object({
  killsCount: z.coerce.number().int().min(0),
  bountyCount: z.coerce.number().int().min(0),
  modifiers: z.array(modifierSummarySchema),
})

export type GameCardRunSummaryFormValues = z.infer<typeof gameCardRunSummaryFormSchema>

export interface CompleteRoundInput {
  cardRunId: string
  finalScore: number
  killsCount: number
  bountyCount: number
  modifierResults: Array<{
    modifierResultId: string
    outcomeStatus: string
    scoreDelta: number
    killDelta: number
    multiplierApplied: null
    resolutionDataJson: null
  }>
}

export function buildGameCardRunSummaryDefaultValues(
  activeRun: GameCardRunDetails,
): GameCardRunSummaryFormValues {
  return {
    killsCount: activeRun.killsCount,
    bountyCount: activeRun.bountyCount,
    modifiers: activeRun.modifierResults.map((modifier) => ({
      modifierResultId: modifier.modifierResultId,
      modifierName: modifier.modifierName,
      outcomeStatus: 'completed' as const,
      scoreDelta: modifier.scoreDelta,
      killDelta: modifier.killDelta,
    })),
  }
}

export function buildCompleteRoundInput(
  activeRun: GameCardRunDetails,
  values: GameCardRunSummaryFormValues,
): CompleteRoundInput {
  const preview = buildGameCardRunScorePreview(activeRun.baseScore, values)

  return {
    cardRunId: activeRun.cardRunId,
    finalScore: preview.finalScore,
    killsCount: values.killsCount,
    bountyCount: values.bountyCount,
    modifierResults: values.modifiers.map((modifier) => ({
      modifierResultId: modifier.modifierResultId,
      outcomeStatus: modifier.outcomeStatus,
      scoreDelta: modifier.scoreDelta,
      killDelta: modifier.killDelta,
      multiplierApplied: null,
      resolutionDataJson: null,
    })),
  }
}

export function buildGameCardRunScorePreview(
  scoreUnit: number,
  values: Pick<GameCardRunSummaryFormValues, 'killsCount' | 'bountyCount' | 'modifiers'>,
) {
  const modifierKillDelta = values.modifiers.reduce((total, modifier) => total + modifier.killDelta, 0)
  const modifierScoreDelta = values.modifiers.reduce(
    (total, modifier) => total + modifier.scoreDelta,
    0,
  )
  const killsScore = values.killsCount * scoreUnit
  const bountyScore = values.bountyCount * scoreUnit
  const modifierKillScore = modifierKillDelta * scoreUnit
  const finalScore = killsScore + bountyScore + modifierKillScore + modifierScoreDelta

  return {
    scoreUnit,
    killsScore,
    bountyScore,
    modifierKillDelta,
    modifierKillScore,
    modifierScoreDelta,
    totalKillCount: values.killsCount + modifierKillDelta,
    finalScore,
  }
}
