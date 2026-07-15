import { Chip, Stack, type ChipProps } from '@mui/material'
import type { ParseKeys } from 'i18next'
import { useTranslation } from 'react-i18next'
import { AppButton } from '../../../shared/ui/index.ts'
import type { GameSetupSyncStatus } from '../use-game-setup-save.ts'

interface SyncChipProps {
  color: NonNullable<ChipProps['color']>
  labelKey: ParseKeys
}

function getSyncChipProps(syncStatus: GameSetupSyncStatus, isDirty: boolean): SyncChipProps {
  switch (syncStatus) {
    case 'saving':
      return { color: 'info' as const, labelKey: 'gameSetup.sync.saving' }
    case 'saved':
      return { color: 'success' as const, labelKey: 'gameSetup.sync.saved' }
    case 'error':
      return { color: 'error' as const, labelKey: 'gameSetup.sync.error' }
    case 'conflict':
      return { color: 'warning' as const, labelKey: 'gameSetup.sync.conflict' }
    default:
      return {
        color: isDirty ? ('warning' as const) : ('default' as const),
        labelKey: isDirty ? 'gameSetup.sync.pending' : 'gameSetup.sync.saved',
      }
  }
}

interface GameSetupSyncActionsProps {
  syncStatus: GameSetupSyncStatus
  isDirty: boolean
  isSaving: boolean
  onSave: () => void
}

export function GameSetupSyncActions({
  syncStatus,
  isDirty,
  isSaving,
  onSave,
}: GameSetupSyncActionsProps) {
  const { t } = useTranslation()
  const syncChip = getSyncChipProps(syncStatus, isDirty)

  return (
    <Stack spacing={1} alignItems="flex-end" justifyContent="flex-end">
      <Stack
        direction="row"
        spacing={1}
        alignItems="center"
        justifyContent="flex-end"
        sx={{ flexWrap: 'nowrap' }}
      >
        <Chip size="small" color="warning" label={t('gameSetup.draftBadge')} />
        <Chip size="small" color={syncChip.color} label={t(syncChip.labelKey)} />
      </Stack>
      <AppButton disabled={!isDirty || isSaving} onClick={onSave}>
        {isSaving ? t('gameSetup.saving') : t('gameSetup.save')}
      </AppButton>
    </Stack>
  )
}
