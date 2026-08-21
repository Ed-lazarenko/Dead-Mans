import { MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AppButton, ConfirmDialog } from '../../../shared/ui/index.ts'
import type { TechnicalCancelRoundInput } from '../use-start-game-round.ts'
import type { GameRoundDetails } from '../model/game-management-panel.ts'

type TechnicalReasonCode = TechnicalCancelRoundInput['reasonCode']

const reasonCodes: readonly TechnicalReasonCode[] = [
  'external_game_failure',
  'stream_or_infrastructure_failure',
  'application_error',
  'operator_error',
  'other',
]

export function RoundSafetyControls({
  activeRound,
  isBusy,
  onRebuild,
  onTechnicalCancel,
}: {
  activeRound: GameRoundDetails
  isBusy: boolean
  onRebuild: (input: { roundId: string; expectedRoundVersion: number }) => void
  onTechnicalCancel: (input: TechnicalCancelRoundInput) => void
}) {
  const { t } = useTranslation()
  const [isRebuildConfirmOpen, setIsRebuildConfirmOpen] = useState(false)
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false)
  const [reasonCode, setReasonCode] = useState<TechnicalReasonCode>('external_game_failure')
  const [internalDetail, setInternalDetail] = useState('')
  const [publicSummary, setPublicSummary] = useState('')
  const normalizedDetail = internalDetail.trim()
  const normalizedSummary = publicSummary.trim()
  const requiresPublicSummary = reasonCode === 'other'
  const canSubmitCancellation =
    normalizedDetail.length > 0 && (!requiresPublicSummary || normalizedSummary.length > 0)

  return (
    <Stack spacing={1}>
      {activeRound.status === 'preparing' ? (
        <>
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.roundPanelRebuildHint')}
          </Typography>
          <AppButton
            tone="dangerSecondary"
            size="small"
            disabled={isBusy}
            onClick={() => setIsRebuildConfirmOpen(true)}
          >
            {t('gameBoard.roundPanelRebuild')}
          </AppButton>
        </>
      ) : null}

      <TextField
        select
        size="small"
        label={t('gameBoard.roundPanelTechnicalReason')}
        value={reasonCode}
        disabled={isBusy}
        onChange={(event) => setReasonCode(event.target.value as TechnicalReasonCode)}
      >
        {reasonCodes.map((code) => (
          <MenuItem key={code} value={code}>
            {t(`gameBoard.technicalCancelReasons.${code}`)}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        size="small"
        multiline
        minRows={2}
        required
        label={t('gameBoard.roundPanelTechnicalDetail')}
        value={internalDetail}
        disabled={isBusy}
        inputProps={{ maxLength: 2000 }}
        onChange={(event) => setInternalDetail(event.target.value)}
      />
      {requiresPublicSummary ? (
        <TextField
          size="small"
          required
          label={t('gameBoard.roundPanelTechnicalPublicSummary')}
          value={publicSummary}
          disabled={isBusy}
          inputProps={{ maxLength: 500 }}
          onChange={(event) => setPublicSummary(event.target.value)}
        />
      ) : null}
      <AppButton
        tone="dangerSecondary"
        size="small"
        disabled={isBusy || !canSubmitCancellation}
        onClick={() => setIsCancelConfirmOpen(true)}
      >
        {t('gameBoard.roundPanelTechnicalCancel')}
      </AppButton>

      <ConfirmDialog
        open={isRebuildConfirmOpen}
        title={t('gameBoard.roundPanelRebuildConfirmTitle')}
        description={t('gameBoard.roundPanelRebuildConfirmDescription')}
        confirmLabel={t('gameBoard.roundPanelRebuild')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        isBusy={isBusy}
        onClose={() => setIsRebuildConfirmOpen(false)}
        onConfirm={() => {
          setIsRebuildConfirmOpen(false)
          onRebuild({
            roundId: activeRound.roundId,
            expectedRoundVersion: activeRound.roundVersion,
          })
        }}
      />
      <ConfirmDialog
        open={isCancelConfirmOpen}
        title={t('gameBoard.roundPanelTechnicalCancelConfirmTitle')}
        description={t('gameBoard.roundPanelTechnicalCancelConfirmDescription')}
        confirmLabel={t('gameBoard.roundPanelTechnicalCancel')}
        cancelLabel={t('common.actions.cancel')}
        confirmTone="danger"
        isBusy={isBusy}
        onClose={() => setIsCancelConfirmOpen(false)}
        onConfirm={() => {
          if (!canSubmitCancellation) return
          setIsCancelConfirmOpen(false)
          onTechnicalCancel({
            roundId: activeRound.roundId,
            expectedRoundVersion: activeRound.roundVersion,
            reasonCode,
            publicSummary: normalizedSummary || null,
            internalDetail: normalizedDetail,
          })
        }}
      />
    </Stack>
  )
}
