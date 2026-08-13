import type { TFunction } from 'i18next'
import type { GameBoardSnapshot, GameTeamQueueItem } from '../../../shared/api/contracts/index.ts'
import type { components } from '../../../shared/api/contracts/generated'
import type { AppButtonTone } from '../../../shared/ui/primitives/app-button-tone.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'
import type { GameManagementFlowStepId } from './game-management-flow.ts'

export type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

export interface RoundActionModel {
  stepNumber: number | null
  stepId: GameManagementFlowStepId | null
  statusTone: 'info' | 'warning' | 'success'
  statusLabel: string
  title: string
  description: string
  actionLabel: string | null
  actionTone: AppButtonTone
  onAction: (() => void) | null
}

export function formatManagementTeamName(
  t: TFunction,
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(
    teamName,
    t('gameBoard.teamQueueTeamTitle', { slot: teamSlotIndex }),
  )
}

export function buildRoundActionModel({
  t,
  snapshot,
  activeRound,
  hasCurrentActiveTeam,
  resumableTeam,
  onStartRound,
  onReviewRound,
  onOpenSummary,
  onResumeTeam,
}: {
  t: TFunction
  snapshot: GameBoardSnapshot
  activeRound: GameRoundDetails | null
  hasCurrentActiveTeam: boolean
  resumableTeam: GameTeamQueueItem | null
  onStartRound: (input: { cellId: string; teamId: string }) => void
  onReviewRound: (roundId: string) => void
  onOpenSummary: () => void
  onResumeTeam: (teamId: string) => void
}): RoundActionModel {
  if (snapshot.status !== 'active') {
    return {
      stepNumber: null,
      stepId: null,
      statusTone: 'info',
      statusLabel: t('gameBoard.managementLaunchTitle'),
      title: t('gameBoard.managementRoundIdleDescription'),
      description: t('gameBoard.managementActiveTeamInactive'),
      actionLabel: null,
      actionTone: 'primary',
      onAction: null,
    }
  }

  if (activeRound?.status === 'awaiting_modifiers') {
    return {
      stepNumber: 3,
      stepId: 'activate_modifiers',
      statusTone: 'warning',
      statusLabel: t('gameBoard.flowSteps.activate_modifiers.title'),
      title: t('gameBoard.flowSteps.activate_modifiers.title'),
      description: t('gameBoard.managementRoundAwaitingActionHint'),
      actionLabel: t('gameBoard.roundPanelStart'),
      actionTone: 'primary',
      onAction: () =>
        onStartRound({
          cellId: activeRound.cellId,
          teamId: activeRound.teamId,
        }),
    }
  }

  if (activeRound?.status === 'in_progress') {
    return {
      stepNumber: 5,
      stepId: 'play_round',
      statusTone: 'success',
      statusLabel: t('gameBoard.flowSteps.play_round.title'),
      title: t('gameBoard.flowSteps.play_round.title'),
      description: t('gameBoard.managementRoundInProgressHint'),
      actionLabel: t('gameBoard.roundPanelReview'),
      actionTone: 'primary',
      onAction: () => onReviewRound(activeRound.roundId),
    }
  }

  if (activeRound?.status === 'reviewing_results') {
    return {
      stepNumber: 6,
      stepId: 'review_round',
      statusTone: 'success',
      statusLabel: t('gameBoard.flowSteps.review_round.title'),
      title: t('gameBoard.flowSteps.review_round.title'),
      description: t('gameBoard.managementRoundReviewActionHint'),
      actionLabel: t('gameBoard.roundPanelOpenSummary'),
      actionTone: 'success',
      onAction: onOpenSummary,
    }
  }

  if (hasCurrentActiveTeam) {
    return {
      stepNumber: 2,
      stepId: 'select_card',
      statusTone: 'info',
      statusLabel: t('gameBoard.flowSteps.select_card.title'),
      title: t('gameBoard.flowSteps.select_card.title'),
      description: t('gameBoard.managementRoundNextActionBoardHint'),
      actionLabel: null,
      actionTone: 'primary',
      onAction: null,
    }
  }

  if (resumableTeam) {
    return {
      stepNumber: 1,
      stepId: 'select_team',
      statusTone: 'warning',
      statusLabel: t('gameBoard.flowSteps.select_team.title'),
      title: t('gameBoard.managementActiveTeamResumeAction'),
      description: t('gameBoard.managementActiveTeamResumeHint', {
        slot: resumableTeam.teamSlotIndex,
      }),
      actionLabel: t('gameBoard.managementActiveTeamResumeAction'),
      actionTone: 'primary',
      onAction: () => onResumeTeam(resumableTeam.teamId),
    }
  }

  return {
    stepNumber: 1,
    stepId: 'select_team',
    statusTone: 'warning',
    statusLabel: t('gameBoard.flowSteps.select_team.title'),
    title: t('gameBoard.flowSteps.select_team.title'),
    description: t('gameBoard.managementRoundNextActionTeamHint'),
    actionLabel: null,
    actionTone: 'primary',
    onAction: null,
  }
}
