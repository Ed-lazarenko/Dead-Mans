import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  FormControl,
  FormHelperText,
  FormLabel,
  InputAdornment,
  RadioGroup,
  Stack,
  Typography,
} from '@mui/material'
import { Controller, useWatch } from 'react-hook-form'
import type { Control, UseFormSetValue } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import { ControlledFormTextField } from '../../../shared/ui/index.ts'
import { modifierPhases, type ModifierFormValues } from '../model/modifier-form-schema.ts'
import {
  FieldWithHelp,
  ModifierConflictField,
  SelectionCard,
  WizardSection,
} from './modifier-form-fields.tsx'

export function ModifierActivationStep({
  control,
  disabled,
  initial,
  kind,
  modifiers,
  setValue,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
  initial?: GameModifierDefinition
  kind: ModifierFormValues['kind']
  modifiers: GameModifierDefinition[]
  setValue: UseFormSetValue<ModifierFormValues>
}) {
  const { t } = useTranslation()
  const help = (field: string) => t(`gameCatalog.modifiers.wizard.help.${field}`)
  const durationEnabled = useWatch({ control, name: 'durationEnabled' })
  return (
    <Stack spacing={1.5}>
      <WizardSection
        title={t('gameCatalog.modifiers.wizard.sections.behavior')}
        description={t('gameCatalog.modifiers.wizard.sections.behaviorDescription')}
      >
        <FieldWithHelp label={t('gameCatalog.modifiers.wizard.phase')} help={help('phase')}>
          <Controller
            control={control}
            name="phase"
            render={({ field, fieldState }) => (
              <FormControl component="fieldset" error={fieldState.invalid} fullWidth>
                <FormLabel component="legend">{t('gameCatalog.modifiers.wizard.phase')}</FormLabel>
                <RadioGroup {...field} sx={{ mt: 0.75, gap: 0.75 }}>
                  {modifierPhases.map((phase) => (
                    <SelectionCard
                      key={phase}
                      value={phase}
                      checked={field.value === phase}
                      disabled={disabled}
                      title={t(`gameCatalog.modifiers.wizard.phases.${phase}`)}
                      description={t(`gameCatalog.modifiers.wizard.phaseDescriptions.${phase}`)}
                    />
                  ))}
                </RadioGroup>
                {fieldState.error ? (
                  <FormHelperText>{fieldState.error.message}</FormHelperText>
                ) : null}
              </FormControl>
            )}
          />
        </FieldWithHelp>
        <FieldWithHelp label={t('gameCatalog.modifiers.wizard.performer')} help={help('performer')}>
          <Controller
            control={control}
            name="performer"
            render={({ field, fieldState }) => (
              <FormControl component="fieldset" error={fieldState.invalid} fullWidth>
                <FormLabel component="legend">
                  {t('gameCatalog.modifiers.wizard.performer')}
                </FormLabel>
                <RadioGroup
                  {...field}
                  sx={{
                    mt: 0.75,
                    display: 'grid',
                    gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                    gap: 0.75,
                  }}
                >
                  {(['activeTeam', 'mentor'] as const).map((performer) => (
                    <SelectionCard
                      key={performer}
                      value={performer}
                      checked={field.value === performer}
                      disabled={disabled}
                      title={t(`gameCatalog.modifiers.wizard.performers.${performer}`)}
                      description={t(
                        `gameCatalog.modifiers.wizard.performerDescriptions.${performer}`,
                      )}
                    />
                  ))}
                </RadioGroup>
                {fieldState.error ? (
                  <FormHelperText>{fieldState.error.message}</FormHelperText>
                ) : null}
              </FormControl>
            )}
          />
        </FieldWithHelp>
        <FieldWithHelp label={t('gameCatalog.modifiers.wizard.rule')} help={help('rule')}>
          <ControlledFormTextField
            control={control}
            name="rule"
            label={t('gameCatalog.modifiers.wizard.rule')}
            multiline
            minRows={3}
            disabled={disabled}
          />
        </FieldWithHelp>
        <FieldWithHelp
          label={t('gameCatalog.modifiers.wizard.requiresHostMonitoring')}
          help={help('requiresHostMonitoring')}
        >
          <Controller
            control={control}
            name="requiresHostMonitoring"
            render={({ field }) => (
              <FormControl component="fieldset" fullWidth>
                <FormLabel component="legend">
                  {t('gameCatalog.modifiers.wizard.requiresHostMonitoring')}
                </FormLabel>
                <RadioGroup
                  value={field.value ? 'yes' : 'no'}
                  onChange={(_, value) => field.onChange(value === 'yes')}
                  sx={{
                    mt: 0.75,
                    display: 'grid',
                    gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                    gap: 0.75,
                  }}
                >
                  {(['yes', 'no'] as const).map((answer) => (
                    <SelectionCard
                      key={answer}
                      value={answer}
                      checked={field.value === (answer === 'yes')}
                      disabled={disabled}
                      title={t(`gameCatalog.modifiers.wizard.monitoringAnswers.${answer}`)}
                      description={t(
                        `gameCatalog.modifiers.wizard.monitoringDescriptions.${answer}`,
                      )}
                    />
                  ))}
                </RadioGroup>
              </FormControl>
            )}
          />
        </FieldWithHelp>
        {kind === 'rule' ? (
          <>
            <FieldWithHelp
              label={t('gameCatalog.modifiers.wizard.durationQuestion')}
              help={help('durationSeconds')}
            >
              <Controller
                control={control}
                name="durationEnabled"
                render={({ field }) => (
                  <FormControl component="fieldset" fullWidth>
                    <FormLabel component="legend">
                      {t('gameCatalog.modifiers.wizard.durationQuestion')}
                    </FormLabel>
                    <RadioGroup
                      value={field.value ? 'yes' : 'no'}
                      onChange={(_, value) => {
                        const enabled = value === 'yes'
                        field.onChange(enabled)
                        if (!enabled) {
                          setValue('durationSeconds', '', { shouldDirty: true })
                        }
                      }}
                      sx={{
                        mt: 0.75,
                        display: 'grid',
                        gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                        gap: 0.75,
                      }}
                    >
                      {(['yes', 'no'] as const).map((answer) => (
                        <SelectionCard
                          key={answer}
                          value={answer}
                          checked={field.value === (answer === 'yes')}
                          disabled={disabled}
                          title={t(`gameCatalog.modifiers.wizard.durationAnswers.${answer}`)}
                          description={t(
                            `gameCatalog.modifiers.wizard.durationDescriptions.${answer}`,
                          )}
                        />
                      ))}
                    </RadioGroup>
                  </FormControl>
                )}
              />
            </FieldWithHelp>
            {durationEnabled ? (
              <FieldWithHelp
                label={t('gameCatalog.modifiers.fields.durationSeconds')}
                help={help('durationSeconds')}
              >
                <ControlledFormTextField
                  control={control}
                  name="durationSeconds"
                  type="number"
                  label={t('gameCatalog.modifiers.fields.durationSeconds')}
                  disabled={disabled}
                  slotProps={{
                    input: {
                      endAdornment: (
                        <InputAdornment position="end">
                          {t('gameCatalog.modifiers.wizard.units.seconds')}
                        </InputAdornment>
                      ),
                    },
                  }}
                />
              </FieldWithHelp>
            ) : null}
          </>
        ) : null}
      </WizardSection>

      <WizardSection
        title={t('gameCatalog.modifiers.wizard.sections.activation')}
        description={t('gameCatalog.modifiers.wizard.sections.activationDescription')}
      >
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <FieldWithHelp
            label={t('gameCatalog.modifiers.fields.activationCost')}
            help={help('activationCost')}
          >
            <ControlledFormTextField
              control={control}
              name="activationCost"
              type="number"
              label={t('gameCatalog.modifiers.fields.activationCost')}
              disabled={disabled}
            />
          </FieldWithHelp>
          <FieldWithHelp
            label={t('gameCatalog.modifiers.fields.activationLimitCount')}
            help={help('activationLimitCount')}
          >
            <ControlledFormTextField
              control={control}
              name="activationLimitCount"
              type="number"
              label={t('gameCatalog.modifiers.fields.activationLimitCount')}
              helperText={t('gameCatalog.modifiers.fields.limitHint')}
              disabled={disabled}
            />
          </FieldWithHelp>
        </Stack>
        <FieldWithHelp label={t('gameCatalog.modifiers.fields.conflicts')} help={help('conflicts')}>
          <ModifierConflictField
            control={control}
            currentModifierId={initial?.id}
            disabled={disabled}
            modifiers={modifiers}
          />
        </FieldWithHelp>
        <Accordion
          disableGutters
          elevation={0}
          sx={{ border: 1, borderColor: 'divider', '&::before': { display: 'none' } }}
        >
          <AccordionSummary>
            <Box>
              <Typography variant="subtitle2">
                {t('gameCatalog.modifiers.wizard.advancedSettings')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameCatalog.modifiers.wizard.advancedSettingsDescription')}
              </Typography>
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <FieldWithHelp
              label={t('gameCatalog.modifiers.fields.activationCommand')}
              help={help('activationCommand')}
            >
              <ControlledFormTextField
                control={control}
                name="activationCommand"
                label={t('gameCatalog.modifiers.fields.activationCommand')}
                helperText={t('gameCatalog.modifiers.wizard.commandHint')}
                disabled={disabled}
              />
            </FieldWithHelp>
          </AccordionDetails>
        </Accordion>
      </WizardSection>
    </Stack>
  )
}
