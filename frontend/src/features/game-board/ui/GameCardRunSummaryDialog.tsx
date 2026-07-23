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
  gameCardRunModifierOutcomeStatuses,
  gameCardRunSummaryFormSchema,
  type CompleteRoundInput,
  type GameCardRunSummaryFormValues,
} from '../model/game-card-run-summary-form.ts'

type GameCardRunDetails = components['schemas']['GameCardRunDetailsDto']

interface GameCardRunSummaryDialogProps {
  open: boolean
  activeRun: GameCardRunDetails
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (input: CompleteRoundInput) => void | Promise<void>
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
  const scorePreview = buildGameCardRunScorePreview(activeRun.baseScore, {
    killsCount: watchedValues.killsCount ?? 0,
    bountyCount: watchedValues.bountyCount ?? 0,
    modifiers: watchedValues.modifiers ?? [],
  })

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
          <AppButton type="submit" form={formId} disabled={isSubmitting}>
            {t('gameBoard.runSummarySubmit')}
          </AppButton>
        </>
      }
    >
      <Box
        component="form"
        id={formId}
        onSubmit={handleSubmit(async (values) => {
          await onSubmit(buildCompleteRoundInput(activeRun, values))
        })}
      >
        <Stack spacing={2}>
          <Alert severity="info" variant="outlined">
            {t('gameBoard.runSummaryFormulaHint', {
              scoreUnit: activeRun.baseScore,
            })}
          </Alert>

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
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview.scoreUnit })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryKillsScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview.killsScore })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryBountiesScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview.bountyScore })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryModifierKills')}
                value={t('gameBoard.runSummaryModifierKillsValue', {
                  kills: scorePreview.modifierKillDelta,
                  score: scorePreview.modifierKillScore,
                })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryModifierPoints')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview.modifierScoreDelta })}
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryTotalKills')}
                value={String(scorePreview.totalKillCount)}
                emphasize
              />
              <SummaryMetric
                label={t('gameBoard.runSummaryFinalScore')}
                value={t('gameBoard.runSummaryScoreValue', { value: scorePreview.finalScore })}
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
                  <Stack spacing={1.25}>
                    <Typography variant="subtitle2">{field.modifierName}</Typography>

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

                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
                      <ControlledFormTextField
                        control={control}
                        name={`modifiers.${index}.killDelta`}
                        type="number"
                        label={t('gameBoard.runSummaryModifierKillsDelta')}
                      />
                      <ControlledFormTextField
                        control={control}
                        name={`modifiers.${index}.scoreDelta`}
                        type="number"
                        label={t('gameBoard.runSummaryModifierScoreDelta')}
                      />
                    </Stack>
                  </Stack>
                </SectionCard>
              ))
            )}
          </Stack>
        </Stack>
      </Box>
    </AppDialog>
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
