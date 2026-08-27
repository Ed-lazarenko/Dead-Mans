import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Box, Chip, CircularProgress, Divider, Stack, Typography } from '@mui/material'
import { useEffect, useId, useMemo, useRef, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import {
  AppButton,
  AppDialog,
  ConfirmDialog,
  ControlledFormTextField,
  FormSelect,
  ParticipantNamesList,
  RoundScoreBreakdown,
  SectionCard,
} from '../../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'
import {
  buildModifierRuntimeUnits,
  calculateModifierRuntimeClock,
  formatRuntimeDuration,
} from '../../game-modifiers/model/modifier-runtime.ts'
import { previewGameRoundScore } from '../../game-rounds/api/game-rounds-api.ts'
import {
  buildCompleteRoundInput,
  buildGameRoundPreviewRequest,
  buildGameRoundSummaryDefaultValues,
  gameRoundPostRoundActions,
  gameRoundRuleOutcomeStatuses,
  gameRoundSummaryFormSchema,
  serializeGameRoundPreviewInput,
  type CompleteRoundInput,
  type GameRoundPostRoundAction,
  type GameRoundSummaryFormValues,
} from '../model/game-round-summary-form.ts'

type GameRoundDetails = components['schemas']['GameRoundDetailsDto']
type ScorePreview = components['schemas']['GameRoundScorePreviewDto']
type PreviewStatus = 'incomplete' | 'debouncing' | 'loading' | 'success' | 'error' | 'stale'

interface PreviewState {
  status: PreviewStatus
  data: ScorePreview | null
  inputKey: string | null
  errorCode: string | null
}

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
  const [previewState, setPreviewState] = useState<PreviewState>({
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
  const displayedPreviewState: PreviewState = !previewInputKey
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
          const errorCode = getApiErrorCode(error)
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

            <RoundContext activeRound={activeRound} />

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
              <SummarySection title={t('gameBoard.roundSummaryRulesTitle')}>
                {defaultValues.ruleGroups.map((group, index) => (
                  <RuleGroupCard key={group.resolutionGroupId} index={index} control={control} />
                ))}
              </SummarySection>
            ) : null}

            {defaultValues.scoringInstances.length > 0 ? (
              <SummarySection title={t('gameBoard.roundSummaryConditionsTitle')}>
                {defaultValues.scoringInstances.map((instance, index) => (
                  <ScoringInstanceCard
                    key={instance.modifierResultId}
                    index={index}
                    control={control}
                  />
                ))}
              </SummarySection>
            ) : null}

            {defaultValues.automaticInstances.length > 0 ? (
              <SummarySection title={t('gameBoard.roundSummaryAutomaticTitle')}>
                {defaultValues.automaticInstances.map((instance) => (
                  <SectionCard key={instance.modifierResultId} inset>
                    <ModifierHeading
                      name={instance.modifierName}
                      index={instance.activationIndex}
                      count={instance.activationCount}
                    />
                    <Typography variant="body2" color="text.secondary">
                      {t('gameBoard.roundSummaryAutomaticHint')}
                    </Typography>
                  </SectionCard>
                ))}
              </SummarySection>
            ) : null}

            {activeRound.modifierResults.length === 0 ? (
              <SectionCard inset>
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.roundSummaryNoModifiers')}
                </Typography>
              </SectionCard>
            ) : null}

            <PreviewSection state={displayedPreviewState} score={scorePreview} />
            <PostRoundSection control={control} />
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

function RoundContext({ activeRound }: { activeRound: GameRoundDetails }) {
  const { t } = useTranslation()
  const gameplayDuration = getGameplayDurationSeconds(activeRound)
  const expiredTimers = buildModifierRuntimeUnits(activeRound).filter(
    (unit) =>
      unit.durationSeconds !== null &&
      calculateModifierRuntimeClock(
        activeRound,
        unit.durationSeconds,
        Date.parse(activeRound.serverNowUtc),
      ).state === 'expired',
  )
  return (
    <Stack direction="row" spacing={1} alignItems="flex-start" flexWrap="wrap" useFlexGap>
      <Chip
        size="small"
        variant="outlined"
        label={formatTeamNameWithFallback(
          activeRound.teamName,
          t('common.teamWithSlot', { slot: activeRound.teamSlotIndex }),
        )}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryRoundVersion', { version: activeRound.roundVersion })}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryCard', {
          card: activeRound.cellTitle ?? t('gameBoard.roundSummaryCardFallback'),
        })}
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryFrozenCardValue', { value: activeRound.baseScore })}
      />
      {gameplayDuration !== null ? (
        <Chip
          size="small"
          variant="outlined"
          label={t('gameBoard.roundSummaryGameplayDuration', {
            duration: formatRuntimeDuration(gameplayDuration),
          })}
        />
      ) : null}
      {expiredTimers.map((timer) => (
        <Chip
          key={timer.key}
          size="small"
          color="warning"
          variant="outlined"
          label={t('gameBoard.roundSummaryExpiredTimer', { modifier: timer.modifierName })}
        />
      ))}
      <Stack spacing={0.35}>
        <Typography variant="caption" color="text.secondary">
          {t('common.entities.players')}
        </Typography>
        <ParticipantNamesList
          names={activeRound.participants.map((participant) => participant.displayName)}
          emptyLabel={t('gameBoard.roundSummaryNoParticipants')}
        />
      </Stack>
    </Stack>
  )
}

function getGameplayDurationSeconds(round: GameRoundDetails) {
  if (!round.gameplayStartedAtUtc) return null
  const startedAtMs = Date.parse(round.gameplayStartedAtUtc)
  const stoppedAtMs = Date.parse(round.reviewedAtUtc ?? round.finishedAtUtc ?? round.serverNowUtc)
  if (!Number.isFinite(startedAtMs) || !Number.isFinite(stoppedAtMs)) return null
  return Math.max(0, Math.floor((stoppedAtMs - startedAtMs) / 1_000))
}

function SummarySection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Stack spacing={1.5}>
      <Typography variant="subtitle2">{title}</Typography>
      {children}
    </Stack>
  )
}

function RuleGroupCard({
  index,
  control,
}: {
  index: number
  control: ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']
}) {
  const { t } = useTranslation()
  const group = useWatch({ control, name: `ruleGroups.${index}` })
  if (!group) return null
  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
          <Typography variant="subtitle2">{group.modifierName}</Typography>
          <Chip
            size="small"
            color="secondary"
            variant="outlined"
            label={t('gameBoard.roundSummaryModifierStackCount', {
              count: group.memberResultIds.length,
            })}
          />
        </Stack>
        {group.modifierDescription ? (
          <Typography variant="body2" color="text.secondary">
            {group.modifierDescription}
          </Typography>
        ) : null}
        <Typography variant="caption" color="text.secondary">
          {t('gameBoard.roundSummaryRuleMembers', {
            members: group.memberActivationIds
              .map((id, memberIndex) => `#${memberIndex + 1} · ${shortId(id)}`)
              .join(', '),
          })}
        </Typography>
        <Controller
          control={control}
          name={`ruleGroups.${index}.outcomeStatus`}
          render={({ field, fieldState }) => (
            <FormSelect
              label={t('gameBoard.roundSummaryRuleStatus')}
              value={field.value ?? ''}
              onChange={(value) => field.onChange(value || null)}
              error={fieldState.invalid}
              helperText={fieldState.invalid ? t('gameBoard.roundSummaryRequired') : undefined}
              options={gameRoundRuleOutcomeStatuses.map((status) => ({
                value: status,
                label: t(`gameBoard.roundSummaryRuleStatusOption.${status}`),
              }))}
            />
          )}
        />
        {group.outcomeStatus === 'violated' ? (
          <ControlledFormTextField
            control={control}
            name={`ruleGroups.${index}.violationComment`}
            label={t('gameBoard.roundSummaryViolationComment')}
            multiline
            minRows={2}
          />
        ) : null}
      </Stack>
    </SectionCard>
  )
}

function ScoringInstanceCard({
  index,
  control,
}: {
  index: number
  control: ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']
}) {
  const { t } = useTranslation()
  const instance = useWatch({ control, name: `scoringInstances.${index}` })
  if (!instance) return null
  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        {instance.memberResultIds.length > 1 ? (
          <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle2">{instance.modifierName}</Typography>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.roundSummaryModifierStackCount', {
                count: instance.memberResultIds.length,
              })}
            />
          </Stack>
        ) : (
          <ModifierHeading
            name={instance.modifierName}
            index={instance.activationIndex}
            count={instance.activationCount}
          />
        )}
        {instance.modifierDescription ? (
          <Typography variant="body2" color="text.secondary">
            {instance.modifierDescription}
          </Typography>
        ) : null}
        {instance.resolutionKind === 'boolean' ? (
          <Controller
            control={control}
            name={`scoringInstances.${index}.isConditionMet`}
            render={({ field, fieldState }) => (
              <FormSelect
                label={t('gameBoard.roundSummaryModifierConditionToggle')}
                value={field.value === null ? '' : String(field.value)}
                onChange={(value) => field.onChange(value === '' ? null : value === 'true')}
                error={fieldState.invalid}
                helperText={fieldState.invalid ? t('gameBoard.roundSummaryRequired') : undefined}
                options={[
                  { value: 'true', label: t('gameBoard.roundSummaryModifierConditionMet') },
                  { value: 'false', label: t('gameBoard.roundSummaryModifierConditionMissed') },
                ]}
              />
            )}
          />
        ) : (
          <ControlledFormTextField
            control={control}
            name={`scoringInstances.${index}.countValue`}
            type="number"
            label={instance.inputLabel ?? t('gameBoard.roundSummaryCountValue')}
            helperText={
              instance.maximumKind === 'activations' && instance.maximumPerActivation !== null
                ? t('gameBoard.roundSummaryActivationCountLimit', {
                    count: instance.memberResultIds.length * instance.maximumPerActivation,
                  })
                : undefined
            }
            inputProps={{
              min: 0,
              ...(instance.maximumKind === 'activations' && instance.maximumPerActivation !== null
                ? { max: instance.memberResultIds.length * instance.maximumPerActivation }
                : {}),
            }}
          />
        )}
      </Stack>
    </SectionCard>
  )
}

function ModifierHeading({ name, index, count }: { name: string; index: number; count: number }) {
  const { t } = useTranslation()
  return (
    <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
      <Typography variant="subtitle2">{name}</Typography>
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryActivationLabel', { index, count })}
      />
    </Stack>
  )
}

function PreviewSection({
  state,
  score,
}: {
  state: PreviewState
  score: GameRoundDetails['scoreDetails'] | null | undefined
}) {
  const { t } = useTranslation()
  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.roundSummaryScoreTitle')}</Typography>
        <Divider />
        {state.status === 'incomplete' ? (
          <Alert severity="warning" variant="outlined">
            {t('gameBoard.roundSummaryPreviewIncomplete')}
          </Alert>
        ) : null}
        {state.status === 'debouncing' || state.status === 'loading' ? (
          <Alert severity="info" variant="outlined" icon={<CircularProgress size={18} />}>
            {t(
              state.status === 'debouncing'
                ? 'gameBoard.roundSummaryPreviewWaiting'
                : 'gameBoard.roundSummaryPreviewLoading',
            )}
          </Alert>
        ) : null}
        {state.status === 'error' ? (
          <Alert severity="error" variant="outlined">
            {t('gameBoard.roundSummaryPreviewFailed', {
              reason: state.errorCode ?? t('gameBoard.roundSummaryPreviewFailedFallback'),
            })}
          </Alert>
        ) : null}
        {state.status === 'stale' ? (
          <Alert severity="error" variant="outlined">
            {t('gameBoard.roundSummaryPreviewStale')}
          </Alert>
        ) : null}
        {state.status === 'success' && score ? (
          <>
            <SummaryMetric
              label={t('gameBoard.roundSummaryScoreUnit')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.scoreUnit })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryKillsScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.killsScore })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryBountiesScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.bountyScore })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryModifierKills')}
              value={t('gameBoard.roundSummaryModifierKillsValue', {
                kills: score.modifierKillDelta,
                score: score.modifierKillScore,
              })}
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryModifierPoints')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.modifierScoreDelta })}
            />
            {score.emptyCardPenaltyScore ? (
              <SummaryMetric
                label={t('gameBoard.roundSummaryEmptyCardPenalty')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: score.emptyCardPenaltyScore,
                })}
              />
            ) : null}
            <SummaryMetric
              label={t('gameBoard.roundSummaryTotalKills')}
              value={String(score.totalKillCount)}
              emphasize
            />
            <SummaryMetric
              label={t('gameBoard.roundSummaryFinalScore')}
              value={t('gameBoard.roundSummaryScoreValue', { value: score.finalScore })}
              emphasize
            />
            <RoundScoreBreakdown score={score} />
            {state.data?.calculationTrace.length ? (
              <Stack spacing={0.75}>
                <Typography variant="caption" color="text.secondary">
                  {t('gameBoard.roundSummaryTraceTitle')}
                </Typography>
                {state.data.calculationTrace.map((trace) => (
                  <Stack
                    key={trace.modifierResultId}
                    direction="row"
                    spacing={1}
                    justifyContent="space-between"
                  >
                    <Typography variant="caption">
                      {trace.formulaCode ?? trace.resolutionKind}
                    </Typography>
                    <Typography variant="caption" fontWeight={700}>
                      {t('gameBoard.roundSummaryTraceDelta', {
                        points: trace.pointsDelta,
                        kills: trace.bonusKillsDelta,
                      })}
                    </Typography>
                  </Stack>
                ))}
              </Stack>
            ) : null}
          </>
        ) : null}
      </Stack>
    </SectionCard>
  )
}

function PostRoundSection({
  control,
}: {
  control: ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']
}) {
  const { t } = useTranslation()
  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Typography variant="subtitle2">{t('gameBoard.roundSummaryPostRoundTitle')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {t('gameBoard.roundSummaryPostRoundDescription')}
        </Typography>
        <Controller
          control={control}
          name="postRoundAction"
          render={({ field }) => (
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25}>
              {gameRoundPostRoundActions.map((action) => (
                <AppButton
                  key={action}
                  type="button"
                  tone={field.value === action ? 'primary' : 'secondary'}
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
              ))}
            </Stack>
          )}
        />
      </Stack>
    </SectionCard>
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

function shortId(value: string) {
  return value.length <= 8 ? value : value.slice(-8)
}

function getApiErrorCode(error: unknown) {
  if (!(error instanceof ApiError) || !error.details || typeof error.details !== 'object')
    return null
  const code = Reflect.get(error.details, 'code')
  return typeof code === 'string' ? code : null
}
