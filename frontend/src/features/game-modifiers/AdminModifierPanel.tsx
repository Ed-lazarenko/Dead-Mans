import { Box, Chip, Drawer, IconButton, Stack, TextField, Tooltip, Typography } from '@mui/material'
import Autocomplete, { createFilterOptions } from '@mui/material/Autocomplete'
import { alpha } from '@mui/material/styles'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  ErrorResponse,
  GameModifierActivation,
  GameModifierAdminPlayer,
  GameModifierAvailability,
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
import { deriveModifierRoundSummaryMeta } from './model/modifier-round-summary.ts'
import { buildModifierSearchText } from './model/modifier-search.ts'

interface AdminModifierPanelProps {
  enabledModifiersCount: number
}

interface CancelModifierOption {
  modifierId: string
  modifierName: string
}

const filterAdminPlayers = createFilterOptions<GameModifierAdminPlayer>({
  limit: 30,
  stringify: (player) => `${player.displayName} ${player.login}`,
})

const emptyAdminPlayers: readonly GameModifierAdminPlayer[] = []

export function AdminModifierPanel({ enabledModifiersCount }: AdminModifierPanelProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [isOpen, setIsOpen] = useState(false)
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
  const filterAvailableModifiers = useMemo(
    () =>
      createFilterOptions<GameModifierAvailability>({
        limit: 30,
        stringify: (option) =>
          buildModifierSearchText(option.modifier, [
            t(`gameModifiers.categories.${option.modifier.category}`),
            t(`gameCatalog.modifiers.mechanics.${option.modifier.mechanicType}`),
            t(
              `gameCatalog.modifiers.roundSummaryType.${
                deriveModifierRoundSummaryMeta(option.modifier).type
              }`,
            ),
          ]),
      }),
    [t],
  )
  const adminActivationsQuery = useQuery({
    ...adminGameModifierActivationsQueryOptions,
    enabled: isAdmin,
  })
  const players = adminPlayersQuery.data ?? emptyAdminPlayers
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

  const summary = useMemo(
    () => ({
      totalAvailablePoints: players.reduce(
        (total, player) => total + player.availableQuizPoints,
        0,
      ),
      totalSpentPoints: players.reduce((total, player) => total + player.spentQuizPoints, 0),
      playersCount: players.length,
    }),
    [players],
  )

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
      <AppButton
        tone="secondary"
        size="small"
        onClick={() => setIsOpen(true)}
        sx={(theme) => ({
          position: 'fixed',
          zIndex: theme.zIndex.drawer - 1,
          right: { xs: 12, md: 0 },
          top: { xs: 'auto', md: '50%' },
          bottom: { xs: 16, md: 'auto' },
          transform: { xs: 'none', md: 'translateY(-50%)' },
          minWidth: { xs: 0, md: 44 },
          minHeight: { xs: 40, md: 164 },
          px: { xs: 1.5, md: 0.75 },
          py: { xs: 0.75, md: 1.25 },
          borderRadius: { xs: 999, md: '16px 0 0 16px' },
          writingMode: { xs: 'horizontal-tb', md: 'vertical-rl' },
          textOrientation: { xs: 'mixed', md: 'mixed' },
          justifyContent: 'center',
          boxShadow: `0 10px 24px ${alpha(theme.palette.common.black, 0.35)}`,
        })}
      >
        {t('gameModifiers.adminPanel.openAction')}
      </AppButton>

      <Drawer anchor="right" open={isOpen} onClose={() => setIsOpen(false)}>
        <Box
          sx={{
            width: { xs: '100vw', md: 460 },
            maxWidth: '100vw',
            p: 2,
          }}
          role="presentation"
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              spacing={1.5}
              alignItems="flex-start"
              justifyContent="space-between"
            >
              <Stack spacing={0.75}>
                <Typography variant="h6">{t('gameModifiers.adminPanel.title')}</Typography>
                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                  <Chip
                    color="info"
                    variant="outlined"
                    label={t('gameModifiers.adminPanel.playersCount', {
                      count: summary.playersCount,
                    })}
                  />
                  <Chip
                    color="warning"
                    variant="outlined"
                    label={t('gameModifiers.adminPanel.summaryEnabledCount', {
                      count: enabledModifiersCount,
                    })}
                  />
                </Stack>
              </Stack>
              <IconButton
                size="small"
                aria-label={t('gameModifiers.adminPanel.closeAction')}
                onClick={() => setIsOpen(false)}
              >
                <Box component="span" aria-hidden sx={{ fontSize: 20, lineHeight: 1 }}>
                  ×
                </Box>
              </IconButton>
            </Stack>

            <SectionCard inset sx={{ p: 1.5 }}>
              <Stack spacing={1}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="subtitle2">
                    {t('gameModifiers.adminPanel.summaryTitle')}
                  </Typography>
                  <HintTooltip title={t('gameModifiers.adminPanel.summaryTooltip')} />
                </Stack>
                <Box
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: {
                      xs: '1fr',
                      sm: 'repeat(3, minmax(0, 1fr))',
                    },
                    gap: 1,
                  }}
                >
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summaryAvailablePoints')}
                    value={t('gameModifiers.myPointsValue', {
                      points: summary.totalAvailablePoints,
                    })}
                  />
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summarySpentPoints')}
                    value={t('gameModifiers.myPointsValue', {
                      points: summary.totalSpentPoints,
                    })}
                  />
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summaryEnabledLabel')}
                    value={String(enabledModifiersCount)}
                  />
                </Box>
              </Stack>
            </SectionCard>

            <AdminBlock
              title={t('gameModifiers.adminPanel.activateLabel')}
              tooltip={t('gameModifiers.adminPanel.activateTooltip')}
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
                    autoHighlight
                    selectOnFocus
                    options={players}
                    filterOptions={filterAdminPlayers}
                    value={selectedPlayer}
                    onChange={(_event, value) => {
                      setSelectedPlayerId(value?.userId ?? '')
                      setSelectedAvailableModifierId('')
                    }}
                    getOptionLabel={(option) => option.displayName}
                    isOptionEqualToValue={(option, value) => option.userId === value.userId}
                    disabled={isBusy}
                    renderOption={(props, option) => (
                      <Box component="li" {...props}>
                        <Stack spacing={0.125}>
                          <Typography variant="body2">{option.displayName}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {t('gameModifiers.adminPanel.playerPointsOption', {
                              points: option.availableQuizPoints,
                            })}
                          </Typography>
                        </Stack>
                      </Box>
                    )}
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
                          color="primary"
                          variant="outlined"
                          label={t('gameModifiers.adminPanel.pointsAvailable', {
                            points: state.availableQuizPoints,
                          })}
                        />
                        <Chip
                          color="warning"
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
                            autoHighlight
                            selectOnFocus
                            options={state.availableModifiers}
                            filterOptions={filterAvailableModifiers}
                            value={selectedAvailableModifier}
                            onChange={(_event, value) =>
                              setSelectedAvailableModifierId(value?.modifier.id ?? '')
                            }
                            getOptionLabel={(option) => option.modifier.name}
                            isOptionEqualToValue={(option, value) =>
                              option.modifier.id === value.modifier.id
                            }
                            disabled={isBusy}
                            renderOption={(props, option) => (
                              <Box component="li" {...props}>
                                <Stack spacing={0.35} sx={{ width: '100%' }}>
                                  <Stack
                                    direction="row"
                                    spacing={1}
                                    justifyContent="space-between"
                                    alignItems="center"
                                  >
                                    <Typography
                                      variant="body2"
                                      sx={(theme) => ({
                                        color: theme.palette.success.light,
                                        fontWeight: 600,
                                      })}
                                    >
                                      {option.modifier.name}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      {t('gameModifiers.adminPanel.modifierCostOption', {
                                        cost: option.modifier.activationCost,
                                      })}
                                    </Typography>
                                  </Stack>
                                  <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                                    <Chip
                                      size="small"
                                      variant="outlined"
                                      label={t(
                                        `gameModifiers.categories.${option.modifier.category}`,
                                      )}
                                    />
                                    <Chip
                                      size="small"
                                      variant="outlined"
                                      color={
                                        deriveModifierRoundSummaryMeta(option.modifier)
                                          .includeInRoundSummary
                                          ? 'secondary'
                                          : 'default'
                                      }
                                      label={t(
                                        `gameCatalog.modifiers.roundSummaryType.${
                                          deriveModifierRoundSummaryMeta(option.modifier).type
                                        }`,
                                      )}
                                    />
                                  </Stack>
                                </Stack>
                              </Box>
                            )}
                            renderInput={(params) => (
                              <TextField
                                {...params}
                                label={t('gameModifiers.adminPanel.activateModifierLabel')}
                              />
                            )}
                          />

                          {selectedAvailableModifier?.blockedReason ? (
                            <InlineStateNotice>
                              {t(
                                `gameModifiers.blockedReasons.${selectedAvailableModifier.blockedReason}`,
                              )}
                            </InlineStateNotice>
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
              tooltip={t('gameModifiers.adminPanel.cancelTooltip')}
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
                    tone="dangerSecondary"
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
        </Box>
      </Drawer>

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
  tooltip,
  children,
}: {
  title: string
  tooltip: string
  children: ReactNode
}) {
  return (
    <SectionCard sx={{ p: 1.5 }}>
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center">
          <Typography variant="subtitle2">{title}</Typography>
          <HintTooltip title={tooltip} />
        </Stack>
        {children}
      </Stack>
    </SectionCard>
  )
}

function HintTooltip({ title }: { title: string }) {
  return (
    <Tooltip title={title} arrow placement="top">
      <Box
        component="span"
        sx={(theme) => ({
          width: 18,
          height: 18,
          borderRadius: '50%',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
          color: 'text.secondary',
          fontSize: '0.7rem',
          cursor: 'help',
          flexShrink: 0,
        })}
      >
        ?
      </Box>
    </Tooltip>
  )
}

function PanelMetric({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.divider, 0.48)}`,
        borderRadius: 1.5,
        px: 1,
        py: 0.9,
        minWidth: 0,
      })}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="subtitle2" fontWeight={700}>
        {value}
      </Typography>
    </Box>
  )
}

function InlineStateNotice({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={(theme) => ({
        border: `1px solid ${alpha(theme.palette.warning.main, 0.45)}`,
        backgroundColor: alpha(theme.palette.warning.main, 0.12),
        borderRadius: 1.5,
        px: 1,
        py: 0.85,
      })}
    >
      <Typography variant="body2">{children}</Typography>
    </Box>
  )
}

function buildCancelModifierOptions(
  activeModifiers: GameModifierActivation[],
): CancelModifierOption[] {
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
