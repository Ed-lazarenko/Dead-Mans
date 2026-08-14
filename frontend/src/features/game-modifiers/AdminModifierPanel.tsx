import { Box, Chip, Drawer, IconButton, Stack, TextField, Tooltip, Typography } from '@mui/material'
import Autocomplete, { createFilterOptions } from '@mui/material/Autocomplete'
import { alpha } from '@mui/material/styles'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { ComponentProps, ReactNode } from 'react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  ErrorResponse,
  GameModifierActivation,
  GameModifierAdminPlayer,
  GameModifierAdminPlayersResult,
  GameModifierAvailability,
} from '../../shared/api/contracts/index.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AppButton, AppToast, ConfirmDialog, SectionCard } from '../../shared/ui/index.ts'
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

interface CancelModifierOption {
  modifierId: string
  modifierName: string
}

const filterAdminPlayers = createFilterOptions<GameModifierAdminPlayer>({
  limit: 30,
  stringify: (player) => `${player.displayName} ${player.login}`,
})

const emptyAdminPlayers: readonly GameModifierAdminPlayer[] = []
const emptyAdminPlayersSummary: GameModifierAdminPlayersResult['summary'] = {
  playersCount: 0,
  totalAvailableQuizPoints: 0,
  totalEarnedQuizPoints: 0,
  totalSpentQuizPoints: 0,
}

export function AdminModifierPanel() {
  const { t, i18n } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [isOpen, setIsOpen] = useState(false)
  const [selectedPlayerId, setSelectedPlayerId] = useState('')
  const [selectedAvailableModifierId, setSelectedAvailableModifierId] = useState('')
  const [selectedCancelModifierId, setSelectedCancelModifierId] = useState('')
  const [selectedActivationId, setSelectedActivationId] = useState('')
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false)
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
            t(`common.modifiers.categories.${option.modifier.category}`),
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
    mutationFn: (activationId: string) => cancelGameModifierActivation(activationId),
    onSuccess: () => {
      setIsCancelConfirmOpen(false)
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
      <AppButton
        tone="secondary"
        size="medium"
        onClick={() => setIsOpen(true)}
        aria-haspopup="dialog"
        sx={(theme) => ({
          position: 'fixed',
          zIndex: theme.zIndex.drawer - 1,
          right: { xs: 12, md: 0 },
          top: { xs: 'auto', md: '50%' },
          bottom: { xs: 16, md: 'auto' },
          transform: { xs: 'none', md: 'translateY(-50%)' },
          minWidth: { xs: 0, md: 52 },
          minHeight: { xs: 46, md: 192 },
          px: { xs: 1.6, md: 0.95 },
          py: { xs: 0.9, md: 1.6 },
          borderRadius: { xs: 999, md: '18px 0 0 18px' },
          writingMode: { xs: 'horizontal-tb', md: 'vertical-rl' },
          textOrientation: { xs: 'mixed', md: 'mixed' },
          justifyContent: 'center',
          letterSpacing: '0.03em',
          whiteSpace: 'nowrap',
          boxShadow: `0 14px 28px ${alpha(theme.palette.common.black, 0.38)}`,
        })}
      >
        {t('gameModifiers.adminPanel.openAction')}
      </AppButton>

      <Drawer anchor="right" open={isOpen} onClose={() => setIsOpen(false)}>
        <Box
          sx={{
            width: { xs: '100vw', md: 520 },
            maxWidth: '100vw',
            p: { xs: 1.5, sm: 2 },
            overflowY: 'auto',
          }}
          role="presentation"
          aria-labelledby="admin-modifier-panel-title"
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              spacing={1.5}
              alignItems="flex-start"
              justifyContent="space-between"
            >
              <Stack spacing={0.75}>
                <Typography id="admin-modifier-panel-title" variant="h6">
                  {t('gameModifiers.adminPanel.title')}
                </Typography>
                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                  <Chip
                    color="warning"
                    variant="outlined"
                    label={t('gameModifiers.adminPanel.summaryUsedCount', {
                      count: activeActivations.length,
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
                      sm: 'repeat(2, minmax(0, 1fr))',
                    },
                    gap: 1,
                  }}
                >
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summaryAvailablePoints')}
                    value={t('gameModifiers.myPointsValue', {
                      points: summary.totalAvailableQuizPoints,
                    })}
                  />
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summarySpentPoints')}
                    value={t('gameModifiers.myPointsValue', {
                      points: summary.totalSpentQuizPoints,
                    })}
                  />
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summaryEarnedPoints')}
                    value={t('gameModifiers.myPointsValue', {
                      points: summary.totalEarnedQuizPoints,
                    })}
                  />
                  <PanelMetric
                    label={t('gameModifiers.adminPanel.summaryUsedLabel')}
                    value={String(activeActivations.length)}
                  />
                </Box>
              </Stack>
            </SectionCard>

            <AdminBlock
              step={t('gameModifiers.adminPanel.stepOne')}
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
                      <TextField
                        {...(params as unknown as ComponentProps<typeof TextField>)}
                        size="small"
                        label={t('common.entities.player')}
                      />
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
                                <Stack
                                  direction="row"
                                  spacing={1}
                                  justifyContent="space-between"
                                  alignItems="center"
                                  sx={{ width: '100%' }}
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
                              </Box>
                            )}
                            renderInput={(params) => (
                              <TextField
                                {...(params as unknown as ComponentProps<typeof TextField>)}
                                size="small"
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
              step={t('gameModifiers.adminPanel.stepTwo')}
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
                        {...(params as unknown as ComponentProps<typeof TextField>)}
                        size="small"
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
                        time: new Date(option.activatedAtUtc).toLocaleTimeString(
                          i18n.resolvedLanguage,
                        ),
                        cost: option.activationCost,
                      })
                    }
                    isOptionEqualToValue={(option, value) =>
                      option.activationId === value.activationId
                    }
                    disabled={isBusy || effectiveSelectedCancelModifierId.length === 0}
                    renderInput={(params) => (
                      <TextField
                        {...(params as unknown as ComponentProps<typeof TextField>)}
                        size="small"
                        label={t('gameModifiers.adminPanel.cancelActivationLabel')}
                      />
                    )}
                  />

                  <AppButton
                    tone="dangerSecondary"
                    size="small"
                    fullWidth
                    disabled={isBusy || effectiveSelectedActivationId.length === 0}
                    onClick={() => setIsCancelConfirmOpen(true)}
                    sx={{ minHeight: 44 }}
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
          if (!effectiveSelectedActivationId) {
            return
          }

          cancelMutation.mutate(effectiveSelectedActivationId)
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

function AdminBlock({
  step,
  title,
  tooltip,
  children,
}: {
  step: string
  title: string
  tooltip: string
  children: ReactNode
}) {
  return (
    <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
      <Stack spacing={1}>
        <Stack spacing={0.2}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontWeight: 750, letterSpacing: '0.025em' }}
          >
            {step}
          </Typography>
          <Stack direction="row" spacing={0.5} alignItems="center">
            <Typography component="h3" variant="subtitle1" sx={{ fontWeight: 850 }}>
              {title}
            </Typography>
            <HintTooltip title={tooltip} />
          </Stack>
        </Stack>
        {children}
      </Stack>
    </SectionCard>
  )
}

function HintTooltip({ title }: { title: string }) {
  return (
    <Tooltip title={title} arrow placement="top">
      <IconButton
        size="small"
        aria-label={title}
        sx={(theme) => ({
          width: 44,
          height: 44,
          color: 'text.secondary',
          '&:hover': {
            color: 'primary.main',
            backgroundColor: alpha(theme.palette.primary.main, 0.08),
          },
        })}
      >
        <Box
          component="span"
          aria-hidden="true"
          sx={(theme) => ({
            width: 18,
            height: 18,
            borderRadius: '999px',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
            fontSize: '0.7rem',
            cursor: 'help',
          })}
        >
          ?
        </Box>
      </IconButton>
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
    case API_ERROR_CODES.gameModifierActiveTeamMember:
      return 'gameModifiers.blockedReasons.active_team_member'
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
