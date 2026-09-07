import { zodResolver } from '@hookform/resolvers/zod'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Autocomplete,
  Box,
  Chip,
  FormControl,
  FormHelperText,
  FormLabel,
  IconButton,
  InputAdornment,
  LinearProgress,
  MenuItem,
  Paper,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import type { Control, FieldPath, UseFormSetValue } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
  GameModifierDraftPreview,
} from '../../../shared/api/contracts/index.ts'
import {
  AppButton,
  AppDialog,
  ConfirmDialog,
  ControlledFormTextField,
} from '../../../shared/ui/index.ts'
import { previewGameModifier } from '../api/catalog-modifiers-api.ts'
import {
  createDefaultModifierFormValues,
  createModifierFormSchema,
  modifierEventMaximumKinds,
  modifierEventMeasurementModes,
  modifierKillMeasurementModes,
  modifierKinds,
  modifierMeasurementDomains,
  modifierPayoutDefaultValues,
  modifierPayoutKinds,
  modifierPhases,
  normalizeModifierTags,
  suggestedModifierTags,
  toModifierRequest,
  type ModifierFormValues,
} from '../model/modifier-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const modifierFormId = 'catalog-modifier-wizard-form'

interface ModifierFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initial?: GameModifierDefinition | undefined
  modifiers: GameModifierDefinition[]
  isBusy: boolean
  isReadOnly?: boolean
  hasStaleConflict?: boolean
  staleLatest?: GameModifierDefinition | null
  onLoadLatest?: () => Promise<void>
  onClose: () => void
  onSubmit: (request: CreateGameModifierRequest) => Promise<void>
}

function HintTooltip({ label, title }: { label: string; title: string }) {
  return (
    <Tooltip title={title} arrow placement="top" enterTouchDelay={0} leaveTouchDelay={5000}>
      <IconButton
        size="small"
        aria-label={`${label}. ${title}`}
        sx={{ width: 40, height: 40, color: 'text.secondary', flexShrink: 0 }}
      >
        <Box
          component="span"
          aria-hidden
          sx={{
            width: 18,
            height: 18,
            borderRadius: '50%',
            border: 1,
            borderColor: 'divider',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '0.7rem',
            fontWeight: 700,
          }}
        >
          ?
        </Box>
      </IconButton>
    </Tooltip>
  )
}

function FieldWithHelp({
  children,
  help,
  label,
}: {
  children: ReactNode
  help: string
  label: string
}) {
  return (
    <Stack direction="row" spacing={0.5} alignItems="flex-start" sx={{ minWidth: 0, flex: 1 }}>
      <Box sx={{ minWidth: 0, flex: 1 }}>{children}</Box>
      <HintTooltip label={label} title={help} />
    </Stack>
  )
}

function WizardSection({
  children,
  description,
  title,
}: {
  children: ReactNode
  description: string
  title: string
}) {
  return (
    <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
      <Typography variant="subtitle2">{title}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        {description}
      </Typography>
      <Stack spacing={1.5} sx={{ mt: 1.5 }}>
        {children}
      </Stack>
    </Box>
  )
}

function SelectionCard({
  checked,
  description,
  disabled,
  title,
  value,
}: {
  checked: boolean
  description: string
  disabled: boolean
  title: string
  value: string
}) {
  return (
    <Paper
      component="label"
      variant="outlined"
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 0.75,
        p: 1.25,
        cursor: disabled ? 'default' : 'pointer',
        borderColor: checked ? 'primary.main' : 'divider',
        bgcolor: checked ? 'action.selected' : 'background.paper',
        transition: (theme) => theme.transitions.create(['border-color', 'background-color']),
        '&:hover': disabled ? undefined : { borderColor: 'primary.main' },
      }}
    >
      <Radio value={value} checked={checked} disabled={disabled} sx={{ p: 0.25 }} />
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="subtitle2">{title}</Typography>
        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>
      </Box>
    </Paper>
  )
}

function ModifierConflictField({
  control,
  currentModifierId,
  disabled,
  modifiers,
}: {
  control: Control<ModifierFormValues>
  currentModifierId?: string
  disabled: boolean
  modifiers: GameModifierDefinition[]
}) {
  const { t } = useTranslation()
  const options = modifiers.filter((modifier) => modifier.id !== currentModifierId)

  return (
    <Controller
      control={control}
      name="conflictingModifierIds"
      render={({ field, fieldState }) => (
        <Autocomplete
          multiple
          disabled={disabled}
          options={options}
          value={options.filter((option) => field.value.includes(option.id))}
          getOptionLabel={(option) => option.name}
          isOptionEqualToValue={(option, value) => option.id === value.id}
          onChange={(_, value) => field.onChange(value.map((option) => option.id))}
          renderInput={(params) => (
            <TextField
              {...params}
              label={t('gameCatalog.modifiers.fields.conflicts')}
              error={fieldState.invalid}
              helperText={
                fieldState.error?.message ?? t('gameCatalog.modifiers.fields.conflictsHint')
              }
            />
          )}
        />
      )}
    />
  )
}

function ModifierTagField({
  control,
  disabled,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
}) {
  const { t } = useTranslation()
  return (
    <Controller
      control={control}
      name="tags"
      render={({ field, fieldState }) => (
        <Autocomplete
          multiple
          freeSolo
          disabled={disabled}
          options={suggestedModifierTags.map((tag) =>
            t(`gameCatalog.modifiers.wizard.suggestedTags.${tag}`),
          )}
          value={field.value}
          onChange={(_, value) => field.onChange(normalizeModifierTags(value))}
          renderTags={(value, getTagProps) =>
            value.map((option, index) => (
              <Chip label={option} size="small" {...getTagProps({ index })} key={option} />
            ))
          }
          renderInput={(params) => (
            <TextField
              {...params}
              label={t('gameCatalog.modifiers.wizard.tags')}
              error={fieldState.invalid}
              helperText={fieldState.error?.message ?? t('gameCatalog.modifiers.wizard.tagsHint')}
            />
          )}
        />
      )}
    />
  )
}

function WizardProgress({ kind, step }: { kind: ModifierFormValues['kind']; step: number }) {
  const { t } = useTranslation()
  const visibleSteps = kind === 'rule' ? [0, 1, 3] : [0, 1, 2, 3]
  const current = visibleSteps.indexOf(step) + 1
  const total = visibleSteps.length
  return (
    <Box sx={{ mb: 2 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 1 }}>
        <Typography variant="subtitle2">
          {t('gameCatalog.modifiers.wizard.step', { current, total })}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t(`gameCatalog.modifiers.wizard.steps.${step}`)}
        </Typography>
      </Stack>
      <LinearProgress variant="determinate" value={(current / total) * 100} aria-hidden />
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        {t(`gameCatalog.modifiers.wizard.stepDescriptions.${step}`)}
      </Typography>
    </Box>
  )
}

function CardStep({
  control,
  disabled,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
}) {
  const { t } = useTranslation()
  const help = (field: string) => t(`gameCatalog.modifiers.wizard.help.${field}`)
  return (
    <Stack spacing={1.5}>
      <FieldWithHelp label={t('gameCatalog.modifiers.wizard.kind')} help={help('kind')}>
        <ControlledFormTextField
          control={control}
          name="kind"
          select
          label={t('gameCatalog.modifiers.wizard.kind')}
          disabled={disabled}
        >
          {modifierKinds.map((kind) => (
            <MenuItem key={kind} value={kind}>
              {t(`gameCatalog.modifiers.wizard.kinds.${kind}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.fields.name')} help={help('name')}>
        <ControlledFormTextField
          control={control}
          name="name"
          label={t('gameCatalog.modifiers.fields.name')}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp
        label={t('gameCatalog.modifiers.fields.description')}
        help={help('description')}
      >
        <ControlledFormTextField
          control={control}
          name="description"
          label={t('gameCatalog.modifiers.fields.description')}
          multiline
          minRows={3}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.fields.iconEmoji')} help={help('iconEmoji')}>
        <ControlledFormTextField
          control={control}
          name="iconEmoji"
          label={t('gameCatalog.modifiers.fields.iconEmoji')}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.wizard.tags')} help={help('tags')}>
        <ModifierTagField control={control} disabled={disabled} />
      </FieldWithHelp>
    </Stack>
  )
}

function ActivationStep({
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

function ImpactStep({
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

function ReviewStep({
  preview,
  isLoading,
  error,
  onRetry,
}: {
  preview: GameModifierDraftPreview | null
  isLoading: boolean
  error: string | null
  onRetry: () => void
}) {
  const { t } = useTranslation()
  if (isLoading) {
    return <LinearProgress aria-label={t('gameCatalog.modifiers.wizard.previewLoading')} />
  }
  if (error || !preview) {
    return (
      <Alert
        severity="error"
        action={
          <AppButton size="small" tone="secondary" onClick={onRetry}>
            {t('common.actions.retry')}
          </AppButton>
        }
      >
        {error ?? t('gameCatalog.modifiers.wizard.previewError')}
      </Alert>
    )
  }
  const localizedExample = {
    ...preview.example,
    resolutionExample: formatResolutionExample(preview.example.resolutionExample, t),
  }

  return (
    <Stack spacing={1.5}>
      <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
        <Typography variant="overline">{t('gameCatalog.modifiers.wizard.playerView')}</Typography>
        <Typography variant="h6">
          {preview.iconEmoji ? `${preview.iconEmoji} ` : ''}
          {preview.name}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
          {preview.description}
        </Typography>
        <Stack direction="row" gap={0.75} flexWrap="wrap" sx={{ mt: 1 }}>
          {preview.normalizedTags.map((tag) => (
            <Chip key={tag} label={tag} size="small" />
          ))}
        </Stack>
      </Box>
      <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
        <Typography variant="overline">{t('gameCatalog.modifiers.wizard.hostView')}</Typography>
        <Typography variant="body2">{preview.behaviorV2.rule}</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
          {t('gameCatalog.modifiers.wizard.commandPreview', {
            command: preview.activationCommand,
          })}
        </Typography>
      </Box>
      <Alert severity="success">
        <Typography variant="subtitle2">
          {t('gameCatalog.modifiers.wizard.exampleTitle')}
        </Typography>
        <Typography variant="body2">
          {t('gameCatalog.modifiers.wizard.exampleFacts', localizedExample)}
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.wizard.exampleResult', preview.example)}
        </Typography>
      </Alert>
    </Stack>
  )
}

function formatResolutionExample(value: string, t: ReturnType<typeof useTranslation>['t']) {
  return value === 'completed' ||
    value === 'automatic' ||
    value === 'succeeded' ||
    value === 'perActivation'
    ? t(`gameCatalog.modifiers.wizard.exampleResolution.${value}`)
    : value
}

const stepFields: Record<number, FieldPath<ModifierFormValues>[]> = {
  0: ['kind', 'name', 'description', 'iconEmoji', 'tags'],
  1: [
    'activationCost',
    'activationLimitCount',
    'phase',
    'performer',
    'rule',
    'requiresHostMonitoring',
    'durationEnabled',
    'durationSeconds',
    'activationCommand',
  ],
  2: [
    'measurementDomain',
    'killMeasurementMode',
    'eventMeasurementMode',
    'eventInputLabel',
    'eventMaximumKind',
    'eventsPerActivation',
    'payoutKind',
    'payoutValue',
    'zeroCountPenaltyPoints',
  ],
  3: [],
}

function ModifierFormDialogBody({
  mode,
  initial,
  modifiers,
  isBusy,
  isReadOnly = false,
  hasStaleConflict = false,
  staleLatest,
  onLoadLatest,
  onClose,
  onSubmit,
}: Omit<ModifierFormDialogProps, 'open'>) {
  const { t } = useTranslation()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const [step, setStep] = useState(0)
  const [showDiscardConfirmation, setShowDiscardConfirmation] = useState(false)
  const [preview, setPreview] = useState<GameModifierDraftPreview | null>(null)
  const [previewError, setPreviewError] = useState<string | null>(null)
  const [isPreviewLoading, setIsPreviewLoading] = useState(false)
  const schema = useMemo(
    () =>
      createModifierFormSchema({
        required: t('gameCatalog.validation.required'),
        number: t('gameCatalog.validation.number'),
        limit: t('gameCatalog.validation.limit'),
        tags: t('gameCatalog.validation.tags'),
      }),
    [t],
  )
  const { control, getValues, handleSubmit, setError, setValue, trigger, formState } =
    useForm<ModifierFormValues>({
      defaultValues: createDefaultModifierFormValues(initial),
      resolver: zodResolver(schema),
    })
  const kind = useWatch({ control, name: 'kind' })
  const disabled = isBusy || isReadOnly
  const isDirty = formState.isDirty

  const loadPreview = async () => {
    setIsPreviewLoading(true)
    setPreviewError(null)
    try {
      setPreview(await previewGameModifier(toModifierRequest(getValues())))
    } catch (error) {
      setPreview(null)
      setPreviewError(resolveCatalogErrorMessage(error, t))
    } finally {
      setIsPreviewLoading(false)
    }
  }

  const goNext = async () => {
    if (!(await trigger(stepFields[step], { shouldFocus: true }))) {
      return
    }
    const nextStep = step === 1 && kind === 'rule' ? 3 : step + 1
    setStep(nextStep)
    if (nextStep === 3) {
      await loadPreview()
    }
  }

  const goBack = () => setStep(step === 3 && kind === 'rule' ? 1 : Math.max(0, step - 1))
  const requestClose = () => {
    if (!isReadOnly && isDirty) {
      setShowDiscardConfirmation(true)
      return
    }
    onClose()
  }
  const submit = handleSubmit(async (values) => {
    if (!preview) {
      setStep(3)
      await loadPreview()
      return
    }
    try {
      await onSubmit(toModifierRequest(values))
    } catch (error) {
      setError('root', { type: 'server', message: resolveCatalogErrorMessage(error, t) })
    }
  })

  return (
    <>
      <AppDialog
        open
        maxWidth="md"
        fullScreen={isMobile}
        onClose={isBusy ? undefined : requestClose}
        title={
          mode === 'create'
            ? t('gameCatalog.modifiers.createTitle')
            : t('gameCatalog.modifiers.editTitle')
        }
        actions={
          <Stack direction="row" spacing={1} width="100%" justifyContent="space-between">
            <AppButton tone="ghost" onClick={requestClose} disabled={isBusy}>
              {isReadOnly ? t('common.actions.close') : t('common.actions.cancel')}
            </AppButton>
            <Stack direction="row" spacing={1}>
              {step > 0 ? (
                <AppButton tone="secondary" onClick={goBack} disabled={isBusy}>
                  {t('common.actions.back')}
                </AppButton>
              ) : null}
              {step < 3 ? (
                <AppButton onClick={() => void goNext()} disabled={isBusy}>
                  {t('common.actions.next')}
                </AppButton>
              ) : isReadOnly ? null : (
                <AppButton
                  type="submit"
                  form={modifierFormId}
                  disabled={isBusy || isPreviewLoading || !preview}
                >
                  {t('common.actions.save')}
                </AppButton>
              )}
            </Stack>
          </Stack>
        }
      >
        <WizardProgress step={step} kind={kind} />
        {formState.errors.root ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            {formState.errors.root.message}
          </Alert>
        ) : null}
        {hasStaleConflict ? (
          <Alert
            severity="warning"
            sx={{ mb: 2 }}
            action={
              staleLatest || !onLoadLatest ? null : (
                <AppButton size="small" tone="secondary" onClick={() => void onLoadLatest()}>
                  {t('gameCatalog.modifiers.loadLatest')}
                </AppButton>
              )
            }
          >
            {t('gameCatalog.modifiers.staleDraftPreserved')}
          </Alert>
        ) : null}
        {staleLatest ? (
          <Paper variant="outlined" sx={{ p: 1.5, mb: 2 }}>
            <Typography variant="subtitle2">
              {t('gameCatalog.modifiers.latestForComparison', {
                revision: staleLatest.revision,
              })}
            </Typography>
            <Typography>{staleLatest.name}</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap' }}>
              {staleLatest.description}
            </Typography>
            <Typography variant="caption">
              {t('gameCatalog.modifiers.latestCostAndLimit', {
                cost: staleLatest.activationCost,
                limit: staleLatest.activationLimit.count ?? t('gameCatalog.modifiers.unlimited'),
              })}
            </Typography>
          </Paper>
        ) : null}
        {isReadOnly ? (
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('gameCatalog.modifiers.contentLockedReason')}
          </Alert>
        ) : null}
        <form id={modifierFormId} onSubmit={(event) => void submit(event)}>
          {step === 0 ? <CardStep control={control} disabled={disabled} /> : null}
          {step === 1 ? (
            <ActivationStep
              control={control}
              disabled={disabled}
              initial={initial}
              kind={kind}
              modifiers={modifiers}
              setValue={setValue}
            />
          ) : null}
          {step === 2 ? (
            <ImpactStep control={control} disabled={disabled} setValue={setValue} />
          ) : null}
          {step === 3 ? (
            <Stack spacing={2}>
              <ReviewStep
                preview={preview}
                isLoading={isPreviewLoading}
                error={previewError}
                onRetry={() => void loadPreview()}
              />
              {!isReadOnly ? (
                <Controller
                  name="changeNote"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label={t('gameCatalog.modifiers.fields.changeNote')}
                      helperText={
                        fieldState.error?.message ??
                        t('gameCatalog.modifiers.fields.changeNoteHint')
                      }
                      error={Boolean(fieldState.error)}
                      multiline
                      minRows={2}
                      inputProps={{ maxLength: 500 }}
                    />
                  )}
                />
              ) : null}
            </Stack>
          ) : null}
        </form>
      </AppDialog>
      <ConfirmDialog
        open={showDiscardConfirmation}
        title={t('gameCatalog.modifiers.wizard.discardTitle')}
        description={t('gameCatalog.modifiers.wizard.discardDescription')}
        confirmLabel={t('gameCatalog.modifiers.wizard.discardConfirm')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        onClose={() => setShowDiscardConfirmation(false)}
        onConfirm={onClose}
      />
    </>
  )
}

export function ModifierFormDialog({ open, ...props }: ModifierFormDialogProps) {
  return open ? <ModifierFormDialogBody {...props} /> : null
}
