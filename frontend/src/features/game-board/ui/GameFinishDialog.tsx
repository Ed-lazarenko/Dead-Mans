import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Checkbox,
  CircularProgress,
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../../shared/api/contracts/generated'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import { AppButton, AppDialog } from '../../../shared/ui/index.ts'
import { gameFinishPreviewQueryOptions } from '../api/game-finish-queries.ts'

type FinishPreview = components['schemas']['GameFinishPreviewDto']

interface GameFinishDialogProps {
  open: boolean
  gameId: string
  isFinishing: boolean
  finishError: unknown
  onClose: () => void
  onFinish: (input: {
    gameId: string
    expectedBoardVersion: number
    requestId: string
    acknowledgedWarningCodes: string[]
    note: string | null
  }) => Promise<unknown>
}

export function GameFinishDialog({
  open,
  gameId,
  isFinishing,
  finishError,
  onClose,
  onFinish,
}: GameFinishDialogProps) {
  const { t } = useTranslation()
  const previewQuery = useQuery({ ...gameFinishPreviewQueryOptions(gameId), enabled: open })
  const [note, setNote] = useState('')
  const [acknowledgedWarnings, setAcknowledgedWarnings] = useState<Set<string>>(new Set())
  const [irreversibleConfirmed, setIrreversibleConfirmed] = useState(false)
  const [requestId] = useState(() => crypto.randomUUID())

  const preview = previewQuery.data
  const requiredWarningCodes = useMemo(
    () => preview?.warnings.map((warning) => warning.code) ?? [],
    [preview],
  )
  const allWarningsAcknowledged = requiredWarningCodes.every((code) =>
    acknowledgedWarnings.has(code),
  )
  const canSubmit =
    preview?.canFinish === true &&
    allWarningsAcknowledged &&
    irreversibleConfirmed &&
    note.length <= 2000 &&
    !isFinishing

  const close = () => {
    if (!isFinishing) onClose()
  }

  return (
    <AppDialog
      open={open}
      onClose={close}
      maxWidth="md"
      title={t('gameBoard.finishDialogTitle')}
      description={t('gameBoard.finishDialogDescription')}
      actions={
        <>
          <AppButton tone="ghost" onClick={close} disabled={isFinishing}>
            {t('common.actions.cancel')}
          </AppButton>
          <AppButton
            disabled={!canSubmit}
            onClick={async () => {
              if (!preview) return
              try {
                await onFinish({
                  gameId,
                  expectedBoardVersion: preview.summary.boardVersion,
                  requestId,
                  acknowledgedWarningCodes: requiredWarningCodes,
                  note: note.trim() || null,
                })
                onClose()
              } catch {
                // The mutation exposes the localized error state inside this dialog.
              }
            }}
          >
            {isFinishing ? t('gameBoard.finishSubmitting') : t('gameBoard.finishConfirmAction')}
          </AppButton>
        </>
      }
    >
      {previewQuery.isLoading ? (
        <Stack alignItems="center" py={4} spacing={1.5} role="status">
          <CircularProgress size={28} />
          <Typography color="text.secondary">{t('gameBoard.finishPreviewLoading')}</Typography>
        </Stack>
      ) : previewQuery.isError ? (
        <Alert
          severity="error"
          action={
            <AppButton tone="ghost" size="small" onClick={() => void previewQuery.refetch()}>
              {t('common.actions.retry')}
            </AppButton>
          }
        >
          {t('gameBoard.finishPreviewError')}
        </Alert>
      ) : preview ? (
        <FinishPreviewContent
          preview={preview}
          note={note}
          acknowledgedWarnings={acknowledgedWarnings}
          irreversibleConfirmed={irreversibleConfirmed}
          finishError={finishError}
          onNoteChange={setNote}
          onWarningChange={(code, checked) =>
            setAcknowledgedWarnings((current) => {
              const next = new Set(current)
              if (checked) next.add(code)
              else next.delete(code)
              return next
            })
          }
          onIrreversibleChange={setIrreversibleConfirmed}
          onRefresh={() => void previewQuery.refetch()}
        />
      ) : null}
    </AppDialog>
  )
}

function FinishPreviewContent({
  preview,
  note,
  acknowledgedWarnings,
  irreversibleConfirmed,
  finishError,
  onNoteChange,
  onWarningChange,
  onIrreversibleChange,
  onRefresh,
}: {
  preview: FinishPreview
  note: string
  acknowledgedWarnings: ReadonlySet<string>
  irreversibleConfirmed: boolean
  finishError: unknown
  onNoteChange: (value: string) => void
  onWarningChange: (code: string, checked: boolean) => void
  onIrreversibleChange: (checked: boolean) => void
  onRefresh: () => void
}) {
  const { t } = useTranslation()
  const stale = finishError instanceof ApiError && finishError.status === 409

  return (
    <Stack spacing={2}>
      {preview.blockers.map((blocker) => (
        <Alert key={blocker.code} severity="error">
          {t(getFinishIssueTranslationKey(blocker.code), { count: blocker.count })}
        </Alert>
      ))}

      {preview.warnings.map((warning) => (
        <Alert key={warning.code} severity="warning">
          <FormControlLabel
            control={
              <Checkbox
                checked={acknowledgedWarnings.has(warning.code)}
                onChange={(event) => onWarningChange(warning.code, event.target.checked)}
              />
            }
            label={t(getFinishIssueTranslationKey(warning.code), { count: warning.count })}
          />
        </Alert>
      ))}

      {preview.summary.pendingQuizQuestionCount > 0 ? (
        <Alert severity="info">
          {t('gameBoard.finishQuizSkipped', {
            count: preview.summary.pendingQuizQuestionCount,
          })}
        </Alert>
      ) : null}

      <Stack spacing={0.75} aria-label={t('gameBoard.finishRankingTitle')}>
        <Typography variant="subtitle1" fontWeight={800}>
          {t('gameBoard.finishRankingTitle')}
        </Typography>
        {preview.summary.teams.map((team) => (
          <Stack
            key={team.teamId}
            direction="row"
            justifyContent="space-between"
            spacing={1}
            sx={{ py: 0.7, borderBottom: 1, borderColor: 'divider' }}
          >
            <Typography>
              {team.placement ? `${team.placement}. ` : ''}
              {team.teamName || t('common.teamWithSlot', { slot: team.teamSlotIndex })}
            </Typography>
            <Typography fontWeight={800}>
              {team.finalScore == null
                ? t('gameBoard.finishDidNotPlay')
                : t('gameBoard.finishPoints', { count: team.finalScore })}
            </Typography>
          </Stack>
        ))}
      </Stack>

      <Accordion disableGutters>
        <AccordionSummary expandIcon={<span aria-hidden="true">⌄</span>}>
          <Typography fontWeight={700}>{t('gameBoard.finishCalculationDetails')}</Typography>
        </AccordionSummary>
        <AccordionDetails>
          <Stack spacing={1}>
            {preview.summary.teams.map((team) => (
              <Typography key={team.teamId} variant="body2">
                {team.teamName || t('common.teamWithSlot', { slot: team.teamSlotIndex })}:{' '}
                {team.bestScore == null
                  ? t('gameBoard.finishDidNotPlay')
                  : t('gameBoard.finishFormula', {
                      best: team.bestScore,
                      penalties: team.penaltyTotal,
                      final: team.finalScore,
                    })}
              </Typography>
            ))}
          </Stack>
        </AccordionDetails>
      </Accordion>

      <TextField
        label={t('gameBoard.finishNoteLabel')}
        helperText={t('gameBoard.finishNoteHelper', { count: note.length })}
        value={note}
        onChange={(event) => onNoteChange(event.target.value)}
        multiline
        minRows={3}
        fullWidth
        error={note.length > 2000}
        inputProps={{ maxLength: 2001 }}
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={irreversibleConfirmed}
            onChange={(event) => onIrreversibleChange(event.target.checked)}
          />
        }
        label={t('gameBoard.finishIrreversibleConfirmation')}
      />

      {finishError ? (
        <Alert
          severity="error"
          action={
            stale ? (
              <AppButton tone="ghost" size="small" onClick={onRefresh}>
                {t('gameBoard.finishRefreshPreview')}
              </AppButton>
            ) : undefined
          }
        >
          {stale ? t('gameBoard.finishStaleError') : t('gameBoard.finishSubmitError')}
        </Alert>
      ) : null}
    </Stack>
  )
}

function getFinishIssueTranslationKey(code: string) {
  switch (code) {
    case 'game_finish.unplayed_teams':
      return 'gameBoard.finishIssue.unplayedTeams'
    case 'game_finish.no_completed_rounds':
      return 'gameBoard.finishIssue.noCompletedRounds'
    case 'game_finish.round_in_progress':
      return 'gameBoard.finishIssue.roundInProgress'
    case 'game_finish.modifier_state_invalid':
      return 'gameBoard.finishIssue.modifierStateInvalid'
    default:
      return 'gameBoard.finishIssue.unknown'
  }
}
