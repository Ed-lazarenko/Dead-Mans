import { Alert, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, FormTextField } from '../../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

interface QuestionCategoryDialogProps {
  open: boolean
  isBusy: boolean
  onClose: () => void
  onSubmit: (name: string) => Promise<void>
}

export function QuestionCategoryDialog({
  open,
  isBusy,
  onClose,
  onSubmit,
}: QuestionCategoryDialogProps) {
  const { t } = useTranslation()
  const [name, setName] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleClose = () => {
    if (isBusy) {
      return
    }

    setName('')
    setErrorMessage(null)
    onClose()
  }

  const handleSubmit = async () => {
    setErrorMessage(null)

    try {
      await onSubmit(name.trim())
      setName('')
      onClose()
    } catch (error) {
      setErrorMessage(resolveCatalogErrorMessage(error, t))
    }
  }

  return (
    <AppDialog
      open={open}
      onClose={handleClose}
      title={t('gameCatalog.questions.categoryDialog.title')}
      actions={
        <>
          <AppButton tone="ghost" onClick={handleClose} disabled={isBusy}>
            {t('gameCatalog.actions.cancel')}
          </AppButton>
          <AppButton onClick={() => void handleSubmit()} disabled={isBusy || name.trim().length === 0}>
            {t('gameCatalog.actions.save')}
          </AppButton>
        </>
      }
    >
      {errorMessage ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {errorMessage}
        </Alert>
      ) : null}
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('gameCatalog.questions.categoryDialog.description')}
      </Typography>
      <FormTextField
        autoFocus
        value={name}
        label={t('gameCatalog.questions.categoryDialog.nameLabel')}
        disabled={isBusy}
        inputProps={{ maxLength: 64 }}
        onChange={(event) => setName(event.target.value)}
      />
    </AppDialog>
  )
}
