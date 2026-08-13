import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

type GameManagementPhase = 'ready' | 'active_idle' | 'round_running' | 'reviewing' | 'finished'

export type GameManagementFlowStepId =
  | 'select_team'
  | 'select_card'
  | 'activate_modifiers'
  | 'start_round'
  | 'play_round'
  | 'review_round'

type GameManagementFlowStepState = 'complete' | 'current' | 'ready' | 'upcoming' | 'blocked'
type GameManagementFlowSummaryKey =
  | 'gameBoard.flowSummary.waitingForLaunch'
  | 'gameBoard.flowSummary.finished'
  | 'gameBoard.flowSummary.selectActiveTeam'
  | 'gameBoard.flowSummary.selectCard'
  | 'gameBoard.flowSummary.awaitingModifiers'
  | 'gameBoard.flowSummary.roundInProgress'
  | 'gameBoard.flowSummary.reviewingResults'
  | 'gameBoard.flowSummary.noCardsLeft'
type GameManagementFlowStepTitleKey =
  | 'gameBoard.flowSteps.select_team.title'
  | 'gameBoard.flowSteps.select_card.title'
  | 'gameBoard.flowSteps.activate_modifiers.title'
  | 'gameBoard.flowSteps.start_round.title'
  | 'gameBoard.flowSteps.play_round.title'
  | 'gameBoard.flowSteps.review_round.title'
type GameManagementFlowStepDescriptionKey =
  | 'gameBoard.flowSteps.select_team.description'
  | 'gameBoard.flowSteps.select_card.description'
  | 'gameBoard.flowSteps.activate_modifiers.description'
  | 'gameBoard.flowSteps.start_round.description'
  | 'gameBoard.flowSteps.play_round.description'
  | 'gameBoard.flowSteps.review_round.description'

interface GameManagementFlowStep {
  id: GameManagementFlowStepId
  state: GameManagementFlowStepState
  titleKey: GameManagementFlowStepTitleKey
  descriptionKey: GameManagementFlowStepDescriptionKey
}

interface GameManagementFlowModel {
  phase: GameManagementPhase
  summaryKey: GameManagementFlowSummaryKey
  currentStepId: GameManagementFlowStepId | null
  nextStepId: GameManagementFlowStepId | null
  steps: GameManagementFlowStep[]
}

const flowStepOrder: readonly GameManagementFlowStepId[] = [
  'select_team',
  'select_card',
  'activate_modifiers',
  'start_round',
  'play_round',
  'review_round',
]

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
    return createFlowModel(
      isGameReady ? 'ready' : 'finished',
      isGameReady ? 'gameBoard.flowSummary.waitingForLaunch' : 'gameBoard.flowSummary.finished',
      isGameReady ? null : null,
      isGameReady ? 'select_team' : null,
      {
        select_team: 'blocked',
        select_card: 'blocked',
        activate_modifiers: 'blocked',
        start_round: 'blocked',
        play_round: 'blocked',
        review_round: 'blocked',
      },
    )
  }

  if (activeRound?.status === 'awaiting_modifiers') {
    return createFlowModel(
      'round_running',
      'gameBoard.flowSummary.awaitingModifiers',
      'activate_modifiers',
      'start_round',
      {
        select_team: 'complete',
        select_card: 'complete',
        activate_modifiers: 'current',
        start_round: 'ready',
        play_round: 'upcoming',
        review_round: 'upcoming',
      },
    )
  }

  if (activeRound?.status === 'in_progress') {
    return createFlowModel(
      'round_running',
      'gameBoard.flowSummary.roundInProgress',
      'play_round',
      'review_round',
      {
        select_team: 'complete',
        select_card: 'complete',
        activate_modifiers: 'complete',
        start_round: 'complete',
        play_round: 'current',
        review_round: 'ready',
      },
    )
  }

  if (activeRound?.status === 'reviewing_results') {
    return createFlowModel(
      'reviewing',
      'gameBoard.flowSummary.reviewingResults',
      'review_round',
      null,
      {
        select_team: 'complete',
        select_card: 'complete',
        activate_modifiers: 'complete',
        start_round: 'complete',
        play_round: 'complete',
        review_round: 'current',
      },
    )
  }

  if (!hasActiveTeam) {
    return createFlowModel(
      'active_idle',
      'gameBoard.flowSummary.selectActiveTeam',
      'select_team',
      null,
      {
        select_team: 'current',
        select_card: 'blocked',
        activate_modifiers: 'blocked',
        start_round: 'blocked',
        play_round: 'blocked',
        review_round: 'blocked',
      },
    )
  }

  if (!hasAvailableCells) {
    return createFlowModel(
      'active_idle',
      'gameBoard.flowSummary.noCardsLeft',
      'select_card',
      null,
      {
        select_team: 'complete',
        select_card: 'blocked',
        activate_modifiers: 'blocked',
        start_round: 'blocked',
        play_round: 'blocked',
        review_round: 'blocked',
      },
    )
  }

  return createFlowModel(
    'active_idle',
    'gameBoard.flowSummary.selectCard',
    'select_card',
    'activate_modifiers',
    {
      select_team: 'complete',
      select_card: 'current',
      activate_modifiers: 'upcoming',
      start_round: 'upcoming',
      play_round: 'upcoming',
      review_round: 'upcoming',
    },
  )
}

function createFlowModel(
  phase: GameManagementPhase,
  summaryKey: GameManagementFlowSummaryKey,
  currentStepId: GameManagementFlowStepId | null,
  nextStepId: GameManagementFlowStepId | null,
  states: Record<GameManagementFlowStepId, GameManagementFlowStepState>,
): GameManagementFlowModel {
  return {
    phase,
    summaryKey,
    currentStepId,
    nextStepId,
    steps: flowStepOrder.map((id) => createStep(id, states[id])),
  }
}

function createStep(
  id: GameManagementFlowStepId,
  state: GameManagementFlowStepState,
): GameManagementFlowStep {
  const stepKeys = {
    select_team: {
      titleKey: 'gameBoard.flowSteps.select_team.title',
      descriptionKey: 'gameBoard.flowSteps.select_team.description',
    },
    select_card: {
      titleKey: 'gameBoard.flowSteps.select_card.title',
      descriptionKey: 'gameBoard.flowSteps.select_card.description',
    },
    activate_modifiers: {
      titleKey: 'gameBoard.flowSteps.activate_modifiers.title',
      descriptionKey: 'gameBoard.flowSteps.activate_modifiers.description',
    },
    start_round: {
      titleKey: 'gameBoard.flowSteps.start_round.title',
      descriptionKey: 'gameBoard.flowSteps.start_round.description',
    },
    play_round: {
      titleKey: 'gameBoard.flowSteps.play_round.title',
      descriptionKey: 'gameBoard.flowSteps.play_round.description',
    },
    review_round: {
      titleKey: 'gameBoard.flowSteps.review_round.title',
      descriptionKey: 'gameBoard.flowSteps.review_round.description',
    },
  } as const

  return {
    id,
    state,
    ...stepKeys[id],
  }
}
