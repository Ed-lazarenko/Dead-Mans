import { zodResolver } from '@hookform/resolvers/zod'
import {
  Alert,
  Autocomplete,
  Checkbox,
  FormControlLabel,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { createFilterOptions } from '@mui/material/Autocomplete'
import type { TextFieldProps } from '@mui/material'
import { Controller, useForm, useWatch } from 'react-hook-form'
import type { Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, ControlledFormTextField } from '../../../shared/ui/index.ts'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'
import { modifierCategoryCodes } from '../../game-modifiers/index.ts'
import { deriveModifierRoundSummaryMeta } from '../../game-modifiers/model/modifier-round-summary.ts'
import { buildModifierSearchText } from '../../game-modifiers/model/modifier-search.ts'
import { isCustomModifierScoreFormula } from '../../game-modifiers/model/modifier-score-formula.ts'
import {
  createModifierFormSchema,
  modifierAutoResultFormulas,
  modifierMechanicTypes,
  type ModifierAutoResultFormula,
  type ModifierFormValues,
  type ModifierMechanicType,
} from '../model/modifier-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const modifierFormId = 'catalog-modifier-form'
const filterConflictModifiers = createFilterOptions<GameModifierDefinition>({
  limit: 30,
  stringify: (modifier) => buildModifierSearchText(modifier),
})

function optionalNumber(value: string): number | null {
  const trimmed = value.trim()
  return trimmed.length === 0 ? null : Number.parseInt(trimmed, 10)
}

function optionalDecimal(value: string): number | null {
  const trimmed = value.trim().replace(',', '.')
  return trimmed.length === 0 ? null : Number.parseFloat(trimmed)
}

function splitCsv(value: string): string[] {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)
}

function boolString(value: boolean | null | undefined, fallback: boolean) {
  return (value ?? fallback) ? 'true' : 'false'
}

function deriveScoringType(mechanicType: ModifierMechanicType) {
  switch (mechanicType) {
    case 'restriction_with_reward':
      return 'conditional_bonus_penalty'
    case 'kill_counter':
      return 'conditional_bonus'
    case 'multiplier':
      return 'multiplier'
    default:
      return 'non_scoring'
  }
}

function deriveAutoResultFormula(
  initial: GameModifierDefinition | undefined,
): ModifierAutoResultFormula {
  if (!initial || initial.mechanicType !== 'restriction_with_reward') {
    return 'flat_per_kill'
  }

  return deriveModifierRoundSummaryMeta(initial).autoResultFormula ?? 'flat_per_kill'
}

function toDefaultValues(initial: GameModifierDefinition | undefined): ModifierFormValues {
  if (!initial) {
    return {
      name: '',
      description: '',
      category: 'round',
      requiresHostControl: false,
      mechanicType: 'rule_only',
      activationCost: '0',
      activationLimitCount: '',
      conflictingModifierIds: [],
      iconEmoji: '',
      activationCommand: '',
      durationSeconds: '',
      ruleText: '',
      perKillBonus: '',
      failurePenaltyPoints: '',
      autoResultFormula: 'flat_per_kill',
      autoResultSuccessExpression: '',
      autoResultFailureExpression: '',
      killDeltaMode: 'conditional_bonus_kill',
      killDeltaValue: '1',
      killCondition: '',
      excludedWeapons: '',
      multiplierTarget: 'kills',
      multiplierDelta: '',
      activeWindow: 'entire_round',
      stopCondition: '',
      mentorLoadoutText: '',
      mentorDurationSeconds: '',
      mentorCanBeRevived: 'false',
      mentorCanBeKilled: 'false',
      mentorKillsCreditToTeam: 'false',
    }
  }

  const effect = initial.effect
  return {
    name: initial.name,
    description: initial.description,
    category: initial.category,
    requiresHostControl: initial.requiresHostControl,
    mechanicType: initial.mechanicType,
    activationCost: String(initial.activationCost),
    activationLimitCount:
      initial.activationLimit.count == null ? '' : String(initial.activationLimit.count),
    conflictingModifierIds: initial.conflictingModifierIds,
    iconEmoji: initial.iconEmoji ?? '',
    activationCommand: initial.activationCommand ?? '',
    durationSeconds: effect.durationSeconds == null ? '' : String(effect.durationSeconds),
    ruleText: effect.ruleText ?? '',
    perKillBonus:
      effect.scoreImpact?.perKillBonus == null ? '' : String(effect.scoreImpact.perKillBonus),
    failurePenaltyPoints:
      effect.scoreImpact?.failurePenaltyPoints == null
        ? ''
        : String(effect.scoreImpact.failurePenaltyPoints),
    autoResultFormula: deriveAutoResultFormula(initial),
    autoResultSuccessExpression: effect.scoreImpact?.scoreFormula?.successExpression ?? '',
    autoResultFailureExpression: effect.scoreImpact?.scoreFormula?.failureExpression ?? '',
    killDeltaMode: effect.killEffect?.killDeltaMode ?? 'conditional_bonus_kill',
    killDeltaValue:
      effect.killEffect?.killDeltaValue == null ? '' : String(effect.killEffect.killDeltaValue),
    killCondition: effect.killEffect?.condition ?? '',
    excludedWeapons: effect.killEffect?.excludedWeapons.join(', ') ?? '',
    multiplierTarget: effect.multiplierEffect?.target ?? 'kills',
    multiplierDelta:
      effect.multiplierEffect?.delta == null ? '' : String(effect.multiplierEffect.delta),
    activeWindow: effect.multiplierEffect?.activeWindow ?? 'entire_round',
    stopCondition: effect.multiplierEffect?.stopCondition ?? '',
    mentorLoadoutText: effect.mentorEffect?.loadoutText ?? '',
    mentorDurationSeconds:
      effect.mentorEffect?.durationSeconds == null
        ? ''
        : String(effect.mentorEffect.durationSeconds),
    mentorCanBeRevived: boolString(effect.mentorEffect?.canBeRevived, false),
    mentorCanBeKilled: boolString(effect.mentorEffect?.canBeKilled, false),
    mentorKillsCreditToTeam: boolString(effect.mentorEffect?.killsCreditToTeam, false),
  }
}

function toRequest(values: ModifierFormValues): CreateGameModifierRequest {
  const limit = optionalNumber(values.activationLimitCount)
  const icon = values.iconEmoji.trim()
  const command = values.activationCommand.trim()
  const mechanicType = values.mechanicType
  const durationSeconds = optionalNumber(values.durationSeconds)
  const mentorDurationSeconds = optionalNumber(values.mentorDurationSeconds)
  const mentorKillsCreditToTeam =
    mechanicType === 'mentor' && values.mentorKillsCreditToTeam === 'true'
  const autoResultSuccessExpression = values.autoResultSuccessExpression.trim()
  const autoResultFailureExpression = values.autoResultFailureExpression.trim()
  const scoreImpact =
    mechanicType === 'restriction_with_reward'
      ? {
          pointsDelta: null,
          perKillBonus: optionalNumber(values.perKillBonus),
          failurePenaltyPoints: optionalNumber(values.failurePenaltyPoints),
          multiplierDelta: null,
          killDelta: null,
          scoreFormula: {
            mode: values.autoResultFormula,
            successExpression:
              values.autoResultFormula === 'custom_expression' && autoResultSuccessExpression !== ''
                ? autoResultSuccessExpression
                : null,
            failureExpression:
              values.autoResultFormula === 'custom_expression' && autoResultFailureExpression !== ''
                ? autoResultFailureExpression
                : null,
          },
        }
      : mechanicType === 'kill_counter'
        ? {
            pointsDelta: null,
            perKillBonus: null,
            failurePenaltyPoints: null,
            multiplierDelta: null,
            killDelta: optionalNumber(values.killDeltaValue),
            scoreFormula: null,
          }
        : mechanicType === 'multiplier'
          ? {
              pointsDelta: null,
              perKillBonus: null,
              failurePenaltyPoints: null,
              multiplierDelta: optionalDecimal(values.multiplierDelta),
              killDelta: null,
              scoreFormula: null,
            }
          : mentorKillsCreditToTeam
            ? {
                pointsDelta: null,
                perKillBonus: null,
                failurePenaltyPoints: null,
                multiplierDelta: null,
                killDelta: null,
                scoreFormula: null,
              }
            : null
  const killEffect = mentorKillsCreditToTeam
    ? {
        killDeltaMode: 'mentor_kills_as_team_kills',
        killDeltaValue: 1,
        condition: null,
        excludedWeapons: [],
      }
    : mechanicType === 'kill_counter'
      ? {
          killDeltaMode: values.killDeltaMode.trim() || 'conditional_bonus_kill',
          killDeltaValue: optionalNumber(values.killDeltaValue),
          condition: values.killCondition.trim() || null,
          excludedWeapons: splitCsv(values.excludedWeapons),
        }
      : null
  const multiplierEffect =
    mechanicType === 'multiplier'
      ? {
          target: values.multiplierTarget.trim() || 'kills',
          delta: optionalDecimal(values.multiplierDelta),
          activeWindow: values.activeWindow.trim() || 'entire_round',
          stopCondition: values.stopCondition.trim() || null,
        }
      : null
  const mentorEffect =
    mechanicType === 'mentor'
      ? {
          loadoutText: values.mentorLoadoutText.trim() || null,
          durationSeconds: mentorDurationSeconds,
          canBeRevived: values.mentorCanBeRevived === 'true',
          canBeKilled: values.mentorCanBeKilled === 'true',
          killsCreditToTeam: values.mentorKillsCreditToTeam === 'true',
        }
      : null
  const conditions =
    mechanicType === 'restriction_with_reward'
      ? [{ type: 'at_least_one_kill', source: 'manual_input' }]
      : mechanicType === 'kill_counter' && values.killCondition.trim()
        ? [{ type: values.killCondition.trim(), source: 'manual_input' }]
        : []
  const resolutionInputs =
    mechanicType === 'rule_only'
      ? []
      : mechanicType === 'multiplier'
        ? ['killsDuringWindow']
        : mechanicType === 'mentor'
          ? mentorKillsCreditToTeam
            ? ['mentorKills']
            : ['mentorStatus']
          : ['kills']
  const traits =
    mechanicType === 'rule_only'
      ? []
      : mechanicType === 'restriction_with_reward'
        ? values.autoResultFormula === 'stacking_per_kill_bonus'
          ? ['requires_manual_resolution', 'stacking_per_kill_bonus']
          : ['requires_manual_resolution']
        : mentorKillsCreditToTeam
          ? ['requires_manual_resolution', 'kill_counter']
          : ['requires_manual_resolution']

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    category: values.category,
    requiresHostControl: values.requiresHostControl,
    mechanicType,
    activationCost: Number.parseInt(values.activationCost, 10),
    activationLimit: {
      count: limit,
    },
    effect: {
      mechanicType,
      traits,
      durationSeconds: mechanicType === 'mentor' ? mentorDurationSeconds : durationSeconds,
      ruleText: values.ruleText.trim() || null,
      scoreImpact,
      conditions,
      resolutionInputs,
      killEffect,
      multiplierEffect,
      mentorEffect,
    },
    conflictingModifierIds: values.conflictingModifierIds,
    defaultLimitPerGame: limit,
    scoringType: deriveScoringType(mechanicType),
    iconEmoji: icon === '' ? null : icon,
    activationCommand: command === '' ? null : command,
  }
}

interface ModifierFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initial?: GameModifierDefinition | undefined
  modifiers: GameModifierDefinition[]
  isBusy: boolean
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
  currentModifierId: string | undefined
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
          filterOptions={filterConflictModifiers}
          value={options.filter((option) => field.value.includes(option.id))}
          getOptionLabel={(option) => option.name}
          onChange={(_, value) => field.onChange(value.map((option) => option.id))}
          renderOption={(props, option) => {
            const roundSummaryMeta = deriveModifierRoundSummaryMeta(option)

            return (
              <Stack component="li" {...props} spacing={0.35}>
                <Typography variant="body2" fontWeight={700}>
                  {option.name}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {option.description}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {t(`gameCatalog.modifiers.mechanics.${option.mechanicType}`)} ·{' '}
                  {t(`gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`)}
                </Typography>
              </Stack>
            )
          }}
          renderInput={(params) => (
            <TextField
              {...(params as TextFieldProps)}
              size="small"
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

function ModifierEffectFields({
  control,
  isBusy,
  mechanicType,
}: {
  control: Control<ModifierFormValues>
  isBusy: boolean
  mechanicType: ModifierMechanicType
}) {
  const { t } = useTranslation()
  const autoResultFormula = useWatch({
    control,
    name: 'autoResultFormula',
  }) as ModifierAutoResultFormula | undefined

  if (mechanicType === 'restriction_with_reward') {
    return (
      <Stack spacing={1.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <ControlledFormTextField
            control={control}
            name="perKillBonus"
            type="number"
            label={t('gameCatalog.modifiers.fields.perKillBonus')}
            disabled={isBusy}
          />
          <ControlledFormTextField
            control={control}
            name="failurePenaltyPoints"
            type="number"
            label={t('gameCatalog.modifiers.fields.failurePenaltyPoints')}
            disabled={isBusy}
          />
        </Stack>
        <ControlledFormTextField
          control={control}
          name="autoResultFormula"
          select
          label={t('gameCatalog.modifiers.fields.autoResultFormula')}
          helperText={t('gameCatalog.modifiers.fields.autoResultFormulaHint')}
          disabled={isBusy}
        >
          {modifierAutoResultFormulas.map((formula) => (
            <MenuItem key={formula} value={formula}>
              {t(`gameCatalog.modifiers.autoResultFormula.${formula}`)}
            </MenuItem>
          ))}
        </ControlledFormTextField>
        {isCustomModifierScoreFormula(autoResultFormula) ? (
          <Stack spacing={1.25}>
            <ControlledFormTextField
              control={control}
              name="autoResultSuccessExpression"
              label={t('gameCatalog.modifiers.fields.autoResultSuccessExpression')}
              helperText={t('gameCatalog.modifiers.fields.autoResultSuccessExpressionHint')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="autoResultFailureExpression"
              label={t('gameCatalog.modifiers.fields.autoResultFailureExpression')}
              helperText={t('gameCatalog.modifiers.fields.autoResultFailureExpressionHint')}
              disabled={isBusy}
            />
            <Alert severity="info">
              <Typography variant="body2" sx={{ fontWeight: 700 }}>
                {t('gameCatalog.modifiers.customFormula.title')}
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.4 }}>
                {t('gameCatalog.modifiers.customFormula.description')}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
                {t('gameCatalog.modifiers.customFormula.variables')}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
                {t('gameCatalog.modifiers.customFormula.functions')}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
                {t('gameCatalog.modifiers.customFormula.example')}
              </Typography>
            </Alert>
          </Stack>
        ) : null}
      </Stack>
    )
  }

  if (mechanicType === 'kill_counter') {
    return (
      <Stack spacing={1.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <ControlledFormTextField
            control={control}
            name="killDeltaMode"
            label={t('gameCatalog.modifiers.fields.killDeltaMode')}
            disabled={isBusy}
          />
          <ControlledFormTextField
            control={control}
            name="killDeltaValue"
            type="number"
            label={t('gameCatalog.modifiers.fields.killDeltaValue')}
            disabled={isBusy}
          />
        </Stack>
        <ControlledFormTextField
          control={control}
          name="killCondition"
          label={t('gameCatalog.modifiers.fields.killCondition')}
          disabled={isBusy}
        />
        <ControlledFormTextField
          control={control}
          name="excludedWeapons"
          label={t('gameCatalog.modifiers.fields.excludedWeapons')}
          helperText={t('gameCatalog.modifiers.fields.csvHint')}
          disabled={isBusy}
        />
      </Stack>
    )
  }

  if (mechanicType === 'multiplier') {
    return (
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <ControlledFormTextField
          control={control}
          name="multiplierTarget"
          label={t('gameCatalog.modifiers.fields.multiplierTarget')}
          disabled={isBusy}
        />
        <ControlledFormTextField
          control={control}
          name="multiplierDelta"
          label={t('gameCatalog.modifiers.fields.multiplierDelta')}
          disabled={isBusy}
        />
        <ControlledFormTextField
          control={control}
          name="activeWindow"
          label={t('gameCatalog.modifiers.fields.activeWindow')}
          disabled={isBusy}
        />
        <ControlledFormTextField
          control={control}
          name="stopCondition"
          label={t('gameCatalog.modifiers.fields.stopCondition')}
          disabled={isBusy}
        />
      </Stack>
    )
  }

  if (mechanicType === 'mentor') {
    return (
      <Stack spacing={1.5}>
        <ControlledFormTextField
          control={control}
          name="mentorLoadoutText"
          label={t('gameCatalog.modifiers.fields.mentorLoadoutText')}
          disabled={isBusy}
        />
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
          <ControlledFormTextField
            control={control}
            name="mentorDurationSeconds"
            type="number"
            label={t('gameCatalog.modifiers.fields.durationSeconds')}
            disabled={isBusy}
          />
          <ControlledFormTextField
            control={control}
            name="mentorCanBeRevived"
            select
            label={t('gameCatalog.modifiers.fields.mentorCanBeRevived')}
            disabled={isBusy}
          >
            <MenuItem value="false">{t('gameCatalog.common.no')}</MenuItem>
            <MenuItem value="true">{t('gameCatalog.common.yes')}</MenuItem>
          </ControlledFormTextField>
          <ControlledFormTextField
            control={control}
            name="mentorKillsCreditToTeam"
            select
            label={t('gameCatalog.modifiers.fields.mentorKillsCreditToTeam')}
            disabled={isBusy}
          >
            <MenuItem value="false">{t('gameCatalog.common.no')}</MenuItem>
            <MenuItem value="true">{t('gameCatalog.common.yes')}</MenuItem>
          </ControlledFormTextField>
        </Stack>
        <ControlledFormTextField
          control={control}
          name="mentorCanBeKilled"
          select
          label={t('gameCatalog.modifiers.fields.mentorCanBeKilled')}
          disabled={isBusy}
        >
          <MenuItem value="false">{t('gameCatalog.common.no')}</MenuItem>
          <MenuItem value="true">{t('gameCatalog.common.yes')}</MenuItem>
        </ControlledFormTextField>
      </Stack>
    )
  }

  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
      <ControlledFormTextField
        control={control}
        name="durationSeconds"
        type="number"
        label={t('gameCatalog.modifiers.fields.durationSeconds')}
        disabled={isBusy}
      />
      <ControlledFormTextField
        control={control}
        name="ruleText"
        label={t('gameCatalog.modifiers.fields.ruleText')}
        disabled={isBusy}
      />
    </Stack>
  )
}

function ModifierFormulaPreview({ values }: { values: ModifierFormValues }) {
  const { t } = useTranslation()
  const categoryLabels = {
    preparation: t('common.modifiers.categories.preparation'),
    round: t('common.modifiers.categories.round'),
    result: t('common.modifiers.categories.result'),
  } as const
  const categoryLabel = categoryLabels[values.category]
  const mechanicLabel = t(`gameCatalog.modifiers.mechanics.${values.mechanicType}`)
  const roundSummaryMeta = deriveModifierRoundSummaryMeta({
    scoringType: deriveScoringType(values.mechanicType),
    mechanicType: values.mechanicType,
    category: values.category,
    requiresHostControl: values.requiresHostControl,
    name: values.name,
    description: values.description,
    activationCost: Number.parseInt(values.activationCost || '0', 10),
    defaultLimitPerGame:
      values.activationLimitCount.trim() === ''
        ? null
        : Number.parseInt(values.activationLimitCount, 10),
    activationLimit: {
      count:
        values.activationLimitCount.trim() === ''
          ? null
          : Number.parseInt(values.activationLimitCount, 10),
    },
    effect: toRequest(values).effect,
    conflictingModifierIds: [],
    iconEmoji: null,
    activationCommand: null,
  })
  const limit =
    values.activationLimitCount.trim() === ''
      ? t('gameCatalog.modifiers.preview.unlimited')
      : t('gameCatalog.modifiers.preview.limit', {
          count: Number.parseInt(values.activationLimitCount, 10),
        })

  return (
    <Alert severity="info">
      <Typography variant="body2" sx={{ fontWeight: 700 }}>
        {t('gameCatalog.modifiers.preview.title')}
      </Typography>
      <Typography variant="body2">
        {t('gameCatalog.modifiers.preview.body', {
          category: categoryLabel,
          mechanic: mechanicLabel,
          scoringType: deriveScoringType(values.mechanicType),
          limit,
        })}
      </Typography>
      <Typography variant="body2" sx={{ mt: 0.75 }}>
        {t('gameCatalog.modifiers.preview.roundSummary', {
          category: t(`gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`),
        })}
      </Typography>
      {roundSummaryMeta.autoResultFormula ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.preview.scoreFormula', {
            formula: t(
              `gameCatalog.modifiers.autoResultFormula.${roundSummaryMeta.autoResultFormula}`,
            ),
          })}
        </Typography>
      ) : null}
      {roundSummaryMeta.autoResultFormula === 'custom_expression' &&
      roundSummaryMeta.autoResultSuccessExpression ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.preview.successExpression', {
            expression: roundSummaryMeta.autoResultSuccessExpression,
          })}
        </Typography>
      ) : null}
      {roundSummaryMeta.autoResultFormula === 'custom_expression' &&
      roundSummaryMeta.autoResultFailureExpression ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.preview.failureExpression', {
            expression: roundSummaryMeta.autoResultFailureExpression,
          })}
        </Typography>
      ) : null}
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
        {t(`gameBoard.roundSummaryModifierTypeDescription.${roundSummaryMeta.type}`)}
      </Typography>
      {roundSummaryMeta.countInput ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.preview.resultInput', {
            input: t(`gameBoard.roundSummaryModifierCountInput.${roundSummaryMeta.countInput}`),
          })}
        </Typography>
      ) : null}
    </Alert>
  )
}

function ModifierFormDialogBody({
  mode,
  initial,
  modifiers,
  isBusy,
  onClose,
  onSubmit,
}: Omit<ModifierFormDialogProps, 'open'>) {
  const { t } = useTranslation()
  const schema = createModifierFormSchema({
    required: t('gameCatalog.validation.required'),
    number: t('gameCatalog.validation.number'),
    limit: t('gameCatalog.validation.limit'),
    formula: t('gameCatalog.validation.formula'),
  })
  const { control, handleSubmit, setError, formState } = useForm<ModifierFormValues>({
    defaultValues: toDefaultValues(initial),
    resolver: zodResolver(schema),
  })
  const values = useWatch({ control }) as ModifierFormValues
  const categoryLabels = {
    preparation: t('common.modifiers.categories.preparation'),
    round: t('common.modifiers.categories.round'),
    result: t('common.modifiers.categories.result'),
  } as const
  const category = values.category ?? 'round'
  const mechanicType = values.mechanicType ?? 'rule_only'

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(toRequest(values))
    } catch (error) {
      setError('root', { type: 'server', message: resolveCatalogErrorMessage(error, t) })
    }
  })

  return (
    <AppDialog
      open
      onClose={isBusy ? undefined : onClose}
      title={
        mode === 'create'
          ? t('gameCatalog.modifiers.createTitle')
          : t('gameCatalog.modifiers.editTitle')
      }
      actions={
        <>
          <AppButton tone="ghost" onClick={onClose} disabled={isBusy}>
            {t('common.actions.cancel')}
          </AppButton>
          <AppButton type="submit" form={modifierFormId} disabled={isBusy}>
            {t('common.actions.save')}
          </AppButton>
        </>
      }
    >
      {formState.errors.root ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {formState.errors.root.message}
        </Alert>
      ) : null}
      <form id={modifierFormId} onSubmit={(event) => void submit(event)}>
        <Stack spacing={1.5}>
          <Typography variant="subtitle2">{t('gameCatalog.modifiers.sections.basic')}</Typography>
          <ControlledFormTextField
            control={control}
            name="name"
            label={t('gameCatalog.modifiers.fields.name')}
            disabled={isBusy}
          />
          <ControlledFormTextField
            control={control}
            name="description"
            label={t('gameCatalog.modifiers.fields.description')}
            multiline
            minRows={2}
            disabled={isBusy}
          />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="category"
              select
              label={t('gameCatalog.modifiers.fields.category')}
              helperText={t('gameCatalog.modifiers.fields.categoryHint')}
              disabled={isBusy}
            >
              {modifierCategoryCodes.map((value) => (
                <MenuItem key={value} value={value}>
                  {categoryLabels[value]}
                </MenuItem>
              ))}
            </ControlledFormTextField>
            <Controller
              control={control}
              name="requiresHostControl"
              render={({ field }) => (
                <FormControlLabel
                  sx={{ minHeight: 56, mt: { xs: 0, sm: 1 } }}
                  control={
                    <Checkbox
                      checked={field.value}
                      onChange={(event) => field.onChange(event.target.checked)}
                      disabled={isBusy}
                    />
                  }
                  label={t('gameCatalog.modifiers.fields.requiresHostControl')}
                />
              )}
            />
          </Stack>
          <Typography variant="subtitle2">
            {t('gameCatalog.modifiers.sections.mechanics')}
          </Typography>
          <ControlledFormTextField
            control={control}
            name="mechanicType"
            select
            label={t('gameCatalog.modifiers.fields.mechanicType')}
            disabled={isBusy}
          >
            {modifierMechanicTypes.map((type) => (
              <MenuItem key={type} value={type}>
                {t(`gameCatalog.modifiers.mechanics.${type}`)}
              </MenuItem>
            ))}
          </ControlledFormTextField>
          <ModifierEffectFields control={control} isBusy={isBusy} mechanicType={mechanicType} />
          <Typography variant="subtitle2">
            {t('gameCatalog.modifiers.sections.availability')}
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="activationCost"
              type="number"
              label={t('gameCatalog.modifiers.fields.activationCost')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="activationLimitCount"
              type="number"
              label={t('gameCatalog.modifiers.fields.activationLimitCount')}
              helperText={t('gameCatalog.modifiers.fields.limitHint')}
              disabled={isBusy}
            />
          </Stack>
          <ModifierConflictField
            control={control}
            currentModifierId={initial?.id}
            disabled={isBusy}
            modifiers={modifiers}
          />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="iconEmoji"
              label={t('gameCatalog.modifiers.fields.iconEmoji')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="activationCommand"
              label={t('gameCatalog.modifiers.fields.activationCommand')}
              disabled={isBusy}
            />
          </Stack>
          <ModifierFormulaPreview values={{ ...values, category } as ModifierFormValues} />
        </Stack>
      </form>
    </AppDialog>
  )
}

export function ModifierFormDialog({ open, ...props }: ModifierFormDialogProps) {
  return open ? <ModifierFormDialogBody {...props} /> : null
}
