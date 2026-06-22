import { zodResolver } from '@hookform/resolvers/zod'
import { Box, Typography } from '@mui/material'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import { AppButton, ControlledFormTextField } from '../../../shared/ui/index.ts'
import { GAME_SETUP_MAX_TITLE_LENGTH } from '../model/game-setup-limits.ts'
import {
  createGameSetupFormSchema,
  type CreateGameSetupFormValues,
} from '../model/create-game-setup-form-schema.ts'

interface CreateGameSetupPanelProps {
  isSubmitting: boolean
  onCreate: (title: string) => Promise<void>
}

export function CreateGameSetupPanel({ isSubmitting, onCreate }: CreateGameSetupPanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)

  const formSchema = createGameSetupFormSchema({
    required: t('gameSetup.createDialog.validationRequired'),
    invalidTitle: t('gameSetup.invalidTitle'),
  })
  const { control, handleSubmit, reset, setError } = useForm<CreateGameSetupFormValues>({
    defaultValues: { title: '' },
    resolver: zodResolver(formSchema),
  })

  const handleCreate = handleSubmit(async ({ title }) => {
    try {
      await onCreate(title)
      reset()
      setIsOpen(false)
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setError('title', { type: 'server', message: t('gameSetup.createDialog.alreadyExists') })
        return
      }
      setError('title', { type: 'server', message: t('gameSetup.createDialog.error') })
    }
  })

  const handleCancel = () => {
    reset()
    setIsOpen(false)
  }

  if (!isOpen) {
    return (
      <Box
        sx={{
          mt: 3,
          flex: 1,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          border: '2px dashed',
          borderColor: 'divider',
          borderRadius: 2,
          p: 4,
          gap: 1,
          textAlign: 'center',
        }}
      >
        <Typography variant="h6" color="text.secondary">
          {t('gameSetup.createDialog.promptTitle')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t('gameSetup.createDialog.promptDescription')}
        </Typography>
        <AppButton onClick={() => setIsOpen(true)}>
          {t('gameSetup.createDialog.startCreate')}
        </AppButton>
      </Box>
    )
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, maxWidth: 480, mt: 3 }}>
      <Box>
        <Typography variant="h6" gutterBottom>
          {t('gameSetup.createDialog.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('gameSetup.createDialog.detailsDescription')}
        </Typography>
      </Box>
      <form onSubmit={(event) => void handleCreate(event)}>
        <ControlledFormTextField
          autoFocus
          control={control}
          name="title"
          label={t('gameSetup.createDialog.nameLabel')}
          disabled={isSubmitting}
          inputProps={{ maxLength: GAME_SETUP_MAX_TITLE_LENGTH }}
        />
        <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
          <AppButton tone="ghost" onClick={handleCancel} disabled={isSubmitting}>
            {t('gameSetup.createDialog.back')}
          </AppButton>
          <AppButton type="submit" disabled={isSubmitting}>
            {t('gameSetup.createDialog.confirm')}
          </AppButton>
        </Box>
      </form>
    </Box>
  )
}
