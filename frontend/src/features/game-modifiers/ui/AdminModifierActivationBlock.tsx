import { Box, Chip, Stack, TextField, Typography } from '@mui/material'
import Autocomplete, { createFilterOptions } from '@mui/material/Autocomplete'
import type { ComponentProps } from 'react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierAdminPlayer,
  GameModifierAvailability,
  GameModifierState,
} from '../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../shared/ui/index.ts'
import { buildModifierSearchText } from '../model/modifier-search.ts'
import { deriveModifierRoundSummaryMeta } from '../model/modifier-round-summary.ts'
import { AdminModifierBlock, AdminModifierStateNotice } from './admin-modifier-panel-primitives.tsx'

const filterAdminPlayers = createFilterOptions<GameModifierAdminPlayer>({
  limit: 30,
  stringify: (player) => `${player.displayName} ${player.login}`,
})

interface AdminModifierActivationBlockProps {
  players: readonly GameModifierAdminPlayer[]
  selectedPlayer: GameModifierAdminPlayer | null
  state: GameModifierState | null
  selectedModifier: GameModifierAvailability | null
  isPlayersLoading: boolean
  isPlayersError: boolean
  isStateLoading: boolean
  isStateError: boolean
  isBusy: boolean
  isActivating: boolean
  isEmergencyDisabling: boolean
  emergencyDisableReason: string
  onPlayerChange: (playerId: string) => void
  onModifierChange: (modifierId: string) => void
  onEmergencyDisableReasonChange: (reason: string) => void
  onActivate: () => void
  onRequestEmergencyDisable: () => void
}

export function AdminModifierActivationBlock({
  players,
  selectedPlayer,
  state,
  selectedModifier,
  isPlayersLoading,
  isPlayersError,
  isStateLoading,
  isStateError,
  isBusy,
  isActivating,
  isEmergencyDisabling,
  emergencyDisableReason,
  onPlayerChange,
  onModifierChange,
  onEmergencyDisableReasonChange,
  onActivate,
  onRequestEmergencyDisable,
}: AdminModifierActivationBlockProps) {
  const { t } = useTranslation()
  const filterAvailableModifiers = useMemo(
    () =>
      createFilterOptions<GameModifierAvailability>({
        limit: 30,
        stringify: (option) =>
          buildModifierSearchText(option.modifier, [
            t(`common.modifiers.categories.${option.modifier.category}`),
            t(`gameCatalog.modifiers.wizard.kinds.${option.modifier.behaviorV2.kind}`),
            t(
              `gameCatalog.modifiers.roundSummaryType.${
                deriveModifierRoundSummaryMeta(option.modifier).type
              }`,
            ),
          ]),
      }),
    [t],
  )

  return (
    <AdminModifierBlock
      sectionId="activate"
      step={t('gameModifiers.adminPanel.stepOne')}
      title={t('gameModifiers.adminPanel.activateLabel')}
      tooltip={t('gameModifiers.adminPanel.activateTooltip')}
    >
      {isPlayersLoading ? (
        <Typography variant="body2" color="text.secondary">
          {t('gameModifiers.adminPanel.stateLoading')}
        </Typography>
      ) : isPlayersError ? (
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
            onChange={(_event, value) => onPlayerChange(value?.userId ?? '')}
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

          {isStateLoading ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameModifiers.adminPanel.stateLoading')}
            </Typography>
          ) : isStateError ? (
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
                    value={selectedModifier}
                    onChange={(_event, value) => onModifierChange(value?.modifier.id ?? '')}
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

                  {selectedModifier?.blockedReason ? (
                    <AdminModifierStateNotice>
                      {t(`gameModifiers.blockedReasons.${selectedModifier.blockedReason}`)}
                    </AdminModifierStateNotice>
                  ) : null}

                  <AppButton
                    tone="primary"
                    size="small"
                    fullWidth
                    disabled={isBusy || selectedModifier?.canActivate !== true}
                    onClick={onActivate}
                  >
                    {isActivating
                      ? t('gameModifiers.adminPanel.activatePending')
                      : t('gameModifiers.adminPanel.activateAction')}
                  </AppButton>

                  <TextField
                    size="small"
                    label={t('gameModifiers.adminPanel.emergencyDisableReasonLabel')}
                    value={emergencyDisableReason}
                    onChange={(event) => onEmergencyDisableReasonChange(event.target.value)}
                    disabled={
                      isBusy || selectedModifier == null || selectedModifier.isEmergencyDisabled
                    }
                    required
                    inputProps={{ maxLength: 1000 }}
                  />

                  {selectedModifier?.isEmergencyDisabled ? (
                    <AdminModifierStateNotice>
                      {t('gameModifiers.adminPanel.emergencyDisabledNotice')}
                    </AdminModifierStateNotice>
                  ) : null}

                  <AppButton
                    tone="dangerSecondary"
                    size="small"
                    fullWidth
                    disabled={
                      isBusy ||
                      selectedModifier == null ||
                      selectedModifier.isEmergencyDisabled ||
                      emergencyDisableReason.trim().length === 0
                    }
                    onClick={onRequestEmergencyDisable}
                  >
                    {isEmergencyDisabling
                      ? t('gameModifiers.adminPanel.emergencyDisablePending')
                      : t('gameModifiers.adminPanel.emergencyDisableAction')}
                  </AppButton>
                </>
              )}
            </>
          )}
        </Stack>
      )}
    </AdminModifierBlock>
  )
}
