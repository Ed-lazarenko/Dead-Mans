import { useTranslation } from 'react-i18next'
import { ConfirmDialog } from '../../../shared/ui/index.ts'

interface ResetGameSetupDialogProps {
  open: boolean
  isSubmitting: boolean
  onClose: () => void
  onConfirm: () => void | Promise<void>
}

export function ResetGameSetupDialog({
  open,
  isSubmitting,
  onClose,
  onConfirm,
}: ResetGameSetupDialogProps) {
  const { t } = useTranslation()

  return (
    <ConfirmDialog
      open={open}
      isBusy={isSubmitting}
      onClose={onClose}
      onConfirm={onConfirm}
      title={t('gameSetup.resetDialog.title')}
      description={t('gameSetup.resetDialog.description')}
      cancelLabel={t('common.actions.cancel')}
      confirmLabel={
        isSubmitting ? t('gameSetup.resetDialog.submitting') : t('gameSetup.resetDialog.confirm')
      }
      confirmTone="danger"
    />
  )
}
