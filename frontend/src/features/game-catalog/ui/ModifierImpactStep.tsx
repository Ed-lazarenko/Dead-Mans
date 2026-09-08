import {
  Alert,
  FormControl,
  FormHelperText,
  FormLabel,
  InputAdornment,
  RadioGroup,
  Stack,
} from '@mui/material'
import { Controller, useWatch } from 'react-hook-form'
import type { Control, UseFormSetValue } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { ControlledFormTextField } from '../../../shared/ui/index.ts'
import {
  modifierEventMaximumKinds,
  modifierEventMeasurementModes,
  modifierKillMeasurementModes,
  modifierMeasurementDomains,
  modifierPayoutDefaultValues,
  modifierPayoutKinds,
  type ModifierFormValues,
} from '../model/modifier-form-schema.ts'
import { FieldWithHelp, SelectionCard, WizardSection } from './modifier-form-fields.tsx'

export function ModifierImpactStep({
  control,
  disabled,
  setValue,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
  setValue: UseFormSetValue<ModifierFormValues>
}) {
  const { t } = useTranslation()
  const measurementDomain = useWatch({ control, name: 'measurementDomain' })
  const killMeasurementMode = useWatch({ control, name: 'killMeasurementMode' })
  const eventMeasurementMode = useWatch({ control, name: 'eventMeasurementMode' })
  const eventMaximumKind = useWatch({ control, name: 'eventMaximumKind' })
  const payoutKind = useWatch({ control, name: 'payoutKind' })
  const payoutValue = useWatch({ control, name: 'payoutValue' })
  const help = (field: string) => t(`gameCatalog.modifiers.wizard.help.${field}`)

  const cards = <T extends string>(
    values: readonly T[],
    selected: T | null,
    prefix: string,
    isDisabled = disabled,
  ) =>
    values.map((value) => (
      <SelectionCard
        key={value}
        value={value}
        checked={selected === value}
        disabled={isDisabled}
        title={t(`${prefix}.${value}.title`)}
        description={t(`${prefix}.${value}.description`)}
      />
    ))

  return (
    <Stack spacing={2}>
      <WizardSection
        title={t('gameCatalog.modifiers.wizard.measurement.title')}
        description={t('gameCatalog.modifiers.wizard.measurement.description')}
      >
        <Controller
          control={control}
          name="measurementDomain"
          render={({ field, fieldState }) => (
            <FormControl component="fieldset" error={fieldState.invalid} fullWidth>
              <FormLabel component="legend">
                {t('gameCatalog.modifiers.wizard.measurement.question')}
              </FormLabel>
              <RadioGroup
                value={field.value ?? ''}
                onChange={(_, value) => field.onChange(value)}
                sx={{
                  mt: 0.75,
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                  gap: 1,
                }}
              >
                {cards(
                  modifierMeasurementDomains,
                  field.value,
                  'gameCatalog.modifiers.wizard.measurement.domains',
                )}
              </RadioGroup>
              {fieldState.error ? (
                <FormHelperText>{fieldState.error.message}</FormHelperText>
              ) : null}
            </FormControl>
          )}
        />

        {measurementDomain === 'kills' ? (
          <Controller
            control={control}
            name="killMeasurementMode"
            render={({ field }) => (
              <FormControl component="fieldset" fullWidth>
                <FormLabel component="legend">
                  {t('gameCatalog.modifiers.wizard.measurement.killQuestion')}
                </FormLabel>
                <RadioGroup {...field} sx={{ mt: 0.75, gap: 0.75 }}>
                  {cards(
                    modifierKillMeasurementModes,
                    field.value,
                    'gameCatalog.modifiers.wizard.measurement.killModes',
                  )}
                </RadioGroup>
              </FormControl>
            )}
          />
        ) : null}

        {measurementDomain === 'event' ? (
          <Controller
            control={control}
            name="eventMeasurementMode"
            render={({ field }) => (
              <FormControl component="fieldset" fullWidth>
                <FormLabel component="legend">
                  {t('gameCatalog.modifiers.wizard.measurement.eventQuestion')}
                </FormLabel>
                <RadioGroup {...field} sx={{ mt: 0.75, gap: 0.75 }}>
                  {cards(
                    modifierEventMeasurementModes,
                    field.value,
                    'gameCatalog.modifiers.wizard.measurement.eventModes',
                  )}
                </RadioGroup>
              </FormControl>
            )}
          />
        ) : null}

        {(measurementDomain === 'kills' && killMeasurementMode === 'qualifying') ||
        (measurementDomain === 'event' && eventMeasurementMode !== 'perActivation') ? (
          <FieldWithHelp
            label={t('gameCatalog.modifiers.wizard.measurement.inputLabel')}
            help={help('eventInputLabel')}
          >
            <ControlledFormTextField
              control={control}
              name="eventInputLabel"
              label={t('gameCatalog.modifiers.wizard.measurement.inputLabel')}
              helperText={t('gameCatalog.modifiers.wizard.measurement.inputLabelHint')}
              disabled={disabled}
            />
          </FieldWithHelp>
        ) : null}

        {measurementDomain === 'event' && eventMeasurementMode === 'count' ? (
          <>
            <Controller
              control={control}
              name="eventMaximumKind"
              render={({ field }) => (
                <FormControl component="fieldset" fullWidth>
                  <FormLabel component="legend">
                    {t('gameCatalog.modifiers.wizard.measurement.maximumQuestion')}
                  </FormLabel>
                  <RadioGroup
                    {...field}
                    sx={{
                      mt: 0.75,
                      display: 'grid',
                      gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                      gap: 1,
                    }}
                  >
                    {cards(
                      modifierEventMaximumKinds,
                      field.value,
                      'gameCatalog.modifiers.wizard.measurement.maximumKinds',
                    )}
                  </RadioGroup>
                </FormControl>
              )}
            />
            {eventMaximumKind === 'activations' ? (
              <ControlledFormTextField
                control={control}
                name="eventsPerActivation"
                type="number"
                label={t('gameCatalog.modifiers.wizard.measurement.eventsPerActivation')}
                helperText={t('gameCatalog.modifiers.wizard.measurement.eventsPerActivationHint')}
                disabled={disabled}
              />
            ) : null}
          </>
        ) : null}
      </WizardSection>

      <WizardSection
        title={t('gameCatalog.modifiers.wizard.payout.title')}
        description={t('gameCatalog.modifiers.wizard.payout.description')}
      >
        <Controller
          control={control}
          name="payoutKind"
          render={({ field, fieldState }) => (
            <FormControl component="fieldset" error={fieldState.invalid} fullWidth>
              <FormLabel component="legend">
                {t('gameCatalog.modifiers.wizard.payout.question')}
              </FormLabel>
              <RadioGroup
                value={field.value ?? ''}
                onChange={(_, value) => {
                  if (field.value !== value) {
                    setValue(
                      'payoutValue',
                      modifierPayoutDefaultValues[
                        value as keyof typeof modifierPayoutDefaultValues
                      ],
                      { shouldDirty: true },
                    )
                    if (value === 'killValueIncrease') {
                      setValue('zeroCountPenaltyPoints', '0', { shouldDirty: true })
                    }
                  }
                  field.onChange(value)
                }}
                sx={{ mt: 0.75, gap: 0.75 }}
              >
                {cards(
                  modifierPayoutKinds,
                  field.value,
                  'gameCatalog.modifiers.wizard.payout.kinds',
                )}
              </RadioGroup>
              {fieldState.error ? (
                <FormHelperText>{fieldState.error.message}</FormHelperText>
              ) : null}
            </FormControl>
          )}
        />

        {payoutKind ? (
          <ControlledFormTextField
            control={control}
            name="payoutValue"
            type="number"
            label={t(`gameCatalog.modifiers.wizard.payout.values.${payoutKind}`)}
            helperText={t(`gameCatalog.modifiers.wizard.payout.valueHints.${payoutKind}`)}
            disabled={disabled}
            slotProps={{
              input: {
                endAdornment:
                  payoutKind === 'cardPercent' ? (
                    <InputAdornment position="end">%</InputAdornment>
                  ) : undefined,
              },
            }}
          />
        ) : null}
        {payoutKind === 'killValueIncrease' ? (
          <ControlledFormTextField
            control={control}
            name="zeroCountPenaltyPoints"
            type="number"
            label={t('gameCatalog.modifiers.wizard.payout.zeroCountPenalty')}
            helperText={t('gameCatalog.modifiers.wizard.payout.zeroCountPenaltyHint')}
            disabled={disabled}
          />
        ) : null}
      </WizardSection>

      {measurementDomain && payoutKind ? (
        <Alert severity="info">
          {t('gameCatalog.modifiers.wizard.payout.summary', {
            source: t(
              `gameCatalog.modifiers.wizard.measurement.domains.${measurementDomain}.title`,
            ),
            effect: t(`gameCatalog.modifiers.wizard.payout.kinds.${payoutKind}.title`),
            value: payoutValue,
          })}
        </Alert>
      ) : null}
    </Stack>
  )
}
