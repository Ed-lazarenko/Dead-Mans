import { Stack, TextField, type SxProps, type Theme } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton } from '../../../shared/ui/index.ts'
import { normalizeTeamNameInput, TEAM_NAME_MAX_LENGTH } from '../model/team-name.ts'

interface RegistrationTeamNameEditorProps {
  value: string | null | undefined
  canEdit: boolean
  isSaving: boolean
  onSave: (name?: string) => void
  buttonSx?: SxProps<Theme>
}

export function RegistrationTeamNameEditor({
  value,
  canEdit,
  isSaving,
  onSave,
  buttonSx,
}: RegistrationTeamNameEditorProps) {
  const { t } = useTranslation()
  const sourceName = value ?? ''
  const [draft, setDraft] = useState(() => ({ sourceName, name: sourceName }))
  const name = draft.sourceName === sourceName ? draft.name : sourceName
  const normalizedName = normalizeTeamNameInput(name) ?? ''
  const currentName = normalizeTeamNameInput(sourceName) ?? ''
  const isChanged = normalizedName !== currentName

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={1}
      alignItems={{ xs: 'stretch', md: 'flex-start' }}
    >
      <TextField
        fullWidth
        size="small"
        label={t('gameApplication.teamNameField')}
        placeholder={t('gameApplication.teamNamePlaceholder')}
        value={name}
        disabled={!canEdit || isSaving}
        slotProps={{ htmlInput: { maxLength: TEAM_NAME_MAX_LENGTH } }}
        onChange={(event) => setDraft({ sourceName, name: event.target.value })}
        helperText={
          canEdit
            ? t('gameApplication.teamNameEditableHelper')
            : t('gameApplication.teamNameLockedHelper')
        }
      />
      <AppButton
        size="small"
        disabled={!canEdit || !isChanged || isSaving}
        onClick={() => onSave(normalizeTeamNameInput(name))}
        sx={buttonSx}
      >
        {t('gameApplication.teamNameSave')}
      </AppButton>
    </Stack>
  )
}
