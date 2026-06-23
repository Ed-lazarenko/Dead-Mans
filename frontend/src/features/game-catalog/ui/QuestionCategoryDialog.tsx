import { Alert, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, FormTextField } from '../../../shared/ui/index.ts'
import { resolveCatalogErrorMessage } from '../model/catalog-error.ts'

interface QuestionCategoryDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  initialName?: string
  isBusy: boolean
  onClose: () => void
  onSubmit: (name: string) => Promise<void>
}

export function QuestionCategoryDialog({
  open,
  mode,
  initialName = '',
  isBusy,
  onClose,
  onSubmit,
}: QuestionCategoryDialogProps) {
  const { t } = useTranslation()
  const [name, setName] = useState(initialName)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!open) {
      return
    }

    setName(initialName)
    setErrorMessage(null)
  }, [initialName, open])

  const handleClose = () => {
    if (isBusy) {
      return
    }

    setName(initialName)
    setErrorMessage(null)
    onClose()
  }

  const handleSubmit = async () => {
    setErrorMessage(null)

    try {
      await onSubmit(name.trim())
      setName(initialName)
      onClose()
    } catch (error) {
      setErrorMessage(resolveCatalogErrorMessage(error, t))
    }
  }

  return (
    <AppDialog
      open={open}
      onClose={handleClose}
      title={
        mode === 'create'
          ? t('gameCatalog.questions.categoryDialog.title')
          : t('gameCatalog.questions.categoryDialog.editTitle')
      }
      actions={
        <>
          <AppButton tone="ghost" onClick={handleClose} disabled={isBusy}>
            {t('gameCatalog.actions.cancel')}
          </AppButton>
          <AppButton
            onClick={() => void handleSubmit()}
            disabled={isBusy || name.trim().length === 0}
          >
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
        {t(
          mode === 'create'
            ? 'gameCatalog.questions.categoryDialog.description'
            : 'gameCatalog.questions.categoryDialog.editDescription',
        )}
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
