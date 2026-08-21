import { zodResolver } from '@hookform/resolvers/zod'
import {
  Alert,
  Autocomplete,
  Box,
  Checkbox,
  Chip,
  FormControlLabel,
  LinearProgress,
  MenuItem,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import type { Control, FieldPath } from 'react-hook-form'
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
  getCompatibleModifierFormulaCodes,
  getCompatibleResolutionKinds,
  modifierKinds,
  modifierPerformers,
  modifierPhases,
  modifierRewards,
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
  onClose: () => void
  onSubmit: (request: CreateGameModifierRequest) => Promise<void>
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

function WizardProgress({ step }: { step: number }) {
  const { t } = useTranslation()
  return (
    <Box sx={{ mb: 2 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 1 }}>
        <Typography variant="subtitle2">
          {t('gameCatalog.modifiers.wizard.step', { current: step + 1, total: 4 })}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t(`gameCatalog.modifiers.wizard.steps.${step}`)}
        </Typography>
      </Stack>
      <LinearProgress variant="determinate" value={(step + 1) * 25} aria-hidden />
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
  return (
    <Stack spacing={1.5}>
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
      <ControlledFormTextField
        control={control}
        name="name"
        label={t('gameCatalog.modifiers.fields.name')}
        disabled={disabled}
      />
      <ControlledFormTextField
        control={control}
        name="description"
        label={t('gameCatalog.modifiers.fields.description')}
        multiline
        minRows={3}
        disabled={disabled}
      />
      <ControlledFormTextField
        control={control}
        name="iconEmoji"
        label={t('gameCatalog.modifiers.fields.iconEmoji')}
        disabled={disabled}
      />
      <ModifierTagField control={control} disabled={disabled} />
    </Stack>
  )
}

function ActivationStep({
  control,
  disabled,
  initial,
  modifiers,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
  initial?: GameModifierDefinition
  modifiers: GameModifierDefinition[]
}) {
  const { t } = useTranslation()
  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <ControlledFormTextField
          control={control}
          name="activationCost"
          type="number"
          label={t('gameCatalog.modifiers.fields.activationCost')}
          disabled={disabled}
        />
        <ControlledFormTextField
          control={control}
          name="activationLimitCount"
          type="number"
          label={t('gameCatalog.modifiers.fields.activationLimitCount')}
          helperText={t('gameCatalog.modifiers.fields.limitHint')}
          disabled={disabled}
        />
      </Stack>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <ControlledFormTextField
          control={control}
          name="phase"
          select
          label={t('gameCatalog.modifiers.wizard.phase')}
          disabled={disabled}
        >
          {modifierPhases.map((phase) => (
            <MenuItem key={phase} value={phase}>
              {t(`gameCatalog.modifiers.wizard.phases.${phase}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
        <ControlledFormTextField
          control={control}
          name="performer"
          select
          label={t('gameCatalog.modifiers.wizard.performer')}
          disabled={disabled}
        >
          {modifierPerformers.map((performer) => (
            <MenuItem key={performer} value={performer}>
              {t(`gameCatalog.modifiers.wizard.performers.${performer}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
      </Stack>
      <ControlledFormTextField
        control={control}
        name="rule"
        label={t('gameCatalog.modifiers.wizard.rule')}
        multiline
        minRows={3}
        disabled={disabled}
      />
      <Controller
        control={control}
        name="requiresHostMonitoring"
        render={({ field }) => (
          <FormControlLabel
            control={
              <Checkbox
                checked={field.value}
                onChange={(event) => field.onChange(event.target.checked)}
                disabled={disabled}
              />
            }
            label={t('gameCatalog.modifiers.wizard.requiresHostMonitoring')}
          />
        )}
      />
      <ControlledFormTextField
        control={control}
        name="durationSeconds"
        type="number"
        label={t('gameCatalog.modifiers.fields.durationSeconds')}
        helperText={t('gameCatalog.modifiers.wizard.durationHint')}
        disabled={disabled}
      />
      <ModifierConflictField
        control={control}
        currentModifierId={initial?.id}
        disabled={disabled}
        modifiers={modifiers}
      />
      <ControlledFormTextField
        control={control}
        name="activationCommand"
        label={t('gameCatalog.modifiers.fields.activationCommand')}
        helperText={t('gameCatalog.modifiers.wizard.commandHint')}
        disabled={disabled}
      />
    </Stack>
  )
}

function ImpactStep({
  control,
  disabled,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
}) {
  const { t } = useTranslation()
  const reward = useWatch({ control, name: 'reward' })
  const resolutionKind = useWatch({ control, name: 'resolutionKind' })
  const formulaCode = useWatch({ control, name: 'formulaCode' })
  const resolutionKinds = getCompatibleResolutionKinds(reward)
  const formulas = getCompatibleModifierFormulaCodes(reward, resolutionKind)

  return (
    <Stack spacing={1.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <ControlledFormTextField
          control={control}
          name="reward"
          select
          label={t('gameCatalog.modifiers.wizard.reward')}
          disabled={disabled}
        >
          {modifierRewards.map((value) => (
            <MenuItem key={value} value={value}>
              {t(`gameCatalog.modifiers.wizard.rewards.${value}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
        <ControlledFormTextField
          control={control}
          name="resolutionKind"
          select
          label={t('gameCatalog.modifiers.wizard.resolution')}
          disabled={disabled}
        >
          {resolutionKinds.map((value) => (
            <MenuItem key={value} value={value}>
              {t(`gameCatalog.modifiers.wizard.resolutions.${value}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
      </Stack>
      <ControlledFormTextField
        control={control}
        name="formulaCode"
        select
        label={t('gameCatalog.modifiers.wizard.formula')}
        helperText={t('gameCatalog.modifiers.wizard.formulaHint')}
        disabled={disabled}
      >
        {formulas.map((code) => (
          <MenuItem key={code} value={code}>
            {t(`gameCatalog.modifiers.wizard.formulas.${code}`)}
          </MenuItem>
        ))}
      </ControlledFormTextField>
      {formulaCode === 'growing_kill_value' ? (
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <ControlledFormTextField
            control={control}
            name="incrementPointsPerKill"
            type="number"
            label={t('gameCatalog.modifiers.wizard.parameters.incrementPointsPerKill')}
            disabled={disabled}
          />
          <ControlledFormTextField
            control={control}
            name="zeroKillPenaltyPoints"
            type="number"
            label={t('gameCatalog.modifiers.wizard.parameters.zeroKillPenaltyPoints')}
            disabled={disabled}
          />
        </Stack>
      ) : null}
      {formulaCode === 'bonus_kill_on_condition' ? (
        <ControlledFormTextField
          control={control}
          name="successBonusKills"
          type="number"
          label={t('gameCatalog.modifiers.wizard.parameters.successBonusKills')}
          disabled={disabled}
        />
      ) : null}
      {formulaCode === 'bonus_kills_by_count' ? (
        <ControlledFormTextField
          control={control}
          name="bonusKillsPerUnit"
          type="number"
          label={t('gameCatalog.modifiers.wizard.parameters.bonusKillsPerUnit')}
          disabled={disabled}
        />
      ) : null}
      {formulaCode === 'window_kill_bonus_points' ? (
        <ControlledFormTextField
          control={control}
          name="bonusRate"
          label={t('gameCatalog.modifiers.wizard.parameters.bonusRate')}
          disabled={disabled}
        />
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
          {t('gameCatalog.modifiers.wizard.exampleFacts', preview.example)}
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.wizard.exampleResult', preview.example)}
        </Typography>
      </Alert>
    </Stack>
  )
}

const stepFields: Record<number, FieldPath<ModifierFormValues>[]> = {
  0: ['kind', 'name', 'description', 'iconEmoji', 'tags'],
  1: [
    'activationCost',
    'activationLimitCount',
    'phase',
    'performer',
    'rule',
    'durationSeconds',
    'activationCommand',
  ],
  2: [
    'reward',
    'resolutionKind',
    'formulaCode',
    'incrementPointsPerKill',
    'zeroKillPenaltyPoints',
    'successBonusKills',
    'bonusKillsPerUnit',
    'bonusRate',
  ],
  3: [],
}

function ModifierFormDialogBody({
  mode,
  initial,
  modifiers,
  isBusy,
  isReadOnly = false,
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
        formula: t('gameCatalog.validation.formula'),
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
  const reward = useWatch({ control, name: 'reward' })
  const resolutionKind = useWatch({ control, name: 'resolutionKind' })
  const formulaCode = useWatch({ control, name: 'formulaCode' })
  const disabled = isBusy || isReadOnly
  const isDirty = formState.isDirty

  useEffect(() => {
    const compatibleResolutions = getCompatibleResolutionKinds(reward)
    if (!compatibleResolutions.includes(resolutionKind)) {
      const nextResolution = compatibleResolutions[0]
      if (nextResolution) {
        setValue('resolutionKind', nextResolution, { shouldDirty: true, shouldValidate: true })
      }
      return
    }
    const compatibleFormulas = getCompatibleModifierFormulaCodes(reward, resolutionKind)
    if (!compatibleFormulas.includes(formulaCode)) {
      const nextFormula = compatibleFormulas[0]
      if (nextFormula) {
        setValue('formulaCode', nextFormula, { shouldDirty: true, shouldValidate: true })
      }
    }
  }, [formulaCode, resolutionKind, reward, setValue])

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
        <WizardProgress step={step} />
        {formState.errors.root ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            {formState.errors.root.message}
          </Alert>
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
              modifiers={modifiers}
            />
          ) : null}
          {step === 2 ? <ImpactStep control={control} disabled={disabled} /> : null}
          {step === 3 ? (
            <ReviewStep
              preview={preview}
              isLoading={isPreviewLoading}
              error={previewError}
              onRetry={() => void loadPreview()}
            />
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
