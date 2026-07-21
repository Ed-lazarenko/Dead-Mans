import { Autocomplete, Box, Chip, Stack, TextField, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  ErrorResponse,
  GameModifierActivation,
} from '../../shared/api/contracts/index.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AppButton, AppToast, SectionCard } from '../../shared/ui/index.ts'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import {
  adminActivateGameModifier,
  cancelGameModifierActivation,
} from './api/game-modifiers-api.ts'
import {
  adminGameModifierActivationsQueryOptions,
  adminGameModifierPlayersQueryOptions,
  adminGameModifierStateQueryOptions,
  gameModifierQueryKeys,
} from './api/game-modifier-queries.ts'

interface CancelModifierOption {
  modifierId: string
  modifierName: string
}

export function AdminModifierPanel() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [selectedPlayerId, setSelectedPlayerId] = useState('')
  const [selectedAvailableModifierId, setSelectedAvailableModifierId] = useState('')
  const [selectedCancelModifierId, setSelectedCancelModifierId] = useState('')
  const [selectedActivationId, setSelectedActivationId] = useState('')
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
  const players = adminPlayersQuery.data ?? []
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
    mutationFn: (activationId: string) => cancelGameModifierActivation(activationId),
    onSuccess: () => {
      setToastSeverity('info')
      setToastMessage(t('gameModifiers.adminPanel.cancelSuccess'))
      setSelectedCancelModifierId('')
      setSelectedActivationId('')
      invalidateModifierCaches()
    },
    onError: (error) => {
      setToastSeverity('error')
      setToastMessage(t(resolveAdminCancelErrorKey(error)))
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
  const isBusy = activateMutation.isPending || cancelMutation.isPending

  return (
    <>
      <SectionCard sx={{ p: 1.5 }}>
        <Stack spacing={1.25}>
          <Box>
            <Typography variant="subtitle2">{t('gameModifiers.adminPanel.title')}</Typography>
            <Typography variant="caption" color="text.secondary">
              {t('gameModifiers.adminPanel.description')}
            </Typography>
          </Box>

          <AdminBlock
            title={t('gameModifiers.adminPanel.activateLabel')}
            hint={t('gameModifiers.adminPanel.activateHint')}
          >
            {adminPlayersQuery.isLoading ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.adminPanel.stateLoading')}
              </Typography>
            ) : adminPlayersQuery.isError ? (
              <Typography variant="body2" color="error.main">
                {t('gameModifiers.errorLoading')}
              </Typography>
            ) : players.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.adminPanel.noPlayers')}
              </Typography>
            ) : (
              <Stack spacing={1}>
                <Autocomplete
                  size="small"
                  options={players}
                  value={selectedPlayer}
                  onChange={(_event, value) => {
                    setSelectedPlayerId(value?.userId ?? '')
                    setSelectedAvailableModifierId('')
                  }}
                  getOptionLabel={(option) =>
                    t('gameModifiers.adminPanel.playerOption', {
                      player: option.displayName,
                      login: option.login,
                      points: option.availableQuizPoints,
                    })
                  }
                  isOptionEqualToValue={(option, value) => option.userId === value.userId}
                  disabled={isBusy}
                  renderInput={(params) => (
                    <TextField {...params} label={t('gameModifiers.adminPanel.playerLabel')} />
                  )}
                />

                {adminStateQuery.isLoading ? (
                  <Typography variant="body2" color="text.secondary">
                    {t('gameModifiers.adminPanel.stateLoading')}
                  </Typography>
                ) : adminStateQuery.isError ? (
                  <Typography variant="body2" color="error.main">
                    {t('gameModifiers.adminPanel.stateError')}
                  </Typography>
                ) : state == null ? null : (
                  <>
                    <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                      <Chip
                        size="small"
                        variant="outlined"
                        label={t('gameModifiers.adminPanel.pointsAvailable', {
                          points: state.availableQuizPoints,
                        })}
                      />
                      <Chip
                        size="small"
                        variant="outlined"
                        label={t('gameModifiers.adminPanel.pointsEarned', {
                          points: state.earnedQuizPoints,
                        })}
                      />
                      <Chip
                        size="small"
                        variant="outlined"
                        label={t('gameModifiers.adminPanel.pointsSpent', {
                          points: state.spentQuizPoints,
                        })}
                      />
                    </Stack>

                    {state.availableModifiers.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">
                        {t('gameModifiers.adminPanel.noAvailableModifiers')}
                      </Typography>
                    ) : (
                      <>
                        <Autocomplete
                          size="small"
                          options={state.availableModifiers}
                          value={selectedAvailableModifier}
                          onChange={(_event, value) =>
                            setSelectedAvailableModifierId(value?.modifier.id ?? '')
                          }
                          getOptionLabel={(option) =>
                            `${option.modifier.name} · ${t('gameModifiers.costLabel', {
                              cost: option.modifier.activationCost,
                            })}`
                          }
                          isOptionEqualToValue={(option, value) =>
                            option.modifier.id === value.modifier.id
                          }
                          disabled={isBusy}
                          renderInput={(params) => (
                            <TextField
                              {...params}
                              label={t('gameModifiers.adminPanel.activateLabel')}
                            />
                          )}
                        />

                        {selectedAvailableModifier?.blockedReason ? (
                          <Typography variant="caption" color="text.secondary">
                            {t(
                              `gameModifiers.blockedReasons.${selectedAvailableModifier.blockedReason}`,
                            )}
                          </Typography>
                        ) : null}

                        <AppButton
                          tone="primary"
                          size="small"
                          fullWidth
                          disabled={
                            isBusy ||
                            effectiveSelectedPlayerId.length === 0 ||
                            effectiveSelectedAvailableModifierId.length === 0 ||
                            selectedAvailableModifier?.canActivate !== true
                          }
                          onClick={() => {
                            if (
                              !effectiveSelectedPlayerId ||
                              !effectiveSelectedAvailableModifierId
                            ) {
                              return
                            }

                            activateMutation.mutate({
                              modifierId: effectiveSelectedAvailableModifierId,
                              playerId: effectiveSelectedPlayerId,
                            })
                          }}
                        >
                          {activateMutation.isPending
                            ? t('gameModifiers.adminPanel.activatePending')
                            : t('gameModifiers.adminPanel.activateAction')}
                        </AppButton>
                      </>
                    )}
                  </>
                )}
              </Stack>
            )}
          </AdminBlock>

          <AdminBlock
            title={t('gameModifiers.adminPanel.cancelModifierLabel')}
            hint={t('gameModifiers.adminPanel.cancelHint')}
          >
            {adminActivationsQuery.isLoading ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.adminPanel.stateLoading')}
              </Typography>
            ) : adminActivationsQuery.isError ? (
              <Typography variant="body2" color="error.main">
                {t('gameModifiers.adminPanel.stateError')}
              </Typography>
            ) : activeActivations.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.adminPanel.noActiveModifiers')}
              </Typography>
            ) : (
              <Stack spacing={1}>
                <Autocomplete
                  size="small"
                  options={cancelModifierOptions}
                  value={selectedCancelModifier}
                  onChange={(_event, value) => {
                    setSelectedCancelModifierId(value?.modifierId ?? '')
                    setSelectedActivationId('')
                  }}
                  getOptionLabel={(option) => option.modifierName}
                  isOptionEqualToValue={(option, value) => option.modifierId === value.modifierId}
                  disabled={isBusy}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label={t('gameModifiers.adminPanel.cancelModifierLabel')}
                    />
                  )}
                />

                <Autocomplete
                  size="small"
                  options={cancelActivationOptions}
                  value={selectedActivation}
                  onChange={(_event, value) => setSelectedActivationId(value?.activationId ?? '')}
                  getOptionLabel={(option) =>
                    t('gameModifiers.adminPanel.activationOption', {
                      player: option.activatedByDisplayName,
                      time: new Date(option.activatedAtUtc).toLocaleTimeString(),
                      cost: option.activationCost,
                    })
                  }
                  isOptionEqualToValue={(option, value) =>
                    option.activationId === value.activationId
                  }
                  disabled={isBusy || effectiveSelectedCancelModifierId.length === 0}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label={t('gameModifiers.adminPanel.cancelActivationLabel')}
                    />
                  )}
                />

                <AppButton
                  tone="secondary"
                  size="small"
                  fullWidth
                  disabled={isBusy || effectiveSelectedActivationId.length === 0}
                  onClick={() => {
                    if (!effectiveSelectedActivationId) {
                      return
                    }

                    cancelMutation.mutate(effectiveSelectedActivationId)
                  }}
                >
                  {cancelMutation.isPending
                    ? t('gameModifiers.adminPanel.cancelPending')
                    : t('gameModifiers.adminPanel.cancelAction')}
                </AppButton>
              </Stack>
            )}
          </AdminBlock>
        </Stack>
      </SectionCard>

      <AppToast
        message={toastMessage}
        onClose={() => setToastMessage(null)}
        severity={toastSeverity}
        autoHideDuration={4000}
      />
    </>
  )
}

function AdminBlock({
  title,
  hint,
  children,
}: {
  title: string
  hint: string
  children: ReactNode
}) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.5)}`,
        borderRadius: 1.5,
        px: 1,
        py: 1,
      })}
    >
      <Stack spacing={1}>
        <Box>
          <Typography variant="caption" color="text.secondary">
            {title}
          </Typography>
          <Typography variant="caption" color="text.secondary" display="block">
            {hint}
          </Typography>
        </Box>
        {children}
      </Stack>
    </Box>
  )
}

function buildCancelModifierOptions(activeModifiers: GameModifierActivation[]): CancelModifierOption[] {
  const seenModifierIds = new Set<string>()
  const options: CancelModifierOption[] = []

  for (const activation of activeModifiers) {
    if (seenModifierIds.has(activation.modifierId)) {
      continue
    }

    seenModifierIds.add(activation.modifierId)
    options.push({
      modifierId: activation.modifierId,
      modifierName: activation.modifierName,
    })
  }

  return options
}

function resolveAdminActivateErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierOrderingClosed:
      return 'gameModifiers.blockedReasons.ordering_closed'
    case API_ERROR_CODES.gameModifierLimitReached:
      return 'gameModifiers.blockedReasons.limit_reached'
    case API_ERROR_CODES.gameModifierConflictActive:
      return 'gameModifiers.blockedReasons.conflict_active'
    case API_ERROR_CODES.gameModifierInsufficientQuizPoints:
      return 'gameModifiers.blockedReasons.insufficient_points'
    case API_ERROR_CODES.gameModifierPlayerNotFound:
      return 'gameModifiers.adminPanel.playerNotFound'
    default:
      return 'gameModifiers.activateFailed'
  }
}

function resolveAdminCancelErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) {
    return 'gameModifiers.activateFailed'
  }

  const payload = error.details as Partial<ErrorResponse>
  switch (payload.code) {
    case API_ERROR_CODES.gameModifierActivationNotFound:
      return 'gameModifiers.adminPanel.activationNotFound'
    case API_ERROR_CODES.gameModifierAlreadyAppliedInRound:
      return 'gameModifiers.adminPanel.alreadyAppliedInRound'
    default:
      return 'gameModifiers.activateFailed'
  }
}
