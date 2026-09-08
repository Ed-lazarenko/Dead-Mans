import { Stack, TextField, Typography } from '@mui/material'
import Autocomplete from '@mui/material/Autocomplete'
import type { ComponentProps } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameModifierActivation } from '../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../shared/ui/index.ts'
import type { CancelModifierOption } from '../model/admin-modifier-support.ts'
import { AdminModifierBlock } from './admin-modifier-panel-primitives.tsx'

interface AdminModifierCancellationBlockProps {
  activeActivations: GameModifierActivation[]
  modifierOptions: CancelModifierOption[]
  selectedModifier: CancelModifierOption | null
  activationOptions: GameModifierActivation[]
  selectedActivation: GameModifierActivation | null
  cancelReason: string
  isLoading: boolean
  isError: boolean
  isBusy: boolean
  isCancelling: boolean
  onModifierChange: (modifierId: string) => void
  onActivationChange: (activationId: string) => void
  onCancelReasonChange: (reason: string) => void
  onRequestCancel: () => void
}

export function AdminModifierCancellationBlock({
  activeActivations,
  modifierOptions,
  selectedModifier,
  activationOptions,
  selectedActivation,
  cancelReason,
  isLoading,
  isError,
  isBusy,
  isCancelling,
  onModifierChange,
  onActivationChange,
  onCancelReasonChange,
  onRequestCancel,
}: AdminModifierCancellationBlockProps) {
  const { t, i18n } = useTranslation()

  return (
    <AdminModifierBlock
      sectionId="cancel"
      step={t('gameModifiers.adminPanel.stepTwo')}
      title={t('gameModifiers.adminPanel.cancelModifierLabel')}
      tooltip={t('gameModifiers.adminPanel.cancelTooltip')}
    >
      {isLoading ? (
        <Typography variant="body2" color="text.secondary">
          {t('gameModifiers.adminPanel.stateLoading')}
        </Typography>
      ) : isError ? (
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
            options={modifierOptions}
            value={selectedModifier}
            onChange={(_event, value) => onModifierChange(value?.modifierId ?? '')}
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
            options={activationOptions}
            value={selectedActivation}
            onChange={(_event, value) => onActivationChange(value?.activationId ?? '')}
            getOptionLabel={(option) =>
              t('gameModifiers.adminPanel.activationOption', {
                player: option.activatedByDisplayName,
                time: new Date(option.activatedAtUtc).toLocaleTimeString(i18n.resolvedLanguage),
                cost: option.activationCost,
              })
            }
            isOptionEqualToValue={(option, value) => option.activationId === value.activationId}
            disabled={isBusy || selectedModifier == null}
            renderInput={(params) => (
              <TextField
                {...(params as unknown as ComponentProps<typeof TextField>)}
                size="small"
                label={t('gameModifiers.adminPanel.cancelActivationLabel')}
              />
            )}
          />

          <TextField
            size="small"
            label={t('gameModifiers.adminPanel.cancelReasonLabel')}
            value={cancelReason}
            onChange={(event) => onCancelReasonChange(event.target.value)}
            disabled={isBusy || selectedActivation == null}
            required
            inputProps={{ maxLength: 1000 }}
          />

          <AppButton
            tone="dangerSecondary"
            size="small"
            fullWidth
            disabled={isBusy || selectedActivation == null || cancelReason.trim().length === 0}
            onClick={onRequestCancel}
            sx={{ minHeight: 44 }}
          >
            {isCancelling
              ? t('gameModifiers.adminPanel.cancelPending')
              : t('gameModifiers.adminPanel.cancelAction')}
          </AppButton>
        </Stack>
      )}
    </AdminModifierBlock>
  )
}
