import { Alert, Stack, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton, AppDialog, FormSelect } from '../../../shared/ui/index.ts'
import {
  applyGameSetupBoardLayoutChange,
  canApplyBoardLayoutChange,
  getBoardLayoutPositionIndexes,
  getBoardLayoutTargetLabel,
  type BoardLayoutAction,
  type BoardLayoutAxis,
} from '../model/game-setup-board-ops.ts'
import type { GameSetupDraftState } from '../model/game-setup-draft.ts'

interface GameSetupBoardLayoutDialogProps {
  open: boolean
  draft: GameSetupDraftState
  onClose: () => void
  onApply: (updater: (current: GameSetupDraftState) => GameSetupDraftState) => void
}

export function GameSetupBoardLayoutDialog({
  open,
  draft,
  onClose,
  onApply,
}: GameSetupBoardLayoutDialogProps) {
  const { t } = useTranslation()
  const [action, setAction] = useState<BoardLayoutAction>('add')
  const [axis, setAxis] = useState<BoardLayoutAxis>('row')
  const [positionIndex, setPositionIndex] = useState(0)
  const [confirmStep, setConfirmStep] = useState(false)

  const positionIndexes = useMemo(
    () => getBoardLayoutPositionIndexes(draft, action, axis),
    [draft, action, axis],
  )

  const canApply = canApplyBoardLayoutChange(draft, action, axis)
  const count = axis === 'row' ? draft.rowLabels.length : draft.colLabels.length
  const selectedPositionIndex = positionIndexes.includes(positionIndex)
    ? positionIndex
    : (positionIndexes[0] ?? 0)

  const handleClose = () => {
    setAction('add')
    setAxis('row')
    setPositionIndex(0)
    setConfirmStep(false)
    onClose()
  }

  const handlePrimaryClick = () => {
    if (!confirmStep) {
      setConfirmStep(true)
      return
    }

    onApply((current) =>
      applyGameSetupBoardLayoutChange(current, action, axis, selectedPositionIndex),
    )
    handleClose()
  }

  const getPositionLabel = (index: number) => {
    if (action === 'add') {
      if (index === 0) {
        return axis === 'row'
          ? t('gameSetup.layoutDialog.addRowAtStart')
          : t('gameSetup.layoutDialog.addColumnAtStart')
      }

      if (index === count) {
        return axis === 'row'
          ? t('gameSetup.layoutDialog.addRowAtEnd')
          : t('gameSetup.layoutDialog.addColumnAtEnd')
      }

      return axis === 'row'
        ? t('gameSetup.layoutDialog.addRowBefore', { position: index + 1 })
        : t('gameSetup.layoutDialog.addColumnBefore', { position: index + 1 })
    }

    const targetLabel = getBoardLayoutTargetLabel(draft, axis, index)
    return axis === 'row'
      ? t('gameSetup.layoutDialog.removeRowTarget', {
          position: index + 1,
          label: targetLabel,
        })
      : t('gameSetup.layoutDialog.removeColumnTarget', {
          position: index + 1,
          label: targetLabel,
        })
  }

  const confirmationKey =
    action === 'add'
      ? axis === 'row'
        ? 'gameSetup.layoutDialog.confirmAddRow'
        : 'gameSetup.layoutDialog.confirmAddColumn'
      : axis === 'row'
        ? 'gameSetup.layoutDialog.confirmRemoveRow'
        : 'gameSetup.layoutDialog.confirmRemoveColumn'

  return (
    <AppDialog
      open={open}
      onClose={handleClose}
      title={t('gameSetup.layoutDialog.title')}
      actions={
        <>
          <AppButton tone="ghost" onClick={confirmStep ? () => setConfirmStep(false) : handleClose}>
            {confirmStep ? t('common.actions.back') : t('common.actions.cancel')}
          </AppButton>
          <AppButton
            tone={action === 'remove' ? 'danger' : 'primary'}
            disabled={!canApply || positionIndexes.length === 0}
            onClick={handlePrimaryClick}
          >
            {confirmStep ? t('gameSetup.layoutDialog.confirm') : t('gameSetup.layoutDialog.review')}
          </AppButton>
        </>
      }
    >
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        {t('gameSetup.layoutDialog.description')}
      </Typography>

      {!confirmStep ? (
        <Stack spacing={2}>
          <FormSelect
            label={t('gameSetup.layoutDialog.actionLabel')}
            value={action}
            onChange={(nextAction) => {
              setAction(nextAction)
              setConfirmStep(false)
            }}
            options={[
              { value: 'add', label: t('gameSetup.layoutDialog.actionAdd') },
              { value: 'remove', label: t('common.actions.remove') },
            ]}
          />

          <FormSelect
            label={t('gameSetup.layoutDialog.axisLabel')}
            value={axis}
            onChange={(nextAxis) => {
              setAxis(nextAxis)
              setConfirmStep(false)
            }}
            options={[
              { value: 'row', label: t('gameSetup.layoutDialog.axisRow') },
              { value: 'column', label: t('gameSetup.layoutDialog.axisColumn') },
            ]}
          />

          <FormSelect
            disabled={positionIndexes.length === 0 || !canApply}
            label={t('gameSetup.layoutDialog.positionLabel')}
            value={selectedPositionIndex}
            onChange={(nextPositionIndex) => {
              setPositionIndex(Number(nextPositionIndex))
              setConfirmStep(false)
            }}
            options={positionIndexes.map((index) => ({
              value: index,
              label: getPositionLabel(index),
            }))}
          />

          {!canApply ? (
            <Alert severity="warning">{t('gameSetup.layoutDialog.limitReached')}</Alert>
          ) : null}
        </Stack>
      ) : (
        <Alert severity="warning">
          {t(confirmationKey, {
            target: getPositionLabel(selectedPositionIndex),
          })}
        </Alert>
      )}
    </AppDialog>
  )
}
