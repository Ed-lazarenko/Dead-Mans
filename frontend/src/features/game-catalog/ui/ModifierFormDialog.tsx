import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Autocomplete, MenuItem, Stack, TextField, Typography } from '@mui/material'
import type { TextFieldProps } from '@mui/material'
import { Controller, useForm, useWatch } from 'react-hook-form'
import type { Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, ControlledFormTextField } from '../../../shared/ui/index.ts'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'
import {
  createModifierFormSchema,
  modifierActivationLimitScopes,
  modifierMechanicTypes,
  type ModifierFormValues,
  type ModifierMechanicType,
} from '../model/modifier-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const modifierFormId = 'catalog-modifier-form'

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

function toDefaultValues(initial: GameModifierDefinition | undefined): ModifierFormValues {
  if (!initial) {
    return {
      name: '',
      description: '',
      kind: 'active',
      mechanicType: 'rule_only',
      tier: 'low',
      activationCost: '0',
      activationLimitCount: '',
      activationLimitScope: 'game',
      conflictingModifierIds: [],
      iconEmoji: '',
      activationCommand: '',
      durationSeconds: '',
      ruleText: '',
      perKillBonus: '',
      failurePenaltyPoints: '',
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
    kind: initial.kind === 'passive' ? 'passive' : 'active',
    mechanicType: initial.mechanicType,
    tier: initial.tier === 'mid' ? 'mid' : initial.tier === 'high' ? 'high' : 'low',
    activationCost: String(initial.activationCost),
    activationLimitCount:
      initial.activationLimit.count == null ? '' : String(initial.activationLimit.count),
    activationLimitScope: initial.activationLimit.scope,
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
  const scoreImpact =
    mechanicType === 'restriction_with_reward'
      ? {
          pointsDelta: null,
          perKillBonus: optionalNumber(values.perKillBonus),
          failurePenaltyPoints: optionalNumber(values.failurePenaltyPoints),
          multiplierDelta: null,
          killDelta: null,
        }
      : mechanicType === 'kill_counter'
        ? {
            pointsDelta: null,
            perKillBonus: null,
            failurePenaltyPoints: null,
            multiplierDelta: null,
            killDelta: optionalNumber(values.killDeltaValue),
          }
        : mechanicType === 'multiplier'
          ? {
              pointsDelta: null,
              perKillBonus: null,
              failurePenaltyPoints: null,
              multiplierDelta: optionalDecimal(values.multiplierDelta),
              killDelta: null,
            }
          : mentorKillsCreditToTeam
            ? {
                pointsDelta: null,
                perKillBonus: null,
                failurePenaltyPoints: null,
                multiplierDelta: null,
                killDelta: null,
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
      : mentorKillsCreditToTeam
        ? ['requires_manual_resolution', 'kill_counter']
        : ['requires_manual_resolution']

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    kind: values.kind,
    mechanicType,
    tier: values.tier,
    activationCost: Number.parseInt(values.activationCost, 10),
    activationLimit: {
      count: limit,
      scope: values.activationLimitScope,
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
    defaultLimitPerGame: values.activationLimitScope === 'game' ? limit : null,
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
          value={options.filter((option) => field.value.includes(option.id))}
          getOptionLabel={(option) => option.name}
          onChange={(_, value) => field.onChange(value.map((option) => option.id))}
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

  if (mechanicType === 'restriction_with_reward') {
    return (
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
  const mechanicLabel = t(`gameCatalog.modifiers.mechanics.${values.mechanicType}`)
  const limit =
    values.activationLimitCount.trim() === ''
      ? t('gameCatalog.modifiers.preview.unlimited')
      : t('gameCatalog.modifiers.preview.limit', {
          count: Number.parseInt(values.activationLimitCount, 10),
          scope: t(`gameCatalog.modifiers.limitScopes.${values.activationLimitScope}`),
        })

  return (
    <Alert severity="info">
      <Typography variant="body2" sx={{ fontWeight: 700 }}>
        {t('gameCatalog.modifiers.preview.title')}
      </Typography>
      <Typography variant="body2">
        {t('gameCatalog.modifiers.preview.body', {
          mechanic: mechanicLabel,
          scoringType: deriveScoringType(values.mechanicType),
          limit,
        })}
      </Typography>
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
  })
  const { control, handleSubmit, setError, formState } = useForm<ModifierFormValues>({
    defaultValues: toDefaultValues(initial),
    resolver: zodResolver(schema),
  })
  const values = useWatch({ control }) as ModifierFormValues
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
            {t('gameCatalog.actions.cancel')}
          </AppButton>
          <AppButton type="submit" form={modifierFormId} disabled={isBusy}>
            {t('gameCatalog.actions.save')}
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
              name="kind"
              select
              label={t('gameCatalog.modifiers.fields.kind')}
              disabled={isBusy}
            >
              <MenuItem value="active">{t('gameCatalog.modifiers.kinds.active')}</MenuItem>
              <MenuItem value="passive">{t('gameCatalog.modifiers.kinds.passive')}</MenuItem>
            </ControlledFormTextField>
            <ControlledFormTextField
              control={control}
              name="tier"
              select
              label={t('gameCatalog.modifiers.fields.tier')}
              disabled={isBusy}
            >
              <MenuItem value="low">{t('gameCatalog.modifiers.tiers.low')}</MenuItem>
              <MenuItem value="mid">{t('gameCatalog.modifiers.tiers.mid')}</MenuItem>
              <MenuItem value="high">{t('gameCatalog.modifiers.tiers.high')}</MenuItem>
            </ControlledFormTextField>
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
            <ControlledFormTextField
              control={control}
              name="activationLimitScope"
              select
              label={t('gameCatalog.modifiers.fields.activationLimitScope')}
              disabled={isBusy}
            >
              {modifierActivationLimitScopes.map((scope) => (
                <MenuItem key={scope} value={scope}>
                  {t(`gameCatalog.modifiers.limitScopes.${scope}`)}
                </MenuItem>
              ))}
            </ControlledFormTextField>
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
          <ModifierFormulaPreview values={values} />
        </Stack>
      </form>
    </AppDialog>
  )
}

export function ModifierFormDialog({ open, ...props }: ModifierFormDialogProps) {
  return open ? <ModifierFormDialogBody {...props} /> : null
}
