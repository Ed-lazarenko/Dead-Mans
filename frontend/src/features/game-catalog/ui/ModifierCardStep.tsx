import { MenuItem, Stack } from '@mui/material'
import type { Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { ControlledFormTextField } from '../../../shared/ui/index.ts'
import { modifierKinds, type ModifierFormValues } from '../model/modifier-form-schema.ts'
import { FieldWithHelp, ModifierTagField } from './modifier-form-fields.tsx'

export function ModifierCardStep({
  control,
  disabled,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
}) {
  const { t } = useTranslation()
  const help = (field: string) => t(`gameCatalog.modifiers.wizard.help.${field}`)
  return (
    <Stack spacing={1.5}>
      <FieldWithHelp label={t('gameCatalog.modifiers.wizard.kind')} help={help('kind')}>
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
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.fields.name')} help={help('name')}>
        <ControlledFormTextField
          control={control}
          name="name"
          label={t('gameCatalog.modifiers.fields.name')}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp
        label={t('gameCatalog.modifiers.fields.description')}
        help={help('description')}
      >
        <ControlledFormTextField
          control={control}
          name="description"
          label={t('gameCatalog.modifiers.fields.description')}
          multiline
          minRows={3}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.fields.iconEmoji')} help={help('iconEmoji')}>
        <ControlledFormTextField
          control={control}
          name="iconEmoji"
          label={t('gameCatalog.modifiers.fields.iconEmoji')}
          disabled={disabled}
        />
      </FieldWithHelp>
      <FieldWithHelp label={t('gameCatalog.modifiers.wizard.tags')} help={help('tags')}>
        <ModifierTagField control={control} disabled={disabled} />
      </FieldWithHelp>
    </Stack>
  )
}
