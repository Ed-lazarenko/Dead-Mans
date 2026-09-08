import {
  Autocomplete,
  Box,
  Chip,
  IconButton,
  LinearProgress,
  Paper,
  Radio,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { Controller } from 'react-hook-form'
import type { Control } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { GameModifierDefinition } from '../../../shared/api/contracts/index.ts'
import {
  normalizeModifierTags,
  suggestedModifierTags,
  type ModifierFormValues,
} from '../model/modifier-form-schema.ts'

function HintTooltip({ label, title }: { label: string; title: string }) {
  return (
    <Tooltip title={title} arrow placement="top" enterTouchDelay={0} leaveTouchDelay={5000}>
      <IconButton
        size="small"
        aria-label={`${label}. ${title}`}
        sx={{ width: 40, height: 40, color: 'text.secondary', flexShrink: 0 }}
      >
        <Box
          component="span"
          aria-hidden
          sx={{
            width: 18,
            height: 18,
            borderRadius: '50%',
            border: 1,
            borderColor: 'divider',
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: '0.7rem',
            fontWeight: 700,
          }}
        >
          ?
        </Box>
      </IconButton>
    </Tooltip>
  )
}

export function FieldWithHelp({
  children,
  help,
  label,
}: {
  children: ReactNode
  help: string
  label: string
}) {
  return (
    <Stack direction="row" spacing={0.5} alignItems="flex-start" sx={{ minWidth: 0, flex: 1 }}>
      <Box sx={{ minWidth: 0, flex: 1 }}>{children}</Box>
      <HintTooltip label={label} title={help} />
    </Stack>
  )
}

export function WizardSection({
  children,
  description,
  title,
}: {
  children: ReactNode
  description: string
  title: string
}) {
  return (
    <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
      <Typography variant="subtitle2">{title}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
        {description}
      </Typography>
      <Stack spacing={1.5} sx={{ mt: 1.5 }}>
        {children}
      </Stack>
    </Box>
  )
}

export function SelectionCard({
  checked,
  description,
  disabled,
  title,
  value,
}: {
  checked: boolean
  description: string
  disabled: boolean
  title: string
  value: string
}) {
  return (
    <Paper
      component="label"
      variant="outlined"
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 0.75,
        p: 1.25,
        cursor: disabled ? 'default' : 'pointer',
        borderColor: checked ? 'primary.main' : 'divider',
        bgcolor: checked ? 'action.selected' : 'background.paper',
        transition: (theme) => theme.transitions.create(['border-color', 'background-color']),
        '&:hover': disabled ? undefined : { borderColor: 'primary.main' },
      }}
    >
      <Radio value={value} checked={checked} disabled={disabled} sx={{ p: 0.25 }} />
      <Box sx={{ minWidth: 0 }}>
        <Typography variant="subtitle2">{title}</Typography>
        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>
      </Box>
    </Paper>
  )
}

export function ModifierConflictField({
  control,
  currentModifierId,
  disabled,
  modifiers,
}: {
  control: Control<ModifierFormValues>
  currentModifierId?: string
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
          isOptionEqualToValue={(option, value) => option.id === value.id}
          onChange={(_, value) => field.onChange(value.map((option) => option.id))}
          renderInput={(params) => (
            <TextField
              {...params}
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

export function ModifierTagField({
  control,
  disabled,
}: {
  control: Control<ModifierFormValues>
  disabled: boolean
}) {
  const { t } = useTranslation()
  return (
    <Controller
      control={control}
      name="tags"
      render={({ field, fieldState }) => (
        <Autocomplete
          multiple
          freeSolo
          disabled={disabled}
          options={suggestedModifierTags.map((tag) =>
            t(`gameCatalog.modifiers.wizard.suggestedTags.${tag}`),
          )}
          value={field.value}
          onChange={(_, value) => field.onChange(normalizeModifierTags(value))}
          renderTags={(value, getTagProps) =>
            value.map((option, index) => (
              <Chip label={option} size="small" {...getTagProps({ index })} key={option} />
            ))
          }
          renderInput={(params) => (
            <TextField
              {...params}
              label={t('gameCatalog.modifiers.wizard.tags')}
              error={fieldState.invalid}
              helperText={fieldState.error?.message ?? t('gameCatalog.modifiers.wizard.tagsHint')}
            />
          )}
        />
      )}
    />
  )
}

export function ModifierWizardProgress({
  kind,
  step,
}: {
  kind: ModifierFormValues['kind']
  step: number
}) {
  const { t } = useTranslation()
  const visibleSteps = kind === 'rule' ? [0, 1, 3] : [0, 1, 2, 3]
  const current = visibleSteps.indexOf(step) + 1
  const total = visibleSteps.length
  return (
    <Box sx={{ mb: 2 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 1 }}>
        <Typography variant="subtitle2">
          {t('gameCatalog.modifiers.wizard.step', { current, total })}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {t(`gameCatalog.modifiers.wizard.steps.${step}`)}
        </Typography>
      </Stack>
      <LinearProgress variant="determinate" value={(current / total) * 100} aria-hidden />
      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
        {t(`gameCatalog.modifiers.wizard.stepDescriptions.${step}`)}
      </Typography>
    </Box>
  )
}
