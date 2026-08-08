import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell } from '../../../shared/api/contracts/index.ts'
import { resolveBackendMediaUrl } from '../../../shared/api/media-url.ts'
import { AppDialog } from '../../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'
import type { GameBoardCardPlayResultRound } from '../use-card-play-result.ts'

interface GameBoardCardPreviewDialogProps {
  cell: GameBoardCell | null
  playResult: {
    round: GameBoardCardPlayResultRound | null
    isLoading: boolean
    isError: boolean
  }
  onClose: () => void
}

export function GameBoardCardPreviewDialog({
  cell,
  playResult,
  onClose,
}: GameBoardCardPreviewDialogProps) {
  const { t } = useTranslation()

  return (
    <AppDialog
      open={cell !== null}
      onClose={onClose}
      maxWidth="lg"
      PaperProps={{
        sx: (theme) => ({
          borderRadius: 2.5,
          border: `1px solid ${alpha(theme.palette.divider, 0.82)}`,
          backgroundImage: 'none',
          boxShadow: `0 22px 70px ${alpha(theme.palette.common.black, 0.38)}`,
          overflow: 'hidden',
        }),
      }}
      title={
        cell ? (
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            justifyContent="space-between"
          >
            <Typography
              component="span"
              variant="h6"
              sx={{
                minWidth: 0,
                fontWeight: 850,
                lineHeight: 1.25,
              }}
            >
              {cell.title || t('gameBoard.cellMediaDialogTitle')}
            </Typography>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.costLabel', { cost: cell.cost })}
            />
          </Stack>
        ) : (
          t('gameBoard.cellMediaDialogTitle')
        )
      }
    >
      {cell ? (
        <Stack spacing={1.25}>
          {cell.description ? (
            <Box
              sx={(theme) => ({
                borderRadius: 1.5,
                border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
                backgroundColor: alpha(theme.palette.background.paper, 0.38),
                px: 1.15,
                py: 0.95,
              })}
            >
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
                {cell.description}
              </Typography>
            </Box>
          ) : null}

          <Box
            sx={{
              display: 'grid',
              gap: 1.25,
              gridTemplateColumns: {
                xs: '1fr',
                lg: 'minmax(0, 1fr) 320px',
              },
              alignItems: 'start',
            }}
          >
            <Box
              sx={(theme) => ({
                display: 'grid',
                gap: 1,
                gridTemplateColumns: '1fr',
                justifyItems: 'center',
                alignItems: 'center',
                borderRadius: 2,
                border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
                background: `linear-gradient(180deg, ${alpha(
                  theme.palette.background.paper,
                  0.42,
                )}, ${alpha(theme.palette.common.black, 0.1)})`,
                boxShadow: `inset 0 1px 0 ${alpha(theme.palette.common.white, 0.06)}`,
                px: { xs: 0.75, sm: 1.1 },
                py: { xs: 0.75, sm: 1.1 },
                minHeight: { xs: 220, sm: 280 },
              })}
            >
              {cell.media.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameBoard.cellMediaEmpty')}
                </Typography>
              ) : (
                cell.media.map((media, index) => (
                  <Box
                    key={`${media.url}-${index}`}
                    component="img"
                    src={resolveBackendMediaUrl(media.url)}
                    alt={cell.title || t('gameBoard.cellMediaDialogTitle')}
                    loading="lazy"
                    decoding="async"
                    sx={{
                      display: 'block',
                      width: 'auto',
                      maxWidth: '100%',
                      height: 'auto',
                      maxHeight: { xs: '48vh', sm: '54vh', md: '58vh' },
                      borderRadius: 1.5,
                      boxShadow: (theme) =>
                        `0 14px 34px ${alpha(theme.palette.common.black, 0.28)}`,
                      objectFit: 'contain',
                      backgroundColor: 'background.default',
                    }}
                  />
                ))
              )}
            </Box>

            <CardPlayResultPanel
              round={playResult.round}
              isLoading={playResult.isLoading}
              isError={playResult.isError}
            />
          </Box>
        </Stack>
      ) : null}
    </AppDialog>
  )
}

function CardPlayResultPanel({
  round,
  isLoading,
  isError,
}: {
  round: GameBoardCardPlayResultRound | null
  isLoading: boolean
  isError: boolean
}) {
  const { t } = useTranslation()

  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 2,
        border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.42),
        px: 1.1,
        py: 1,
      })}
    >
      <Stack spacing={1}>
        <Typography variant="subtitle2" sx={{ fontWeight: 850 }}>
          {t('gameBoard.cardPlayResultTitle')}
        </Typography>

        {isLoading ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.cardPlayResultLoading')}
          </Typography>
        ) : isError ? (
          <Typography variant="body2" color="error.main">
            {t('gameBoard.cardPlayResultError')}
          </Typography>
        ) : !round ? (
          <Typography variant="body2" color="text.secondary">
            {t('gameBoard.cardPlayResultEmpty')}
          </Typography>
        ) : (
          <>
            <Stack spacing={0.45}>
              <Typography variant="body2" sx={{ fontWeight: 800 }}>
                {formatGameBoardTeamName(t, round.teamName, round.teamSlotIndex)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {round.participants.length > 0
                  ? round.participants.map((participant) => participant.displayName).join(', ')
                  : t('gameBoard.roundSummaryNoParticipants')}
              </Typography>
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gap: 0.65,
                gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
              }}
            >
              <CardResultMetric
                label={t('gameBoard.roundSummaryFinalScore')}
                value={t('gameBoard.roundSummaryScoreValue', {
                  value: round.finalScore ?? round.baseScore,
                })}
              />
              <CardResultMetric
                label={t('gameBoard.roundSummaryScoreUnit')}
                value={t('gameBoard.roundSummaryScoreValue', { value: round.cellCost })}
              />
              <CardResultMetric
                label={t('gameBoard.roundSummaryKills')}
                value={String(round.killsCount)}
              />
              <CardResultMetric
                label={t('gameBoard.roundSummaryBounties')}
                value={String(round.bountyCount)}
              />
            </Box>

            <Stack spacing={0.55}>
              <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 800 }}>
                {t('gameBoard.cardPlayResultModifiers')}
              </Typography>
              {round.modifiers.length === 0 ? (
                <Typography variant="caption" color="text.secondary">
                  {t('gameBoard.cardPlayResultNoModifiers')}
                </Typography>
              ) : (
                <Stack direction="row" spacing={0.45} flexWrap="wrap" useFlexGap>
                  {round.modifiers.map((modifier) => (
                    <Chip
                      key={modifier.modifierResultId}
                      size="small"
                      variant="outlined"
                      label={modifier.modifierName}
                    />
                  ))}
                </Stack>
              )}
            </Stack>
          </>
        )}
      </Stack>
    </Box>
  )
}

function formatGameBoardTeamName(
  t: ReturnType<typeof useTranslation>['t'],
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(
    teamName,
    t('gameBoard.teamQueueTeamTitle', { slot: teamSlotIndex }),
  )
}

function CardResultMetric({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={(theme) => ({
        minWidth: 0,
        borderRadius: 1.4,
        border: `1px solid ${alpha(theme.palette.divider, 0.72)}`,
        backgroundColor: alpha(theme.palette.background.paper, 0.34),
        px: 0.8,
        py: 0.65,
      })}
    >
      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 850 }} noWrap>
        {value}
      </Typography>
    </Box>
  )
}
