import { Box, Chip, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useQueries, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { currentGameBoardQueryOptions } from '../game-board/index.ts'
import type {
  GameModifierActivation,
  GameModifierDefinition,
} from '../../shared/api/contracts/index.ts'
import { useAuth } from '../../shared/auth/use-auth.ts'
import { AsyncSection, PageShell, SectionCard, SectionHeader } from '../../shared/ui/index.ts'
import { userGameHistoryQueryOptions } from '../game-history/api/game-history-queries.ts'
import { gameModifierCatalogQueryOptions } from './api/game-modifier-queries.ts'

export function GameModifiersPage() {
  const { t } = useTranslation()
  const { user } = useAuth()

  const [snapshotQuery, catalogQuery] = useQueries({
    queries: [currentGameBoardQueryOptions, gameModifierCatalogQueryOptions],
  })

  const historyQuery = useQuery({
    ...userGameHistoryQueryOptions(user?.id ?? ''),
    enabled: user != null,
  })

  const isLoading = snapshotQuery.isLoading || catalogQuery.isLoading
  const isError = snapshotQuery.isError || catalogQuery.isError
  const snapshot = snapshotQuery.data ?? null
  const catalog = catalogQuery.data ?? []
  const isEmpty = !isLoading && !isError && snapshot == null

  // Compute my quiz points for the current game
  const myPoints = (() => {
    if (!snapshot || !historyQuery.data) return null
    const gameEntry = historyQuery.data.find((g) => g.gameId === snapshot.gameId)
    if (!gameEntry) return 0
    return gameEntry.questionAnswers.reduce((sum, a) => sum + a.awardedPoints, 0)
  })()

  // Build catalog lookup map
  const catalogMap = new Map<string, GameModifierDefinition>(catalog.map((m) => [m.id, m]))

  const activeModifiers: GameModifierActivation[] = snapshot?.activeModifiers ?? []
  const enabledModifierIds: string[] = snapshot?.enabledModifierIds ?? []
  const activeModifierIds = new Set(activeModifiers.map((a) => a.modifierId))

  const enabledModifiers = enabledModifierIds
    .map((modifierId) => catalogMap.get(modifierId))
    .filter((m): m is GameModifierDefinition => m !== undefined)

  return (
    <PageShell>
      <SectionHeader
        title={t('gameModifiers.title')}
        actions={
          myPoints !== null && user ? (
            <Chip
              label={`${t('gameModifiers.myPoints')}: ${t('gameModifiers.myPointsValue', { points: myPoints })}`}
              color="primary"
              variant="outlined"
              size="medium"
            />
          ) : undefined
        }
      />

      <AsyncSection
        isLoading={isLoading}
        isError={isError}
        isEmpty={isEmpty}
        loadingMessage={t('gameModifiers.loading')}
        errorMessage={t('gameModifiers.errorLoading')}
        emptyMessage={t('gameModifiers.noGame')}
      >
        <Stack spacing={3} sx={{ mt: 1 }}>
          {/* Active modifiers section */}
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
              {t('gameModifiers.activeTitle')}
            </Typography>
            {activeModifiers.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.activeEmpty')}
              </Typography>
            ) : (
              <Stack spacing={1.5}>
                {activeModifiers.map((activation) => {
                  const def = catalogMap.get(activation.modifierId)
                  return (
                    <ModifierCard
                      key={`${activation.modifierId}-${activation.activatedAtUtc}`}
                      definition={def}
                      modifierId={activation.modifierId}
                      isActive
                      activatedAt={activation.activatedAtUtc}
                    />
                  )
                })}
              </Stack>
            )}
          </Box>

          {/* Available modifiers section */}
          <Box>
            <Typography variant="overline" color="text.secondary" sx={{ mb: 1, display: 'block' }}>
              {t('gameModifiers.availableTitle')}
            </Typography>
            {enabledModifiers.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.availableEmpty')}
              </Typography>
            ) : (
              <Stack spacing={1.5}>
                {enabledModifiers.map((def) => (
                  <ModifierCard
                    key={def.id}
                    definition={def}
                    modifierId={def.id}
                    isActive={activeModifierIds.has(def.id)}
                  />
                ))}
              </Stack>
            )}
          </Box>
        </Stack>
      </AsyncSection>
    </PageShell>
  )
}

interface ModifierCardProps {
  definition: GameModifierDefinition | undefined
  modifierId: string
  isActive: boolean
  activatedAt?: string
}

function ModifierCard({ definition, modifierId, isActive, activatedAt }: ModifierCardProps) {
  const { t } = useTranslation()
  const categoryLabels = {
    preparation: t('gameModifiers.categories.preparation'),
    round: t('gameModifiers.categories.round'),
    result: t('gameModifiers.categories.result'),
  } as const

  return (
    <SectionCard
      sx={(theme) => ({
        borderColor: isActive
          ? alpha(theme.palette.primary.main, 0.55)
          : alpha(theme.palette.divider, 0.4),
      })}
    >
      <Stack direction="row" spacing={1.5} alignItems="flex-start">
        {definition?.iconEmoji ? (
          <Typography sx={{ fontSize: '1.75rem', lineHeight: 1 }}>
            {definition.iconEmoji}
          </Typography>
        ) : null}

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle2" fontWeight={700}>
              {definition?.name ?? modifierId}
            </Typography>
            {isActive && (
              <Chip
                label={t('gameModifiers.activeTag')}
                color="primary"
                size="small"
                sx={{ height: 20, fontSize: '0.68rem' }}
              />
            )}
          </Stack>

          {definition?.description ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {definition.description}
            </Typography>
          ) : null}

          {definition ? (
            <Stack direction="row" spacing={2} sx={{ mt: 1 }} flexWrap="wrap" useFlexGap>
              <Typography variant="caption" color="text.secondary">
                {categoryLabels[definition.category]}
              </Typography>
              {definition.requiresHostControl ? (
                <Typography variant="caption" color="text.secondary">
                  {t('gameModifiers.hostControlTag')}
                </Typography>
              ) : null}
              <Typography variant="caption" color="text.secondary">
                {t('gameModifiers.costLabel', { cost: definition.activationCost })}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {definition.defaultLimitPerGame != null
                  ? t('gameModifiers.limitLabel', { limit: definition.defaultLimitPerGame })
                  : t('gameModifiers.noLimit')}
              </Typography>
            </Stack>
          ) : null}

          {activatedAt ? (
            <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
              {t('gameModifiers.activatedAt', {
                time: new Date(activatedAt).toLocaleTimeString(),
              })}
            </Typography>
          ) : null}
        </Box>
      </Stack>
    </SectionCard>
  )
}
