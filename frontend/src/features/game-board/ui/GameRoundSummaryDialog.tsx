import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Divider, Stack, Typography } from '@mui/material'
import { useEffect, useId, useMemo, useRef, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import {
  AppButton,
  AppDialog,
  ConfirmDialog,
  ControlledFormTextField,
  SectionCard,
} from '../../../shared/ui/index.ts'
import { previewGameRoundScore } from '../../game-rounds/api/game-rounds-api.ts'
import { getGameRoundPreviewErrorCode } from '../model/game-round-preview-error.ts'
import {
  buildCompleteRoundInput,
  buildGameRoundPreviewRequest,
  buildGameRoundSummaryDefaultValues,
  gameRoundSummaryFormSchema,
  serializeGameRoundPreviewInput,
  type CompleteRoundInput,
  type GameRoundPostRoundAction,
  type GameRoundSummaryFormValues,
} from '../model/game-round-summary-form.ts'
import { GameRoundContext } from './GameRoundContext.tsx'
import { GameRoundPostRoundSection } from './GameRoundPostRoundSection.tsx'
import { GameRoundPreviewSection, type GameRoundPreviewState } from './GameRoundPreviewSection.tsx'
import {
  GameRoundModifierHeading,
  GameRoundRuleGroupCard,
  GameRoundScoringInstanceCard,
  GameRoundSummarySection,
} from './GameRoundResolutionFields.tsx'

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
  const requestSequence = useRef(0)
  const defaultValues = useMemo(
    () => buildGameRoundSummaryDefaultValues(activeRound),
    [activeRound],
  )
  const {
    control,
    getValues,
    handleSubmit,
    reset,
    formState: { isDirty },
  } = useForm<GameRoundSummaryFormValues>({
    resolver: zodResolver(gameRoundSummaryFormSchema),
    defaultValues,
    mode: 'onChange',
  })
  const watchedValues = useWatch({ control })
  const [previewState, setPreviewState] = useState<GameRoundPreviewState>({
    status: 'incomplete',
    data: null,
    inputKey: null,
    errorCode: null,
  })
  const [isCloseConfirmOpen, setIsCloseConfirmOpen] = useState(false)
  const parsedValues = useMemo(
    () => gameRoundSummaryFormSchema.safeParse(watchedValues),
    [watchedValues],
  )
  const previewInput = useMemo(
    () => (parsedValues.success ? buildCompleteRoundInput(activeRound, parsedValues.data) : null),
    [activeRound, parsedValues],
  )
  const previewInputKey = useMemo(
    () =>
      previewInput
        ? `${activeRound.roundId}:${serializeGameRoundPreviewInput(previewInput)}`
        : null,
    [activeRound.roundId, previewInput],
  )
  const isPreviewFresh =
    previewState.status === 'success' &&
    previewState.inputKey === previewInputKey &&
    previewState.data?.roundVersion === activeRound.roundVersion &&
    Boolean(previewState.data.normalizedInputHash.trim())
  const scorePreview = isPreviewFresh ? previewState.data?.scoreDetails : null
  const displayedPreviewState: GameRoundPreviewState = !previewInputKey
    ? { status: 'incomplete', data: null, inputKey: null, errorCode: null }
    : previewState.inputKey !== previewInputKey
      ? { status: 'debouncing', data: previewState.data, inputKey: null, errorCode: null }
      : previewState

  useEffect(() => {
    if (!open) return
    reset(defaultValues)
  }, [defaultValues, open, reset])

  useEffect(() => {
    const sequence = ++requestSequence.current
    if (!open) return
    if (!previewInput || !previewInputKey) return
    const timer = window.setTimeout(() => {
      if (requestSequence.current !== sequence) return
      setPreviewState((current) => ({
        ...current,
        status: 'loading',
        inputKey: previewInputKey,
        errorCode: null,
      }))
      previewGameRoundScore(activeRound.roundId, buildGameRoundPreviewRequest(previewInput))
        .then((data) => {
          if (requestSequence.current !== sequence) return
          if (data.roundVersion !== activeRound.roundVersion) {
            setPreviewState({
              status: 'stale',
              data: null,
              inputKey: previewInputKey,
              errorCode: null,
            })
            return
          }
          if (!data.normalizedInputHash.trim()) {
            setPreviewState({
              status: 'error',
              data: null,
              inputKey: previewInputKey,
              errorCode: null,
            })
            return
          }
          setPreviewState({
            status: 'success',
            data,
            inputKey: previewInputKey,
            errorCode: null,
          })
        })
        .catch((error: unknown) => {
          if (requestSequence.current !== sequence) return
          const errorCode = getGameRoundPreviewErrorCode(error)
          setPreviewState({
            status:
              errorCode === 'game_round.stale_version' ||
              (error instanceof ApiError && error.status === 409)
                ? 'stale'
                : 'error',
            data: null,
            inputKey: previewInputKey,
            errorCode,
          })
        })
    }, 350)

    return () => window.clearTimeout(timer)
  }, [activeRound.roundId, activeRound.roundVersion, open, previewInput, previewInputKey])

  const requestClose = () => {
    if (isDirty || JSON.stringify(getValues()) !== JSON.stringify(defaultValues)) {
      setIsCloseConfirmOpen(true)
      return
    }
    onClose()
  }

  return (
    <>
      <AppDialog
        open={open}
        onClose={isSubmitting ? undefined : requestClose}
        maxWidth="md"
        title={t('gameBoard.roundSummaryDialogTitle')}
        description={t('gameBoard.roundSummaryDialogDescription')}
        actions={
          <>
            <AppButton tone="ghost" onClick={requestClose} disabled={isSubmitting}>
              {t('common.actions.close')}
            </AppButton>
            <AppButton type="submit" form={formId} disabled={isSubmitting || !isPreviewFresh}>
              {t('gameBoard.roundSummarySubmit')}
            </AppButton>
          </>
        }
      >
        <Box
          component="form"
          id={formId}
          onSubmit={handleSubmit(async (values) => {
            const input = buildCompleteRoundInput(activeRound, values)
            if (
              previewState.status !== 'success' ||
              previewState.inputKey !==
                `${activeRound.roundId}:${serializeGameRoundPreviewInput(input)}` ||
              previewState.data?.roundVersion !== activeRound.roundVersion
            ) {
              return
            }
            await onSubmit({ roundSummary: input, postRoundAction: values.postRoundAction })
          })}
        >
          <Stack spacing={2}>
            <Alert severity="info" variant="outlined">
              {t('gameBoard.roundSummaryFormulaHint', { scoreUnit: activeRound.baseScore })}
            </Alert>

            <GameRoundContext activeRound={activeRound} />

            <SectionCard inset>
              <Stack spacing={1.5}>
                <Typography variant="subtitle2">
                  {t('gameBoard.roundSummaryResultTitle')}
                </Typography>
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
                <ControlledFormTextField
                  control={control}
                  name="notes"
                  label={t('gameBoard.roundSummaryNotes')}
                  multiline
                  minRows={2}
                  inputProps={{ maxLength: 2000 }}
                  helperText={t('gameBoard.roundSummaryNotesHint')}
                />
              </Stack>
            </SectionCard>

            {defaultValues.ruleGroups.length > 0 ? (
              <GameRoundSummarySection title={t('gameBoard.roundSummaryRulesTitle')}>
                {defaultValues.ruleGroups.map((group, index) => (
                  <GameRoundRuleGroupCard
                    key={group.resolutionGroupId}
                    index={index}
                    control={control}
                  />
                ))}
              </GameRoundSummarySection>
            ) : null}

            {defaultValues.scoringInstances.length > 0 ? (
              <GameRoundSummarySection title={t('gameBoard.roundSummaryConditionsTitle')}>
                {defaultValues.scoringInstances.map((instance, index) => (
                  <GameRoundScoringInstanceCard
                    key={instance.modifierResultId}
                    index={index}
                    control={control}
                  />
                ))}
              </GameRoundSummarySection>
            ) : null}

            {defaultValues.automaticInstances.length > 0 ? (
              <GameRoundSummarySection title={t('gameBoard.roundSummaryAutomaticTitle')}>
                {defaultValues.automaticInstances.map((instance) => (
                  <SectionCard key={instance.modifierResultId} inset>
                    <GameRoundModifierHeading
                      name={instance.modifierName}
                      index={instance.activationIndex}
                      count={instance.activationCount}
                    />
                    <Typography variant="body2" color="text.secondary">
                      {t('gameBoard.roundSummaryAutomaticHint')}
                    </Typography>
                  </SectionCard>
                ))}
              </GameRoundSummarySection>
            ) : null}

            {activeRound.modifierResults.length === 0 ? (
              <SectionCard inset>
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.roundSummaryNoModifiers')}
                </Typography>
              </SectionCard>
            ) : null}

            <GameRoundPreviewSection state={displayedPreviewState} score={scorePreview} />
            <GameRoundPostRoundSection control={control} />
          </Stack>
        </Box>
      </AppDialog>

      <ConfirmDialog
        open={isCloseConfirmOpen}
        title={t('gameBoard.roundSummaryCloseConfirmTitle')}
        description={t('gameBoard.roundSummaryCloseConfirmDescription')}
        confirmLabel={t('gameBoard.roundSummaryCloseConfirmAction')}
        cancelLabel={t('gameBoard.roundSummaryCloseConfirmCancel')}
        confirmTone="danger"
        onClose={() => setIsCloseConfirmOpen(false)}
        onConfirm={() => {
          setIsCloseConfirmOpen(false)
          onClose()
        }}
      />
    </>
  )
}
