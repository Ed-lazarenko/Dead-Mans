import { Box, CircularProgress, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { components } from '../../api/contracts/generated'
import { resolveBackendMediaUrl } from '../../api/media-url.ts'
import { AppDialog } from '../feedback/AppDialog.tsx'
import { PlayedCardResultPanel } from './PlayedCardResultPanel.tsx'

type PlayedCardPreviewRound = components['schemas']['GameHistoryRoundItemDto']

interface PlayedCardPreviewCard {
  title?: string | null
  description?: string | null
  cost: number
  media: readonly { url: string }[]
}

interface PlayedCardPreviewDialogProps {
  card: PlayedCardPreviewCard | null
  round: PlayedCardPreviewRound | null
  isLoading?: boolean
  isError?: boolean
  onClose: () => void
}

export function PlayedCardPreviewDialog({
  card,
  round,
  isLoading = false,
  isError = false,
  onClose,
}: PlayedCardPreviewDialogProps) {
  const { t } = useTranslation()
  const previewCard = round ? getCardFromRound(round) : card
  const media = previewCard?.media ?? []

  return (
    <AppDialog
      open={previewCard !== null}
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
        previewCard ? (
          <Typography
            component="span"
            variant="h6"
            sx={{ minWidth: 0, fontWeight: 850, lineHeight: 1.25 }}
          >
            {previewCard.title || t('gameHistory.cardDialogFallbackTitle')}
          </Typography>
        ) : (
          t('gameHistory.cardDialogFallbackTitle')
        )
      }
    >
      {previewCard ? (
        <Stack spacing={1.25}>
          {previewCard.description ? (
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
                {previewCard.description}
              </Typography>
            </Box>
          ) : null}

          <Box
            sx={{
              display: 'grid',
              gap: 1.25,
              gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) 320px' },
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
              {media.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameHistory.cardMediaEmpty')}
                </Typography>
              ) : (
                media.map((item, index) => (
                  <PlayedCardMediaImage
                    key={`${item.url}-${index}`}
                    url={item.url}
                    title={previewCard.title}
                  />
                ))
              )}
            </Box>

            <PlayedCardResultPanel
              cardCost={previewCard.cost}
              round={round}
              isLoading={isLoading}
              isError={isError}
            />
          </Box>
        </Stack>
      ) : null}
    </AppDialog>
  )
}

function PlayedCardMediaImage({ url, title }: { url: string; title?: string | null }) {
  const { t } = useTranslation()
  const [status, setStatus] = useState<'loading' | 'loaded' | 'error'>('loading')

  return (
    <Box
      sx={{
        display: 'grid',
        width: '100%',
        minHeight: { xs: 200, sm: 260 },
        placeItems: 'center',
      }}
    >
      {status === 'loading' ? (
        <Stack
          role="status"
          spacing={1}
          alignItems="center"
          sx={{ gridArea: '1 / 1', color: 'text.secondary' }}
        >
          <CircularProgress size={32} thickness={4} />
          <Typography variant="body2">{t('gameHistory.cardMediaLoading')}</Typography>
        </Stack>
      ) : null}

      {status === 'error' ? (
        <Typography
          role="alert"
          variant="body2"
          color="error.main"
          sx={{ gridArea: '1 / 1', textAlign: 'center' }}
        >
          {t('gameHistory.cardMediaError')}
        </Typography>
      ) : null}

      <Box
        component="img"
        src={resolveBackendMediaUrl(url)}
        alt={title || t('gameHistory.cardDialogFallbackTitle')}
        decoding="async"
        onLoad={() => setStatus('loaded')}
        onError={() => setStatus('error')}
        sx={{
          gridArea: '1 / 1',
          display: 'block',
          visibility: status === 'loaded' ? 'visible' : 'hidden',
          width: 'auto',
          maxWidth: '100%',
          height: 'auto',
          maxHeight: { xs: '48vh', sm: '54vh', md: '58vh' },
          borderRadius: 1.5,
          boxShadow: (theme) => `0 14px 34px ${alpha(theme.palette.common.black, 0.28)}`,
          objectFit: 'contain',
          backgroundColor: 'background.default',
        }}
      />
    </Box>
  )
}

function getCardFromRound(round: PlayedCardPreviewRound): PlayedCardPreviewCard {
  return {
    title: round.cellTitle,
    description: round.cellDescription,
    cost: round.cellCost,
    media: round.cellMedia,
  }
}
