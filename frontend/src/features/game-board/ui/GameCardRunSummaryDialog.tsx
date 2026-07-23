import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { useEffect, useId, useMemo } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import {
  AppButton,
  AppDialog,
  ControlledFormTextField,
  FormSelect,
  SectionCard,
} from '../../../shared/ui/index.ts'
import {
  buildCompleteRoundInput,
  buildGameCardRunScorePreview,
  buildGameCardRunSummaryDefaultValues,
  gameCardRunPostRoundActions,
  gameCardRunModifierOutcomeStatuses,
  gameCardRunSummaryFormSchema,
  type CompleteRoundInput,
  type GameCardRunPostRoundAction,
  type GameCardRunSummaryFormValues,
} from '../model/game-card-run-summary-form.ts'

type GameCardRunDetails = components['schemas']['GameCardRunDetailsDto']

interface GameCardRunSummaryDialogProps {
  open: boolean
  activeRun: GameCardRunDetails
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (input: {
    roundSummary: CompleteRoundInput
    postRoundAction: GameCardRunPostRoundAction
  }) => void | Promise<void>
}

export function GameCardRunSummaryDialog({
  open,
  activeRun,
  isSubmitting,
  onClose,
  onSubmit,
}: GameCardRunSummaryDialogProps) {
  const { t } = useTranslation()
  const formId = useId()
  const defaultValues = useMemo(
    () => buildGameCardRunSummaryDefaultValues(activeRun),
    [activeRun],
  )
  const { control, handleSubmit, reset } = useForm<GameCardRunSummaryFormValues>({
    resolver: zodResolver(gameCardRunSummaryFormSchema),
    defaultValues,
  })
  const modifierFields = useFieldArray({
    control,
    name: 'modifiers',
  })
  const watchedValues = useWatch({ control })
  const previewState = useMemo(() => {
    try {
      return {
        scorePreview: buildGameCardRunScorePreview(activeRun.baseScore, {
          killsCount: watchedValues.killsCount ?? 0,
          bountyCount: watchedValues.bountyCount ?? 0,
          modifiers: watchedValues.modifiers ?? [],
        }),
        previewError: null,
      }
    } catch (error) {
      return {
        scorePreview: null,
        previewError:
          error instanceof Error
            ? error.message
            : t('gameBoard.runSummaryPreviewFailedFallback'),
      }
    }
  }, [activeRun.baseScore, t, watchedValues.bountyCount, watchedValues.killsCount, watchedValues.modifiers])
  const scorePreview = previewState.scorePreview

  useEffect(() => {
    if (!open) {
      return
    }

    reset(defaultValues)
  }, [defaultValues, open, reset])

  return (
    <AppDialog
      open={open}
      onClose={isSubmitting ? undefined : onClose}
      maxWidth="md"
      title={t('gameBoard.runSummaryDialogTitle')}
      description={t('gameBoard.runSummaryDialogDescription')}
      actions={
        <>
          <AppButton tone="ghost" onClick={onClose} disabled={isSubmitting}>
            {t('gameBoard.runSummaryClose')}
          </AppButton>
          <AppButton
            type="submit"
            form={formId}
            disabled={isSubmitting || previewState.previewError !== null}
          >
            {t('gameBoard.runSummarySubmit')}
          </AppButton>
        </>
      }
    >
      <Box
        component="form"
        id={formId}
        onSubmit={handleSubmit(async (values) => {
          if (previewState.previewError) {
            return
          }

          await onSubmit({
            roundSummary: buildCompleteRoundInput(activeRun, values),
            postRoundAction: values.postRoundAction,
          })
        })}
      >
        <Stack spacing={2}>
          <Alert severity="info" variant="outlined">
            {t('gameBoard.runSummaryFormulaHint', {
              scoreUnit: activeRun.baseScore,
            })}
          </Alert>

          {previewState.previewError ? (
            <Alert severity="error" variant="outlined">
              {t('gameBoard.runSummaryPreviewFailed', {
                reason: previewState.previewError,
              })}
            </Alert>
          ) : null}

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.teamQueueTeamTitle', {
                slot: activeRun.teamSlotIndex,
              })}
            />
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.runSummaryParticipants', {
                players:
                  activeRun.participants.length > 0
                    ? activeRun.participants.map((participant) => participant.displayName).join(', ')
                    : t('gameBoard.runSummaryNoParticipants'),
              })}
            />
          </Stack>

          <SectionCard inset>
            <Stack spacing={1.5}>
              <Typography variant="subtitle2">{t('gameBoard.runSummaryResultTitle')}</Typography>
              <Divider />
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
                <ControlledFormTextField
                  control={control}
                  name="killsCount"
                  type="number"
                  label={t('gameBoard.runSummaryKills')}
                  inputProps={{ min: 0 }}
                />
                <ControlledFormTextField
                  control={control}
                  name="bountyCount"
                  type="number"
                  label={t('gameBoard.runSummaryBounties')}
                  inputProps={{ min: 0 }}
                />
              </Stack>
            </Stack>
          </SectionCard>

          <SectionCard inset>
            <Stack spacing={1.25}>
              <Typography variant="subtitle2">{t('gameBoard.runSummaryScoreTitle')}</Typography>
              <Divider />
              <SummaryMetric
                label={t('gameBoard.runSummaryScoreUnit')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview?.scoreUnit ?? 0 })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryKillsScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview?.killsScore ?? 0 })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryBountiesScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview?.bountyScore ?? 0 })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryModifierKills')}
                value={t('gameBoard.runSummaryModifierKillsValue', {
                  kills: scorePreview?.modifierKillDelta ?? 0,
                  score: scorePreview?.modifierKillScore ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryModifierPoints')}
                value={t('gameBoard.runSummaryScoreValue', {
                  value: scorePreview?.modifierScoreDelta ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryTotalKills')}
                value={String(scorePreview?.totalKillCount ?? 0)}
                emphasize
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryFinalScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview?.finalScore ?? 0 })}
                emphasize
              />
            </Stack>
          </SectionCard>

          <Stack spacing={1.5}>
            <Typography variant="subtitle2">{t('gameBoard.runSummaryModifiersTitle')}</Typography>
            {modifierFields.fields.length === 0 ? (
              <SectionCard inset>
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.runSummaryNoModifiers')}
                </Typography>
              </SectionCard>
            ) : (
              modifierFields.fields.map((field, index) => (
                <SectionCard key={field.id} inset>
                  <ModifierSummaryCard
                    index={index}
                    control={control}
                    computedModifier={scorePreview?.computedModifiers[index] ?? null}
                  />
                </SectionCard>
              ))
            )}
          </Stack>

          <SectionCard inset>
            <Stack spacing={1.25}>
              <Typography variant="subtitle2">
                {t('gameBoard.runSummaryPostRoundTitle')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.runSummaryPostRoundDescription')}
              </Typography>

              <Controller
                control={control}
                name="postRoundAction"
                render={({ field }) => (
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
                    {gameCardRunPostRoundActions.map((action) => {
                      const isSelected = field.value === action

                      return (
                        <AppButton
                          key={action}
                          type="button"
                          tone={isSelected ? 'primary' : 'secondary'}
                          fullWidth
                          onClick={() => field.onChange(action)}
                          sx={{ minHeight: 58, justifyContent: 'flex-start', px: 1.5 }}
                        >
                          <Stack alignItems="flex-start" spacing={0.35}>
                            <Typography variant="subtitle2" fontWeight={800}>
                              {t(`gameBoard.runSummaryPostRoundOption.${action}.title`)}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" textAlign="left">
                              {t(`gameBoard.runSummaryPostRoundOption.${action}.description`)}
                            </Typography>
                          </Stack>
                        </AppButton>
                      )
                    })}
                  </Stack>
                )}
              />
            </Stack>
          </SectionCard>
        </Stack>
      </Box>
    </AppDialog>
  )
}

function ModifierSummaryCard({
  index,
  control,
  computedModifier,
}: {
  index: number
  control: ReturnType<typeof useForm<GameCardRunSummaryFormValues>>['control']
  computedModifier: ReturnType<typeof buildGameCardRunScorePreview>['computedModifiers'][number] | null
}) {
  const { t } = useTranslation()
  const modifier = useWatch({
    control,
    name: `modifiers.${index}`,
  })

  if (!modifier) {
    return null
  }

  return (
    <Stack spacing={1.25}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={1}
        alignItems={{ xs: 'flex-start', md: 'center' }}
        justifyContent="space-between"
      >
        <Typography variant="subtitle2">{modifier.modifierName}</Typography>
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          <Chip
            size="small"
            variant="outlined"
            label={t(`gameBoard.runSummaryModifierType.${modifier.roundSummaryType}`)}
          />
          {modifier.activationCount > 1 ? (
            <Chip
              size="small"
              color="secondary"
              variant="outlined"
              label={t('gameBoard.runSummaryModifierStackCount', {
                count: modifier.activationCount,
              })}
            />
          ) : null}
        </Stack>
      </Stack>

      <Typography variant="body2" color="text.secondary">
        {t(`gameBoard.runSummaryModifierTypeDescription.${modifier.roundSummaryType}`)}
      </Typography>

      {modifier.modifierDescription ? (
        <Box
          sx={(theme) => ({
            border: `1px solid ${theme.palette.divider}`,
            borderRadius: 1.25,
            px: 1,
            py: 0.9,
            backgroundColor: 'action.hover',
          })}
        >
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ display: 'block', mb: 0.35, textTransform: 'uppercase', letterSpacing: '0.04em' }}
          >
            {t('gameBoard.runSummaryModifierDescriptionLabel')}
          </Typography>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-line' }}>
            {modifier.modifierDescription}
          </Typography>
        </Box>
      ) : null}

      {modifier.activationCount > 1 ? (
        <Alert severity="info" variant="outlined">
          {t('gameBoard.runSummaryModifierStackHint', {
            count: modifier.activationCount,
          })}
        </Alert>
      ) : null}

      {modifier.roundSummaryType === 'auto_result' ? (
        <Alert severity="info" variant="outlined">
          {t('gameBoard.runSummaryModifierAutoResultHint')}
        </Alert>
      ) : null}

      {modifier.roundSummaryType === 'toggle_bonus' ? (
        <Controller
          control={control}
          name={`modifiers.${index}.isConditionMet`}
          render={({ field: selectField, fieldState }) => (
            <FormSelect
              label={t('gameBoard.runSummaryModifierConditionToggle')}
              value={selectField.value ? 'true' : 'false'}
              onChange={(value) => selectField.onChange(value === 'true')}
              error={fieldState.invalid}
              helperText={fieldState.error?.message}
              options={[
                {
                  value: 'false',
                  label: t('gameBoard.runSummaryModifierConditionMissed'),
                },
                {
                  value: 'true',
                  label: t('gameBoard.runSummaryModifierConditionMet'),
                },
              ]}
            />
          )}
        />
      ) : null}

      {modifier.roundSummaryType === 'counted_bonus' || modifier.roundSummaryType === 'kill_multiplier' ? (
        <ControlledFormTextField
          control={control}
          name={`modifiers.${index}.countValue`}
          type="number"
          label={t(`gameBoard.runSummaryModifierCountInput.${modifier.countInput ?? 'bonusKills'}`)}
          inputProps={{ min: 0 }}
        />
      ) : null}

      {modifier.roundSummaryType === 'manual_points' ? (
        <Stack spacing={1.25}>
          <Controller
            control={control}
            name={`modifiers.${index}.outcomeStatus`}
            render={({ field: selectField, fieldState }) => (
              <FormSelect
                label={t('gameBoard.runSummaryModifierStatus')}
                value={selectField.value}
                onChange={selectField.onChange}
                error={fieldState.invalid}
                helperText={fieldState.error?.message}
                options={gameCardRunModifierOutcomeStatuses.map((status) => ({
                  value: status,
                  label: t(`gameBoard.runSummaryModifierStatusOption.${status}`),
                }))}
              />
            )}
          />
          <ControlledFormTextField
            control={control}
            name={`modifiers.${index}.manualScoreDelta`}
            type="number"
            label={t('gameBoard.runSummaryModifierScoreDelta')}
          />
        </Stack>
      ) : null}

      {computedModifier ? (
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} flexWrap="wrap" useFlexGap>
          <Chip
            size="small"
            color="info"
            variant="outlined"
            label={t('gameBoard.runSummaryModifierKillsPreview', {
              value: computedModifier.killDelta,
            })}
          />
          <Chip
            size="small"
            color="success"
            variant="outlined"
            label={t('gameBoard.runSummaryModifierScorePreview', {
              value: computedModifier.scoreDelta,
            })}
          />
          <Chip
            size="small"
            variant="outlined"
            label={t(`gameBoard.runSummaryModifierStatusOption.${computedModifier.outcomeStatus}`)}
          />
        </Stack>
      ) : null}
    </Stack>
  )
}

function SummaryMetric({
  label,
  value,
  emphasize = false,
}: {
  label: string
  value: string
  emphasize?: boolean
}) {
  return (
    <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography variant={emphasize ? 'subtitle2' : 'body2'} fontWeight={emphasize ? 700 : 500}>
        {value}
      </Typography>
    </Stack>
  )
}
