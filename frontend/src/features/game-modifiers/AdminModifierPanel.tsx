import { Box, Chip, Stack, Typography } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierAdminPlayer,
  GameModifierAdminPlayersResult,
} from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AppToast, ConfirmDialog, SectionCard } from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import {
  adminActivateGameModifier,
  cancelGameModifierActivation,
  emergencyDisableGameModifier,
} from './api/game-modifiers-api.ts'
import {
  adminGameModifierActivationsQueryOptions,
  adminGameModifierPlayersQueryOptions,
  adminGameModifierStateQueryOptions,
  gameModifierQueryKeys,
} from './api/game-modifier-queries.ts'
import {
  buildCancelModifierOptions,
  resolveAdminActivateErrorKey,
  resolveAdminCancelErrorKey,
} from './model/admin-modifier-support.ts'
import { AdminModifierActivationBlock } from './ui/AdminModifierActivationBlock.tsx'
import { AdminModifierCancellationBlock } from './ui/AdminModifierCancellationBlock.tsx'
import { AdminModifierHint, AdminModifierMetric } from './ui/admin-modifier-panel-primitives.tsx'

const emptyAdminPlayers: readonly GameModifierAdminPlayer[] = []
const emptyAdminPlayersSummary: GameModifierAdminPlayersResult['summary'] = {
  playersCount: 0,
  totalAvailableQuizPoints: 0,
  totalEarnedQuizPoints: 0,
  totalSpentQuizPoints: 0,
}

export function AdminModifierTool() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [selectedPlayerId, setSelectedPlayerId] = useState('')
  const [selectedAvailableModifierId, setSelectedAvailableModifierId] = useState('')
  const [selectedCancelModifierId, setSelectedCancelModifierId] = useState('')
  const [selectedActivationId, setSelectedActivationId] = useState('')
  const [cancelReason, setCancelReason] = useState('')
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false)
  const [emergencyDisableReason, setEmergencyDisableReason] = useState('')
  const [isEmergencyDisableConfirmOpen, setIsEmergencyDisableConfirmOpen] = useState(false)
  const [toastMessage, setToastMessage] = useState<string | null>(null)
  const [toastSeverity, setToastSeverity] = useState<'info' | 'error'>('info')

  const isAdmin = user?.roles.includes('admin') ?? false
  const adminPlayersQuery = useQuery({
    ...adminGameModifierPlayersQueryOptions,
    enabled: isAdmin,
  })
  const adminActivationsQuery = useQuery({
    ...adminGameModifierActivationsQueryOptions,
    enabled: isAdmin,
  })
  const players = adminPlayersQuery.data?.players ?? emptyAdminPlayers
  const summary = adminPlayersQuery.data?.summary ?? emptyAdminPlayersSummary
  const effectiveSelectedPlayerId =
    selectedPlayerId.length > 0 && players.some((player) => player.userId === selectedPlayerId)
      ? selectedPlayerId
      : (players[0]?.userId ?? '')
  const selectedPlayer =
    players.find((player) => player.userId === effectiveSelectedPlayerId) ?? null

  const adminStateQuery = useQuery({
    ...adminGameModifierStateQueryOptions(effectiveSelectedPlayerId),
    enabled: isAdmin && effectiveSelectedPlayerId.length > 0,
  })

  const invalidateModifierCaches = () => {
    void queryClient.invalidateQueries({ queryKey: gameModifierQueryKeys.all })
    void queryClient.invalidateQueries({ queryKey: currentGameBoardQueryOptions.queryKey })
  }

  const activateMutation = useMutation({
    mutationFn: (input: { modifierId: string; playerId: string }) =>
      adminActivateGameModifier(input.modifierId, input.playerId),
    onSuccess: () => {
      setToastSeverity('info')
      setToastMessage(t('gameModifiers.adminPanel.activateSuccess'))
      setSelectedAvailableModifierId('')
      invalidateModifierCaches()
    },
    onError: (error) => {
      setToastSeverity('error')
      setToastMessage(t(resolveAdminActivateErrorKey(error)))
      invalidateModifierCaches()
    },
  })

  const cancelMutation = useMutation({
    mutationFn: (input: { activationId: string; roundVersion: number; reason: string }) =>
      cancelGameModifierActivation(input.activationId, input.roundVersion, input.reason),
    onSuccess: () => {
      setIsCancelConfirmOpen(false)
      setToastSeverity('info')
      setToastMessage(t('gameModifiers.adminPanel.cancelSuccess'))
      setSelectedCancelModifierId('')
      setSelectedActivationId('')
      setCancelReason('')
      invalidateModifierCaches()
    },
    onError: (error) => {
      setToastSeverity('error')
      setToastMessage(t(resolveAdminCancelErrorKey(error)))
      invalidateModifierCaches()
    },
  })

  const emergencyDisableMutation = useMutation({
    mutationFn: (input: { modifierId: string; reason: string }) =>
      emergencyDisableGameModifier(input.modifierId, input.reason),
    onSuccess: () => {
      setIsEmergencyDisableConfirmOpen(false)
      setEmergencyDisableReason('')
      setToastSeverity('info')
      setToastMessage(t('gameModifiers.adminPanel.emergencyDisableSuccess'))
      invalidateModifierCaches()
    },
    onError: () => {
      setToastSeverity('error')
      setToastMessage(t('gameModifiers.adminPanel.emergencyDisableError'))
      invalidateModifierCaches()
    },
  })

  if (!isAdmin) {
    return null
  }

  const state = adminStateQuery.data ?? null
  const activeActivations = adminActivationsQuery.data ?? []
  const effectiveSelectedAvailableModifierId =
    selectedAvailableModifierId.length > 0 &&
    (state?.availableModifiers.some((item) => item.modifier.id === selectedAvailableModifierId) ??
      false)
      ? selectedAvailableModifierId
      : ''
  const effectiveSelectedCancelModifierId =
    selectedCancelModifierId.length > 0 &&
    activeActivations.some((item) => item.modifierId === selectedCancelModifierId)
      ? selectedCancelModifierId
      : ''
  const cancelModifierOptions = buildCancelModifierOptions(activeActivations)
  const selectedAvailableModifier =
    state?.availableModifiers.find(
      (item) => item.modifier.id === effectiveSelectedAvailableModifierId,
    ) ?? null
  const selectedCancelModifier =
    cancelModifierOptions.find((item) => item.modifierId === effectiveSelectedCancelModifierId) ??
    null
  const cancelActivationOptions = activeActivations
    .filter((item) => item.modifierId === effectiveSelectedCancelModifierId)
    .sort((left, right) => right.activatedAtUtc.localeCompare(left.activatedAtUtc))
  const effectiveSelectedActivationId =
    selectedActivationId.length > 0 &&
    cancelActivationOptions.some((item) => item.activationId === selectedActivationId)
      ? selectedActivationId
      : ''
  const selectedActivation =
    cancelActivationOptions.find((item) => item.activationId === effectiveSelectedActivationId) ??
    null
  const isBusy =
    activateMutation.isPending || cancelMutation.isPending || emergencyDisableMutation.isPending

  return (
    <>
      <Stack data-testid="modifier-management-tool" spacing={2}>
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          <Chip
            color="warning"
            variant="outlined"
            label={t('gameModifiers.adminPanel.summaryUsedCount', {
              count: activeActivations.length,
            })}
          />
        </Stack>

        <SectionCard inset sx={{ p: 1.5 }}>
          <Stack spacing={1}>
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography variant="subtitle2">
                {t('gameModifiers.adminPanel.summaryTitle')}
              </Typography>
              <AdminModifierHint title={t('gameModifiers.adminPanel.summaryTooltip')} />
            </Stack>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
                gap: 1,
              }}
            >
              <AdminModifierMetric
                label={t('gameModifiers.adminPanel.summaryAvailablePoints')}
                value={t('gameModifiers.myPointsValue', {
                  points: summary.totalAvailableQuizPoints,
                })}
              />
              <AdminModifierMetric
                label={t('gameModifiers.adminPanel.summarySpentPoints')}
                value={t('gameModifiers.myPointsValue', {
                  points: summary.totalSpentQuizPoints,
                })}
              />
              <AdminModifierMetric
                label={t('gameModifiers.adminPanel.summaryEarnedPoints')}
                value={t('gameModifiers.myPointsValue', {
                  points: summary.totalEarnedQuizPoints,
                })}
              />
              <AdminModifierMetric
                label={t('gameModifiers.adminPanel.summaryUsedLabel')}
                value={String(activeActivations.length)}
              />
            </Box>
          </Stack>
        </SectionCard>

        <AdminModifierActivationBlock
          players={players}
          selectedPlayer={selectedPlayer}
          state={state}
          selectedModifier={selectedAvailableModifier}
          isPlayersLoading={adminPlayersQuery.isLoading}
          isPlayersError={adminPlayersQuery.isError}
          isStateLoading={adminStateQuery.isLoading}
          isStateError={adminStateQuery.isError}
          isBusy={isBusy}
          isActivating={activateMutation.isPending}
          isEmergencyDisabling={emergencyDisableMutation.isPending}
          emergencyDisableReason={emergencyDisableReason}
          onPlayerChange={(playerId) => {
            setSelectedPlayerId(playerId)
            setSelectedAvailableModifierId('')
          }}
          onModifierChange={(modifierId) => {
            setSelectedAvailableModifierId(modifierId)
            setEmergencyDisableReason('')
          }}
          onEmergencyDisableReasonChange={setEmergencyDisableReason}
          onActivate={() => {
            if (!effectiveSelectedPlayerId || !effectiveSelectedAvailableModifierId) {
              return
            }

            activateMutation.mutate({
              modifierId: effectiveSelectedAvailableModifierId,
              playerId: effectiveSelectedPlayerId,
            })
          }}
          onRequestEmergencyDisable={() => setIsEmergencyDisableConfirmOpen(true)}
        />

        <AdminModifierCancellationBlock
          activeActivations={activeActivations}
          modifierOptions={cancelModifierOptions}
          selectedModifier={selectedCancelModifier}
          activationOptions={cancelActivationOptions}
          selectedActivation={selectedActivation}
          cancelReason={cancelReason}
          isLoading={adminActivationsQuery.isLoading}
          isError={adminActivationsQuery.isError}
          isBusy={isBusy}
          isCancelling={cancelMutation.isPending}
          onModifierChange={(modifierId) => {
            setSelectedCancelModifierId(modifierId)
            setSelectedActivationId('')
            setCancelReason('')
          }}
          onActivationChange={(activationId) => {
            setSelectedActivationId(activationId)
            setCancelReason('')
          }}
          onCancelReasonChange={setCancelReason}
          onRequestCancel={() => setIsCancelConfirmOpen(true)}
        />
      </Stack>

      <ConfirmDialog
        open={isEmergencyDisableConfirmOpen}
        title={t('gameModifiers.adminPanel.emergencyDisableConfirmTitle')}
        description={
          selectedAvailableModifier
            ? t('gameModifiers.adminPanel.emergencyDisableConfirmDescription', {
                modifier: selectedAvailableModifier.modifier.name,
              })
            : ''
        }
        confirmLabel={t('gameModifiers.adminPanel.emergencyDisableAction')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        isBusy={emergencyDisableMutation.isPending}
        onClose={() => setIsEmergencyDisableConfirmOpen(false)}
        onConfirm={() => {
          if (!selectedAvailableModifier || emergencyDisableReason.trim().length === 0) {
            return
          }

          emergencyDisableMutation.mutate({
            modifierId: selectedAvailableModifier.modifier.id,
            reason: emergencyDisableReason.trim(),
          })
        }}
      />

      <ConfirmDialog
        open={isCancelConfirmOpen}
        title={t('gameModifiers.adminPanel.cancelConfirmTitle')}
        description={
          selectedActivation
            ? t('gameModifiers.adminPanel.cancelConfirmDescription', {
                modifier: selectedActivation.modifierName,
                player: selectedActivation.activatedByDisplayName,
                cost: selectedActivation.activationCost,
              })
            : ''
        }
        confirmLabel={t('gameModifiers.adminPanel.cancelAction')}
        cancelLabel={t('gameModifiers.adminPanel.cancelConfirmCancel')}
        confirmTone="danger"
        isBusy={cancelMutation.isPending}
        onClose={() => setIsCancelConfirmOpen(false)}
        onConfirm={() => {
          if (!selectedActivation || cancelReason.trim().length === 0) {
            return
          }

          cancelMutation.mutate({
            activationId: selectedActivation.activationId,
            roundVersion: selectedActivation.roundVersion,
            reason: cancelReason.trim(),
          })
        }}
      />

      <AppToast
        message={toastMessage}
        onClose={() => setToastMessage(null)}
        severity={toastSeverity}
        autoHideDuration={4000}
      />
    </>
  )
}
