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
  buildGameRoundScorePreview,
  buildGameRoundSummaryDefaultValues,
  gameRoundPostRoundActions,
  gameRoundModifierOutcomeStatuses,
  gameRoundSummaryFormSchema,
  type CompleteRoundInput,
  type GameRoundPostRoundAction,
  type GameRoundSummaryFormValues,
} from '../model/game-round-summary-form.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']

interface GameRoundSummaryDialogProps {
  open: boolean
  activeRound: GameRoundDetails
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (input: {
    roundSummary: CompleteRoundInput
    postRoundAction: GameRoundPostRoundAction
  }) => void | Promise<void>
}

export function GameRoundSummaryDialog({
  open,
  activeRound,
  isSubmitting,
  onClose,
  onSubmit,
}: GameRoundSummaryDialogProps) {
  const { t } = useTranslation()
  const formId = useId()
  const defaultValues = useMemo(
    () => buildGameRoundSummaryDefaultValues(activeRound),
    [activeRound],
  )
  const { control, handleSubmit, reset } = useForm<GameRoundSummaryFormValues>({
    resolver: zodResolver(gameRoundSummaryFormSchema),
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
        scorePreview: buildGameRoundScorePreview(activeRound.baseScore, {
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
          error instanceof Error ? error.message : t('gameBoard.roundSummaryPreviewFailedFallback'),
      }
    }
  }, [
    activeRound.baseScore,
    t,
    watchedValues.bountyCount,
    watchedValues.killsCount,
    watchedValues.modifiers,
  ])
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
      title={t('gameBoard.roundSummaryDialogTitle')}
      description={t('gameBoard.roundSummaryDialogDescription')}
      actions={
        <>
          <AppButton tone="ghost" onClick={onClose} disabled={isSubmitting}>
            {t('gameBoard.roundSummaryClose')}
          </AppButton>
          <AppButton
            type="submit"
            form={formId}
            disabled={isSubmitting || previewState.previewError !== null}
          >
            {t('gameBoard.roundSummarySubmit')}
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
            roundSummary: buildCompleteRoundInput(activeRound, values),
            postRoundAction: values.postRoundAction,
          })
        })}
      >
        <Stack spacing={2}>
          <Alert severity="info" variant="outlined">
            {t('gameBoard.roundSummaryFormulaHint', {
              scoreUnit: activeRound.baseScore,
            })}
          </Alert>

          {previewState.previewError ? (
            <Alert severity="error" variant="outlined">
              {t('gameBoard.roundSummaryPreviewFailed', {
                reason: previewState.previewError,
              })}
            </Alert>
          ) : null}

          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.teamQueueTeamTitle', {
                slot: activeRound.teamSlotIndex,
              })}
            />
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.roundSummaryParticipants', {
                players:
                  activeRound.participants.length > 0
                    ? activeRound.participants
                        .map((participant) => participant.displayName)
                        .join(', ')
                    : t('gameBoard.roundSummaryNoParticipants'),
              })}
            />
          </Stack>

          <SectionCard inset>
            <Stack spacing={1.5}>
              <Typography variant="subtitle2">{t('gameBoard.roundSummaryResultTitle')}</Typography>
              <Divider />
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
                <ControlledFormTextField
                  control={control}
                  name="killsCount"
                  type="number"
                  label={t('gameBoard.roundSummaryKills')}
                  inputProps={{ min: 0 }}
                />
                <ControlledFormTextField
                  control={control}
                  name="bountyCount"
                  type="number"
                  label={t('gameBoard.roundSummaryBounties')}
                  inputProps={{ min: 0 }}
                />
              </Stack>
            </Stack>
          </SectionCard>

          <SectionCard inset>
            <Stack spacing={1.25}>
              <Typography variant="subtitle2">{t('gameBoard.roundSummaryScoreTitle')}</Typography>
              <Divider />
              <SummaryMetric
                label={t('gameBoard.roundSummaryScoreUnit')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: scorePreview?.scoreUnit ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryKillsScore')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: scorePreview?.killsScore ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryBountiesScore')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: scorePreview?.bountyScore ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryModifierKills')}
                value={t('gameBoard.roundSummaryModifierKillsValue', {
                  kills: scorePreview?.modifierKillDelta ?? 0,
                  score: scorePreview?.modifierKillScore ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryModifierPoints')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: scorePreview?.modifierScoreDelta ?? 0,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryTotalKills')}
                value={String(scorePreview?.totalKillCount ?? 0)}
                emphasize
              />
              <SummaryMetric
                label={t('gameBoard.roundSummaryFinalScore')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: scorePreview?.finalScore ?? 0,
                })}
                emphasize
              />
            </Stack>
          </SectionCard>

          <Stack spacing={1.5}>
            <Typography variant="subtitle2">{t('gameBoard.roundSummaryModifiersTitle')}</Typography>
            {modifierFields.fields.length === 0 ? (
              <SectionCard inset>
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.roundSummaryNoModifiers')}
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
                {t('gameBoard.roundSummaryPostRoundTitle')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.roundSummaryPostRoundDescription')}
              </Typography>

              <Controller
                control={control}
                name="postRoundAction"
                render={({ field }) => (
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
                    {gameRoundPostRoundActions.map((action) => {
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
                              {t(`gameBoard.roundSummaryPostRoundOption.${action}.title`)}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" textAlign="left">
                              {t(`gameBoard.roundSummaryPostRoundOption.${action}.description`)}
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
  control: ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']
  computedModifier:
    | ReturnType<typeof buildGameRoundScorePreview>['computedModifiers'][number]
    | null
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
            label={t(`gameBoard.roundSummaryModifierType.${modifier.roundSummaryType}`)}
          />
          {modifier.activationCount > 1 ? (
            <Chip
              size="small"
              color="secondary"
              variant="outlined"
              label={t('gameBoard.roundSummaryModifierStackCount', {
                count: modifier.activationCount,
              })}
            />
          ) : null}
        </Stack>
      </Stack>

      <Typography variant="body2" color="text.secondary">
        {t(`gameBoard.roundSummaryModifierTypeDescription.${modifier.roundSummaryType}`)}
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
            {t('gameBoard.roundSummaryModifierDescriptionLabel')}
          </Typography>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-line' }}>
            {modifier.modifierDescription}
          </Typography>
        </Box>
      ) : null}

      {modifier.activationCount > 1 ? (
        <Alert severity="info" variant="outlined">
          {t('gameBoard.roundSummaryModifierStackHint', {
            count: modifier.activationCount,
          })}
        </Alert>
      ) : null}

      {modifier.roundSummaryType === 'auto_result' ? (
        <Alert severity="info" variant="outlined">
          {t('gameBoard.roundSummaryModifierAutoResultHint')}
        </Alert>
      ) : null}

      {modifier.roundSummaryType === 'toggle_bonus' ? (
        <Controller
          control={control}
          name={`modifiers.${index}.isConditionMet`}
          render={({ field: selectField, fieldState }) => (
            <FormSelect
              label={t('gameBoard.roundSummaryModifierConditionToggle')}
              value={selectField.value ? 'true' : 'false'}
              onChange={(value) => selectField.onChange(value === 'true')}
              error={fieldState.invalid}
              helperText={fieldState.error?.message}
              options={[
                {
                  value: 'false',
                  label: t('gameBoard.roundSummaryModifierConditionMissed'),
                },
                {
                  value: 'true',
                  label: t('gameBoard.roundSummaryModifierConditionMet'),
                },
              ]}
            />
          )}
        />
      ) : null}

      {modifier.roundSummaryType === 'counted_bonus' ||
      modifier.roundSummaryType === 'kill_multiplier' ? (
        <ControlledFormTextField
          control={control}
          name={`modifiers.${index}.countValue`}
          type="number"
          label={t(
            `gameBoard.roundSummaryModifierCountInput.${modifier.countInput ?? 'bonusKills'}`,
          )}
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
                label={t('gameBoard.roundSummaryModifierStatus')}
                value={selectField.value}
                onChange={selectField.onChange}
                error={fieldState.invalid}
                helperText={fieldState.error?.message}
                options={gameRoundModifierOutcomeStatuses.map((status) => ({
                  value: status,
                  label: t(`gameBoard.roundSummaryModifierStatusOption.${status}`),
                }))}
              />
            )}
          />
          <ControlledFormTextField
            control={control}
            name={`modifiers.${index}.manualScoreDelta`}
            type="number"
            label={t('gameBoard.roundSummaryModifierScoreDelta')}
          />
        </Stack>
      ) : null}

      {computedModifier ? (
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} flexWrap="wrap" useFlexGap>
          <Chip
            size="small"
            color="info"
            variant="outlined"
            label={t('gameBoard.roundSummaryModifierKillsPreview', {
              value: computedModifier.killDelta,
            })}
          />
          <Chip
            size="small"
            color="success"
            variant="outlined"
            label={t('gameBoard.roundSummaryModifierScorePreview', {
              value: computedModifier.scoreDelta,
            })}
          />
          <Chip
            size="small"
            variant="outlined"
            label={t(
              `gameBoard.roundSummaryModifierStatusOption.${computedModifier.outcomeStatus}`,
            )}
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
