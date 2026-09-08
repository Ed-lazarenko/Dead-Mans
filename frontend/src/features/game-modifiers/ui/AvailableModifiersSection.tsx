import { Box, Collapse, List, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameModifierAvailability } from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { groupAvailableGameModifiers } from '../model/game-modifier-groups.ts'
import { deriveModifierRoundSummaryMeta } from '../model/modifier-round-summary.ts'
import { getCategoryLabel } from './modifier-category.ts'
import {
  InlineMetaPill,
  ModifierCategorySection,
  ModifierCountBadge,
  ModifierIcon,
  ModifierSectionHeading,
} from './modifier-list-primitives.tsx'
import { ModifierActivationControl } from './ModifierActivationControl.tsx'

type AvailableModifierGroup = ReturnType<typeof groupAvailableGameModifiers>[number]

interface AvailableModifiersSectionProps {
  groups: AvailableModifierGroup[]
  modifierNamesById: ReadonlyMap<string, string>
  activeModifierIds: ReadonlySet<string>
  hasSearch: boolean
  isBusy: boolean
  pendingModifierId: string | null
  onActivate: (modifierId: string) => void
}

export function AvailableModifiersSection({
  groups,
  modifierNamesById,
  activeModifierIds,
  hasSearch,
  isBusy,
  pendingModifierId,
  onActivate,
}: AvailableModifiersSectionProps) {
  const { t } = useTranslation()

  return (
    <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
      <ModifierSectionHeading title={t('gameModifiers.availableTitle')} />

      {groups.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
          {hasSearch ? t('common.modifiers.emptySearch') : t('gameModifiers.availableEmpty')}
        </Typography>
      ) : (
        <Stack spacing={1.25} sx={{ mt: 0.7 }}>
          {groups.map((group) => (
            <ModifierCategorySection key={group.category} category={group.category}>
              <Stack spacing={0.35}>
                <Stack
                  direction="row"
                  spacing={0.8}
                  justifyContent="space-between"
                  alignItems="center"
                  flexWrap="wrap"
                  useFlexGap
                >
                  <Typography component="h3" variant="subtitle2" sx={{ fontWeight: 850 }}>
                    {getCategoryLabel(t, group.category)}
                  </Typography>
                  <ModifierCountBadge count={group.items.length} />
                </Stack>

                <List disablePadding component="ul">
                  {group.items.map((availability) => (
                    <AvailableModifierRow
                      key={availability.modifier.id}
                      availability={availability}
                      isBusy={isBusy}
                      isPending={pendingModifierId === availability.modifier.id}
                      onActivate={onActivate}
                      conflictingModifierNames={availability.modifier.conflictingModifierIds.map(
                        (modifierId) => modifierNamesById.get(modifierId) ?? modifierId,
                      )}
                      activeConflictingModifierNames={availability.modifier.conflictingModifierIds
                        .filter((modifierId) => activeModifierIds.has(modifierId))
                        .map((modifierId) => modifierNamesById.get(modifierId) ?? modifierId)}
                    />
                  ))}
                </List>
              </Stack>
            </ModifierCategorySection>
          ))}
        </Stack>
      )}
    </SectionCard>
  )
}

interface AvailableModifierRowProps {
  availability: GameModifierAvailability
  isBusy: boolean
  isPending: boolean
  conflictingModifierNames: readonly string[]
  activeConflictingModifierNames: readonly string[]
  onActivate: (modifierId: string) => void
}

function AvailableModifierRow({
  availability,
  isBusy,
  isPending,
  conflictingModifierNames,
  activeConflictingModifierNames,
  onActivate,
}: AvailableModifierRowProps) {
  const { t } = useTranslation()
  const [isDetailsOpen, setIsDetailsOpen] = useState(false)
  const definition = availability.modifier
  const roundSummaryMeta = deriveModifierRoundSummaryMeta(definition)
  const hasLimit = availability.limit != null
  const limitReached = hasLimit && availability.activationsCount >= (availability.limit ?? 0)
  const hasConflicts = definition.conflictingModifierIds.length > 0
  const detailsId = `modifier-details-${definition.id}`
  const blockedReasonLabel =
    availability.blockedReason != null
      ? t(`gameModifiers.blockedReasonLabels.${availability.blockedReason}`)
      : t('gameModifiers.unavailableAction')
  const blockedReasonTooltip =
    availability.blockedReason === 'conflict_active' && activeConflictingModifierNames.length > 0
      ? t('gameModifiers.blockedByConflicts', {
          names: activeConflictingModifierNames.join(', '),
        })
      : availability.blockedReason != null
        ? t(`gameModifiers.blockedReasons.${availability.blockedReason}`)
        : t('gameModifiers.unavailableAction')

  return (
    <Box
      component="li"
      sx={(theme) => ({
        listStyle: 'none',
        py: 1,
        '&:not(:last-child)': {
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.32)}`,
        },
      })}
    >
      <Stack spacing={0.65}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'flex-start' }}
        >
          <Stack direction="row" spacing={0.9} sx={{ minWidth: 0, flex: 1 }}>
            <ModifierIcon emoji={definition.iconEmoji} />
            <Box sx={{ minWidth: 0, flex: 1 }}>
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={{ xs: 0.25, md: 0.8 }}
                alignItems={{ xs: 'flex-start', md: 'center' }}
              >
                <Typography variant="subtitle2">{definition.name}</Typography>
                <Typography variant="body2" color="warning.light" sx={{ fontWeight: 700 }}>
                  {t('gameModifiers.costLabel', { cost: definition.activationCost })}
                </Typography>
              </Stack>
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{
                  mt: 0.35,
                  ...(isDetailsOpen
                    ? {}
                    : {
                        display: '-webkit-box',
                        WebkitBoxOrient: 'vertical',
                        WebkitLineClamp: 2,
                        overflow: 'hidden',
                      }),
                }}
              >
                {definition.description}
              </Typography>
            </Box>
          </Stack>

          <ModifierActivationControl
            availability={availability}
            isBusy={isBusy}
            isPending={isPending}
            blockedReasonLabel={blockedReasonLabel}
            blockedReasonTooltip={blockedReasonTooltip}
            onActivate={onActivate}
          />
        </Stack>

        <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap alignItems="center">
          <InlineMetaPill
            tone={limitReached ? 'error' : 'default'}
            label={
              hasLimit
                ? t('gameModifiers.limitProgressLabel', {
                    count: availability.activationsCount,
                    limit: availability.limit,
                  })
                : t('gameModifiers.noLimit')
            }
          />
          {availability.isActive ? (
            <InlineMetaPill label={t('gameModifiers.activeTag')} tone="success" />
          ) : null}
          <AppButton
            tone="ghost"
            size="small"
            aria-expanded={isDetailsOpen}
            aria-controls={detailsId}
            onClick={() => setIsDetailsOpen((current) => !current)}
            sx={{ minHeight: 44, px: 0.75 }}
          >
            {isDetailsOpen
              ? t('gameModifiers.hideDetailsAction')
              : t('gameModifiers.detailsAction')}
          </AppButton>
        </Stack>

        <Collapse in={isDetailsOpen} timeout="auto" unmountOnExit>
          <Stack
            id={detailsId}
            spacing={0.55}
            sx={(theme) => ({
              borderLeft: `2px solid ${alpha(theme.palette.primary.main, 0.44)}`,
              pl: 1,
              pt: 0.3,
            })}
          >
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
              <InlineMetaPill
                label={t(`gameCatalog.modifiers.roundSummaryType.${roundSummaryMeta.type}`)}
              />
              {definition.behaviorV2.requiresHostMonitoring ? (
                <InlineMetaPill label={t('gameModifiers.hostControlTag')} />
              ) : null}
              {hasConflicts ? (
                <InlineMetaPill
                  tone={availability.blockedReason === 'conflict_active' ? 'error' : 'warning'}
                  label={t('gameModifiers.conflictsTag', {
                    count: definition.conflictingModifierIds.length,
                  })}
                />
              ) : null}
            </Stack>
            {conflictingModifierNames.length > 0 ? (
              <Typography variant="body2" color="text.secondary">
                {t('gameModifiers.conflictsListLabel', {
                  names: conflictingModifierNames.join(', '),
                })}
              </Typography>
            ) : null}
          </Stack>
        </Collapse>
      </Stack>
    </Box>
  )
}
