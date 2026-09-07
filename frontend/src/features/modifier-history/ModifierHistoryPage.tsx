import { Box, Chip, Divider, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useDeferredValue, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { gameHistoryRoute } from '../../routes/app-routes.ts'
import {
  AppButton,
  AsyncSection,
  PageShell,
  SectionCard,
  SectionHeader,
} from '../../shared/ui/index.ts'
import type { ModifierVersionDetail } from '../../shared/api/contracts/index.ts'
import {
  modifierHistoryQueryOptions,
  modifierVersionGamesQueryOptions,
  modifierVersionQueryOptions,
  modifierVersionsQueryOptions,
} from './api/modifier-history-queries.ts'

type ArchiveFilter = 'active' | 'archived' | 'all'

export function ModifierHistoryPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim())
  const [filter, setFilter] = useState<ArchiveFilter>('all')
  const modifierId = params.get('modifierId') ?? ''
  const revision = Number.parseInt(params.get('revision') ?? '0', 10) || 0
  const history = useInfiniteQuery(modifierHistoryQueryOptions(deferredSearch, filter))
  const versions = useInfiniteQuery(modifierVersionsQueryOptions(modifierId))
  const detail = useQuery(modifierVersionQueryOptions(modifierId, revision))
  const previousDetail = useQuery(modifierVersionQueryOptions(modifierId, revision - 1))
  const games = useInfiniteQuery(modifierVersionGamesQueryOptions(modifierId, revision))
  const historyItems = history.data?.pages.flatMap((page) => page.items) ?? []
  const versionItems = useMemo(
    () => versions.data?.pages.flatMap((page) => page.items) ?? [],
    [versions.data],
  )
  const gameItems = games.data?.pages.flatMap((page) => page.items) ?? []
  const revisionButtons = useRef<Array<HTMLButtonElement | null>>([])
  const selectedSummary = historyItems.find((item) => item.modifierId === modifierId)

  useEffect(() => {
    if (modifierId && revision === 0 && versionItems[0]) {
      setParams({ modifierId, revision: String(versionItems[0].revision) }, { replace: true })
    }
  }, [modifierId, revision, setParams, versionItems])

  const selectModifier = (id: string) => setParams({ modifierId: id })
  const selectRevision = (value: number) =>
    setParams({ modifierId, revision: String(value) }, { replace: true })

  return (
    <PageShell sx={{ maxWidth: 'none', width: '100%' }}>
      <SectionHeader
        title={t('modifierHistory.title')}
        description={t('modifierHistory.description')}
      />
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ my: 2 }}>
        <TextField
          label={t('modifierHistory.search')}
          value={search}
          onChange={(event) => setSearch(event.target.value.slice(0, 100))}
          inputProps={{ maxLength: 100 }}
          fullWidth
        />
        <TextField
          select
          label={t('modifierHistory.filter')}
          value={filter}
          onChange={(event) => setFilter(event.target.value as ArchiveFilter)}
          sx={{ minWidth: 190 }}
        >
          {(['all', 'active', 'archived'] as const).map((value) => (
            <MenuItem key={value} value={value}>
              {t(`modifierHistory.${value}`)}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', lg: '320px minmax(0, 1fr)' },
          gap: 2,
        }}
      >
        <SectionCard>
          <AsyncSection
            isLoading={history.isLoading}
            isError={history.isError}
            isEmpty={historyItems.length === 0}
            loadingMessage={t('modifierHistory.loading')}
            errorMessage={t('modifierHistory.error')}
            emptyMessage={t('modifierHistory.empty')}
          >
            <Stack spacing={1}>
              {historyItems.map((item) => (
                <AppButton
                  key={item.modifierId}
                  tone={modifierId === item.modifierId ? 'primary' : 'secondary'}
                  onClick={() => selectModifier(item.modifierId)}
                  sx={{ justifyContent: 'flex-start', textAlign: 'left' }}
                >
                  <Stack alignItems="flex-start">
                    <span>
                      {item.iconEmoji ? `${item.iconEmoji} ` : ''}
                      {item.name}
                    </span>
                    <Typography component="span" variant="caption">
                      {t('modifierHistory.revision', { revision: item.currentRevision })}
                    </Typography>
                  </Stack>
                </AppButton>
              ))}
              {history.hasNextPage ? (
                <AppButton
                  tone="secondary"
                  disabled={history.isFetchingNextPage}
                  onClick={() => history.fetchNextPage()}
                >
                  {t('modifierHistory.loadMore')}
                </AppButton>
              ) : null}
            </Stack>
          </AsyncSection>
        </SectionCard>

        {!modifierId ? (
          <SectionCard>
            <Typography>{t('modifierHistory.choose')}</Typography>
          </SectionCard>
        ) : (
          <Stack spacing={2}>
            {selectedSummary ? (
              <SectionCard inset>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  gap={1}
                  alignItems={{ sm: 'center' }}
                >
                  <Box sx={{ flex: 1 }}>
                    <Typography variant="h6">
                      {selectedSummary.iconEmoji ? `${selectedSummary.iconEmoji} ` : ''}
                      {selectedSummary.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('modifierHistory.currentState', {
                        revision: selectedSummary.currentRevision,
                        versions: selectedSummary.versionCount,
                        games: selectedSummary.gamesCount,
                      })}
                    </Typography>
                  </Box>
                  <Chip
                    color={selectedSummary.isArchived ? 'warning' : 'success'}
                    label={t(
                      selectedSummary.isArchived
                        ? 'modifierHistory.archivedBadge'
                        : 'modifierHistory.activeBadge',
                    )}
                  />
                </Stack>
              </SectionCard>
            ) : null}
            <SectionCard>
              <SectionHeader title={t('modifierHistory.revisions')} />
              <AsyncSection
                isLoading={versions.isLoading}
                isError={versions.isError}
                isEmpty={versionItems.length === 0}
                loadingMessage={t('modifierHistory.loading')}
                errorMessage={t('modifierHistory.error')}
                emptyMessage={t('modifierHistory.empty')}
              >
                <Stack spacing={1} role="list" aria-label={t('modifierHistory.revisions')}>
                  {versionItems.map((item, index) => (
                    <AppButton
                      key={item.versionId}
                      ref={(node) => {
                        revisionButtons.current[index] = node
                      }}
                      size="small"
                      tone={revision === item.revision ? 'primary' : 'secondary'}
                      onClick={() => selectRevision(item.revision)}
                      onKeyDown={(event) => {
                        const last = versionItems.length - 1
                        const nextIndex =
                          event.key === 'ArrowDown' || event.key === 'ArrowRight'
                            ? Math.min(index + 1, last)
                            : event.key === 'ArrowUp' || event.key === 'ArrowLeft'
                              ? Math.max(index - 1, 0)
                              : event.key === 'Home'
                                ? 0
                                : event.key === 'End'
                                  ? last
                                  : index
                        if (nextIndex !== index) {
                          event.preventDefault()
                          revisionButtons.current[nextIndex]?.focus()
                        }
                      }}
                      sx={{ justifyContent: 'flex-start', textAlign: 'left', py: 1 }}
                    >
                      <Stack alignItems="flex-start" spacing={0.25}>
                        <strong>
                          {t('modifierHistory.revision', { revision: item.revision })}
                        </strong>
                        <Typography component="span" variant="caption">
                          {t(`modifierHistory.changeTypes.${item.changeType}`)} ·{' '}
                          {t('modifierHistory.by', {
                            author: item.createdByDisplayName,
                            date: new Intl.DateTimeFormat(i18n.resolvedLanguage).format(
                              new Date(item.createdAtUtc),
                            ),
                          })}
                        </Typography>
                        <Typography component="span" variant="caption">
                          {item.changeNote ?? t('modifierHistory.noNote')}
                        </Typography>
                      </Stack>
                    </AppButton>
                  ))}
                  {versions.hasNextPage ? (
                    <AppButton
                      size="small"
                      tone="secondary"
                      disabled={versions.isFetchingNextPage}
                      onClick={() => versions.fetchNextPage()}
                    >
                      {t('modifierHistory.loadMore')}
                    </AppButton>
                  ) : null}
                </Stack>
              </AsyncSection>
            </SectionCard>

            <AsyncSection
              isLoading={detail.isLoading}
              isError={detail.isError}
              isEmpty={!detail.data}
              loadingMessage={t('modifierHistory.loading')}
              errorMessage={t('modifierHistory.error')}
              emptyMessage={t('modifierHistory.choose')}
            >
              {detail.data ? (
                <VersionDetails
                  item={detail.data}
                  previous={previousDetail.data}
                  locale={i18n.resolvedLanguage}
                />
              ) : null}
            </AsyncSection>

            <SectionCard>
              <SectionHeader title={t('modifierHistory.relatedGames')} />
              <AsyncSection
                isLoading={games.isLoading}
                isError={games.isError}
                isEmpty={gameItems.length === 0}
                loadingMessage={t('modifierHistory.loading')}
                errorMessage={t('modifierHistory.error')}
                emptyMessage={t('modifierHistory.noGames')}
              >
                <Stack spacing={1}>
                  {gameItems.map((game) => (
                    <Box
                      key={game.gameId}
                      sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 1.25 }}
                    >
                      <AppButton
                        component={RouterLink}
                        to={`${gameHistoryRoute.fullPath}?gameId=${game.gameId}`}
                        tone="ghost"
                      >
                        {game.gameTitle}
                      </AppButton>
                      <Stack direction="row" gap={0.75} flexWrap="wrap">
                        <Chip
                          size="small"
                          label={t('modifierHistory.activations', {
                            count: game.successfulActivationsCount,
                          })}
                        />
                        <Chip
                          size="small"
                          label={t('modifierHistory.cancelled', {
                            count: game.cancelledActivationsCount,
                          })}
                        />
                        <Chip
                          size="small"
                          label={t('modifierHistory.results', { count: game.resultsCount })}
                        />
                        {game.isEmergencyDisabled ? (
                          <Chip size="small" color="error" label={t('modifierHistory.emergency')} />
                        ) : null}
                      </Stack>
                    </Box>
                  ))}
                  {games.hasNextPage ? (
                    <AppButton
                      tone="secondary"
                      disabled={games.isFetchingNextPage}
                      onClick={() => games.fetchNextPage()}
                    >
                      {t('modifierHistory.loadMore')}
                    </AppButton>
                  ) : null}
                </Stack>
              </AsyncSection>
            </SectionCard>
          </Stack>
        )}
      </Box>
    </PageShell>
  )
}

function VersionDetails({
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
          {item.conflicts.map((x) => x.name).join(', ') || t('modifierHistory.noConflicts')}
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
    compatibility: item.conflicts.map((x) => x.name).join(', ') || t('modifierHistory.noConflicts'),
    behaviorV2: flattenObject(item.behaviorV2)
      .map(([key, value]) => `${key}: ${value}`)
      .join(' · '),
    created: t('modifierHistory.createdValue'),
  }
  const value = values[field]
  return value === null || value === undefined || value === '' ? '—' : String(value)
}
