import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Paper, Stack, TextField, Typography, useMediaQuery, useTheme } from '@mui/material'
import { useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import type { FieldPath } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
  GameModifierDraftPreview,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, AppDialog, ConfirmDialog } from '../../../shared/ui/index.ts'
import { previewGameModifier } from '../api/catalog-modifiers-api.ts'
import {
  createDefaultModifierFormValues,
  createModifierFormSchema,
  toModifierRequest,
  type ModifierFormValues,
} from '../model/modifier-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'
import { ModifierActivationStep } from './ModifierActivationStep.tsx'
import { ModifierCardStep } from './ModifierCardStep.tsx'
import { ModifierImpactStep } from './ModifierImpactStep.tsx'
import { ModifierReviewStep } from './ModifierReviewStep.tsx'
import { ModifierWizardProgress } from './modifier-form-fields.tsx'

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
        <ModifierWizardProgress step={step} kind={kind} />
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
          {step === 0 ? <ModifierCardStep control={control} disabled={disabled} /> : null}
          {step === 1 ? (
            <ModifierActivationStep
              control={control}
              disabled={disabled}
              initial={initial}
              kind={kind}
              modifiers={modifiers}
              setValue={setValue}
            />
          ) : null}
          {step === 2 ? (
            <ModifierImpactStep control={control} disabled={disabled} setValue={setValue} />
          ) : null}
          {step === 3 ? (
            <Stack spacing={2}>
              <ModifierReviewStep
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
