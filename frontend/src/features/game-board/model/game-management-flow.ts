import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

export type GameManagementFlowStepId =
  | 'select_team'
  | 'select_card'
  | 'activate_modifiers'
  | 'start_round'
  | 'play_round'
  | 'review_round'

export type GameManagementFlowStepState = 'complete' | 'current' | 'ready' | 'upcoming' | 'blocked'

export interface GameManagementFlowStep {
  id: GameManagementFlowStepId
  state: GameManagementFlowStepState
  titleKey: string
  descriptionKey: string
}

export interface GameManagementFlowModel {
  summaryKey: string
  steps: GameManagementFlowStep[]
}

export function buildGameManagementFlow(
  snapshot: GameBoardSnapshot,
  activeRound: GameRoundDetails | null,
): GameManagementFlowModel {
  const currentActiveTeamId = activeRound?.teamId ?? snapshot.activeTeamId ?? null
  const hasActiveTeam = currentActiveTeamId != null
  const hasAvailableCells = snapshot.cells.some((cell) => cell.state !== 'open')
  const isGameActive = snapshot.status === 'active'
  const isGameReady = snapshot.status === 'ready'

  if (!isGameActive) {
    return {
      summaryKey: isGameReady
        ? 'gameBoard.flowSummary.waitingForLaunch'
        : 'gameBoard.flowSummary.finished',
      steps: [
        createStep('select_team', 'blocked'),
        createStep('select_card', 'blocked'),
        createStep('activate_modifiers', 'blocked'),
        createStep('start_round', 'blocked'),
        createStep('play_round', 'blocked'),
        createStep('review_round', 'blocked'),
      ],
    }
  }

  if (activeRound?.status === 'awaiting_modifiers') {
    return {
      summaryKey: 'gameBoard.flowSummary.awaitingModifiers',
      steps: [
        createStep('select_team', 'complete'),
        createStep('select_card', 'complete'),
        createStep('activate_modifiers', 'current'),
        createStep('start_round', 'ready'),
        createStep('play_round', 'upcoming'),
        createStep('review_round', 'upcoming'),
      ],
    }
  }

  if (activeRound?.status === 'in_progress') {
    return {
      summaryKey: 'gameBoard.flowSummary.roundInProgress',
      steps: [
        createStep('select_team', 'complete'),
        createStep('select_card', 'complete'),
        createStep('activate_modifiers', 'complete'),
        createStep('start_round', 'complete'),
        createStep('play_round', 'current'),
        createStep('review_round', 'ready'),
      ],
    }
  }

  if (activeRound?.status === 'reviewing_results') {
    return {
      summaryKey: 'gameBoard.flowSummary.reviewingResults',
      steps: [
        createStep('select_team', 'complete'),
        createStep('select_card', 'complete'),
        createStep('activate_modifiers', 'complete'),
        createStep('start_round', 'complete'),
        createStep('play_round', 'complete'),
        createStep('review_round', 'current'),
      ],
    }
  }

  if (!hasActiveTeam) {
    return {
      summaryKey: 'gameBoard.flowSummary.selectActiveTeam',
      steps: [
        createStep('select_team', 'current'),
        createStep('select_card', 'blocked'),
        createStep('activate_modifiers', 'blocked'),
        createStep('start_round', 'blocked'),
        createStep('play_round', 'blocked'),
        createStep('review_round', 'blocked'),
      ],
    }
  }

  if (!hasAvailableCells) {
    return {
      summaryKey: 'gameBoard.flowSummary.noCardsLeft',
      steps: [
        createStep('select_team', 'complete'),
        createStep('select_card', 'blocked'),
        createStep('activate_modifiers', 'blocked'),
        createStep('start_round', 'blocked'),
        createStep('play_round', 'blocked'),
        createStep('review_round', 'blocked'),
      ],
    }
  }

  return {
    summaryKey: 'gameBoard.flowSummary.selectCard',
    steps: [
      createStep('select_team', 'complete'),
      createStep('select_card', 'current'),
      createStep('activate_modifiers', 'upcoming'),
      createStep('start_round', 'upcoming'),
      createStep('play_round', 'upcoming'),
      createStep('review_round', 'upcoming'),
    ],
  }
}

function createStep(
  id: GameManagementFlowStepId,
  state: GameManagementFlowStepState,
): GameManagementFlowStep {
  return {
    id,
    state,
    titleKey: `gameBoard.flowSteps.${id}.title`,
    descriptionKey: `gameBoard.flowSteps.${id}.description`,
  }
}
