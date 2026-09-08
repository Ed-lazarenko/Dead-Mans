import { Alert, Box, Chip, LinearProgress, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { GameModifierDraftPreview } from '../../../shared/api/contracts/index.ts'
import { AppButton } from '../../../shared/ui/index.ts'

export function ModifierReviewStep({
  preview,
  isLoading,
  error,
  onRetry,
}: {
  preview: GameModifierDraftPreview | null
  isLoading: boolean
  error: string | null
  onRetry: () => void
}) {
  const { t } = useTranslation()
  if (isLoading) {
    return <LinearProgress aria-label={t('gameCatalog.modifiers.wizard.previewLoading')} />
  }
  if (error || !preview) {
    return (
      <Alert
        severity="error"
        action={
          <AppButton size="small" tone="secondary" onClick={onRetry}>
            {t('common.actions.retry')}
          </AppButton>
        }
      >
        {error ?? t('gameCatalog.modifiers.wizard.previewError')}
      </Alert>
    )
  }
  const localizedExample = {
    ...preview.example,
    resolutionExample: formatResolutionExample(preview.example.resolutionExample, t),
  }

  return (
    <Stack spacing={1.5}>
      <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
        <Typography variant="overline">{t('gameCatalog.modifiers.wizard.playerView')}</Typography>
        <Typography variant="h6">
          {preview.iconEmoji ? `${preview.iconEmoji} ` : ''}
          {preview.name}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line' }}>
          {preview.description}
        </Typography>
        <Stack direction="row" gap={0.75} flexWrap="wrap" sx={{ mt: 1 }}>
          {preview.normalizedTags.map((tag) => (
            <Chip key={tag} label={tag} size="small" />
          ))}
        </Stack>
      </Box>
      <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.5 }}>
        <Typography variant="overline">{t('gameCatalog.modifiers.wizard.hostView')}</Typography>
        <Typography variant="body2">{preview.behaviorV2.rule}</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
          {t('gameCatalog.modifiers.wizard.commandPreview', {
            command: preview.activationCommand,
          })}
        </Typography>
      </Box>
      <Alert severity="success">
        <Typography variant="subtitle2">
          {t('gameCatalog.modifiers.wizard.exampleTitle')}
        </Typography>
        <Typography variant="body2">
          {t('gameCatalog.modifiers.wizard.exampleFacts', localizedExample)}
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.5 }}>
          {t('gameCatalog.modifiers.wizard.exampleResult', preview.example)}
        </Typography>
      </Alert>
    </Stack>
  )
}

function formatResolutionExample(value: string, t: ReturnType<typeof useTranslation>['t']) {
  return value === 'completed' ||
    value === 'automatic' ||
    value === 'succeeded' ||
    value === 'perActivation'
    ? t(`gameCatalog.modifiers.wizard.exampleResolution.${value}`)
    : value
}
