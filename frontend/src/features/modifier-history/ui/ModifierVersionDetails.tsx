import { Box, Chip, Divider, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { ModifierVersionDetail } from '../../../shared/api/contracts/index.ts'
import { SectionCard } from '../../../shared/ui/index.ts'

export function ModifierVersionDetails({
  item,
  previous,
  locale,
}: {
  item: ModifierVersionDetail
  previous?: ModifierVersionDetail
  locale?: string
}) {
  const { t } = useTranslation()

  return (
    <SectionCard>
      <Stack spacing={1.5}>
        <Stack direction="row" gap={1} flexWrap="wrap" alignItems="center">
          <Typography variant="h5">
            {item.iconEmoji ? `${item.iconEmoji} ` : ''}
            {item.name}
          </Typography>
          {item.isCurrent ? <Chip color="success" label={t('modifierHistory.current')} /> : null}
          {item.isArchived ? (
            <Chip color="warning" label={t('modifierHistory.archivedBadge')} />
          ) : null}
          <Chip label={t(`modifierHistory.changeTypes.${item.changeType}`)} />
        </Stack>
        <Typography color="text.secondary">
          {t('modifierHistory.by', {
            author: item.createdByDisplayName,
            date: new Intl.DateTimeFormat(locale).format(new Date(item.createdAtUtc)),
          })}
        </Typography>
        <Typography sx={{ whiteSpace: 'pre-wrap' }}>
          {item.changeNote ?? t('modifierHistory.noNote')}
        </Typography>
        <Box>
          <Typography variant="subtitle2">{t('modifierHistory.changedFields')}</Typography>
          <Stack spacing={0.75} sx={{ mt: 0.75 }}>
            {item.changedFields.map((field) => (
              <Box
                key={field}
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: '180px 1fr 1fr' },
                  gap: 1,
                }}
              >
                <Typography variant="body2" fontWeight={700}>
                  {t(`modifierHistory.fields.${field}`, { defaultValue: field })}
                </Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ overflowWrap: 'anywhere' }}
                >
                  {t('modifierHistory.before')}: {formatDiffValue(previous, field, t)}
                </Typography>
                <Typography variant="body2" sx={{ overflowWrap: 'anywhere' }}>
                  {t('modifierHistory.after')}: {formatDiffValue(item, field, t)}
                </Typography>
              </Box>
            ))}
          </Stack>
        </Box>
        <Divider />
        <Typography variant="h6">{t('modifierHistory.fullConfiguration')}</Typography>
        <ModifierConfigurationReadOnly item={item} />
      </Stack>
    </SectionCard>
  )
}

function ModifierConfigurationReadOnly({ item }: { item: ModifierVersionDetail }) {
  const { t } = useTranslation()
  const behaviorRows = flattenObject(item.behaviorV2)

  return (
    <Stack spacing={1.25}>
      <Typography sx={{ whiteSpace: 'pre-wrap' }}>{item.description}</Typography>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
          gap: 1,
        }}
      >
        {[
          [t('modifierHistory.category'), item.category],
          [t('modifierHistory.cost'), String(item.activationCost)],
          [
            t('modifierHistory.limit'),
            item.activationLimit.count == null
              ? t('modifierHistory.unlimited')
              : String(item.activationLimit.count),
          ],
          [t('modifierHistory.command'), item.activationCommand ?? '—'],
          [t('modifierHistory.icon'), item.iconEmoji ?? '—'],
        ].map(([label, value]) => (
          <Box key={label}>
            <Typography variant="caption" color="text.secondary">
              {label}
            </Typography>
            <Typography sx={{ overflowWrap: 'anywhere' }}>{value}</Typography>
          </Box>
        ))}
      </Box>
      <Box>
        <Typography variant="subtitle2">{t('modifierHistory.tags')}</Typography>
        <Typography>{item.normalizedTags.join(', ') || '—'}</Typography>
      </Box>
      <Box>
        <Typography variant="subtitle2">{t('modifierHistory.conflicts')}</Typography>
        <Typography>
          {item.conflicts.map((conflict) => conflict.name).join(', ') ||
            t('modifierHistory.noConflicts')}
        </Typography>
      </Box>
      <Box>
        <Typography variant="subtitle2">{t('modifierHistory.behavior')}</Typography>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: '220px 1fr' },
            gap: 0.75,
            mt: 0.75,
          }}
        >
          {behaviorRows.map(([key, value]) => (
            <Box key={key} sx={{ display: 'contents' }}>
              <Typography variant="body2" color="text.secondary">
                {t(`modifierHistory.behaviorFields.${key}`, { defaultValue: key })}
              </Typography>
              <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>
                {value}
              </Typography>
            </Box>
          ))}
        </Box>
      </Box>
    </Stack>
  )
}

function flattenObject(value: unknown, prefix = ''): Array<[string, string]> {
  if (value === null || value === undefined) return [[prefix, '—']]
  if (Array.isArray(value)) return [[prefix, value.map(String).join(', ') || '—']]
  if (typeof value !== 'object') return [[prefix, String(value)]]

  return Object.entries(value as Record<string, unknown>).flatMap(([key, nested]) =>
    flattenObject(nested, prefix ? `${prefix}.${key}` : key),
  )
}

function formatDiffValue(
  item: ModifierVersionDetail | undefined,
  field: string,
  t: (key: string, options?: Record<string, unknown>) => string,
) {
  if (!item) return '—'

  const values: Record<string, unknown> = {
    name: item.name,
    description: item.description,
    category: item.category,
    iconEmoji: item.iconEmoji,
    activationCommand: item.activationCommand,
    activationCost: item.activationCost,
    activationLimit: item.activationLimit.count ?? t('modifierHistory.unlimited'),
    normalizedTags: item.normalizedTags.join(', '),
    compatibility:
      item.conflicts.map((conflict) => conflict.name).join(', ') ||
      t('modifierHistory.noConflicts'),
    behaviorV2: flattenObject(item.behaviorV2)
      .map(([key, value]) => `${key}: ${value}`)
      .join(' · '),
    created: t('modifierHistory.createdValue'),
  }
  const value = values[field]

  return value === null || value === undefined || value === '' ? '—' : String(value)
}
