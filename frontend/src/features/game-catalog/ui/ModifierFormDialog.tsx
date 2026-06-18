import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, MenuItem, Stack } from '@mui/material'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, ControlledFormTextField } from '../../../shared/ui/index.ts'
import type {
  CreateGameModifierRequest,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'
import { createModifierFormSchema, type ModifierFormValues } from '../model/modifier-form-schema.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

const modifierFormId = 'catalog-modifier-form'

function toDefaultValues(initial: GameModifierDefinition | undefined): ModifierFormValues {
  if (!initial) {
    return {
      code: '',
      name: '',
      description: '',
      kind: 'active',
      category: '',
      scoringType: 'non_scoring',
      tier: 'low',
      activationCost: '0',
      defaultLimitPerGame: '',
      iconEmoji: '',
      activationCommand: '',
    }
  }

  return {
    code: initial.code,
    name: initial.name,
    description: initial.description,
    kind: initial.kind === 'passive' ? 'passive' : 'active',
    category: initial.category,
    scoringType: initial.scoringType,
    tier: initial.tier === 'mid' ? 'mid' : initial.tier === 'high' ? 'high' : 'low',
    activationCost: String(initial.activationCost),
    defaultLimitPerGame:
      initial.defaultLimitPerGame == null ? '' : String(initial.defaultLimitPerGame),
    iconEmoji: initial.iconEmoji ?? '',
    activationCommand: initial.activationCommand ?? '',
  }
}

function toRequest(values: ModifierFormValues): CreateGameModifierRequest {
  const limit = values.defaultLimitPerGame.trim()
  const icon = values.iconEmoji.trim()
  const command = values.activationCommand.trim()
  return {
    code: values.code.trim(),
    name: values.name.trim(),
    description: values.description.trim(),
    kind: values.kind,
    category: values.category.trim(),
    scoringType: values.scoringType.trim(),
    tier: values.tier,
    activationCost: Number.parseInt(values.activationCost, 10),
    defaultLimitPerGame: limit === '' ? null : Number.parseInt(limit, 10),
    iconEmoji: icon === '' ? null : icon,
    activationCommand: command === '' ? null : command,
  }
}

interface ModifierFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initial?: GameModifierDefinition | undefined
  isBusy: boolean
  onClose: () => void
  onSubmit: (request: CreateGameModifierRequest) => Promise<void>
}

function ModifierFormDialogBody({
  mode,
  initial,
  isBusy,
  onClose,
  onSubmit,
}: Omit<ModifierFormDialogProps, 'open'>) {
  const { t } = useTranslation()
  const schema = createModifierFormSchema(
    {
      required: t('gameCatalog.validation.required'),
      code: t('gameCatalog.validation.code'),
      number: t('gameCatalog.validation.number'),
      limit: t('gameCatalog.validation.limit'),
    },
    mode === 'create',
  )
  const { control, handleSubmit, setError, formState } = useForm<ModifierFormValues>({
    defaultValues: toDefaultValues(initial),
    resolver: zodResolver(schema),
  })

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
          <ControlledFormTextField
            control={control}
            name="code"
            label={t('gameCatalog.modifiers.fields.code')}
            disabled={mode === 'edit' || isBusy}
            {...(mode === 'create'
              ? { helperText: t('gameCatalog.modifiers.fields.codeHint') }
              : {})}
          />
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
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <ControlledFormTextField
              control={control}
              name="category"
              label={t('gameCatalog.modifiers.fields.category')}
              disabled={isBusy}
            />
            <ControlledFormTextField
              control={control}
              name="scoringType"
              label={t('gameCatalog.modifiers.fields.scoringType')}
              disabled={isBusy}
            />
          </Stack>
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
              name="defaultLimitPerGame"
              type="number"
              label={t('gameCatalog.modifiers.fields.defaultLimitPerGame')}
              helperText={t('gameCatalog.modifiers.fields.limitHint')}
              disabled={isBusy}
            />
          </Stack>
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
        </Stack>
      </form>
    </AppDialog>
  )
}

export function ModifierFormDialog({ open, ...props }: ModifierFormDialogProps) {
  return open ? <ModifierFormDialogBody {...props} /> : null
}
