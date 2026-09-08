import { Box, List, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type {
  GameModifierActivation,
  GameModifierDefinition,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { groupActiveGameModifiers } from '../model/game-modifier-groups.ts'
import { deriveModifierRoundSummaryMeta } from '../model/modifier-round-summary.ts'
import { getCategoryLabel } from './modifier-category.ts'
import {
  InlineMetaPill,
  ModifierIcon,
  ModifierSectionHeading,
} from './modifier-list-primitives.tsx'

type ActiveModifierGroup = ReturnType<typeof groupActiveGameModifiers>[number]

interface ActiveModifiersSectionProps {
  groups: ActiveModifierGroup[]
  activationsCount: number
  definitionsById: ReadonlyMap<string, GameModifierDefinition>
  currentUserId: string | null
  canSelfCancel: boolean
  isCancelling: boolean
  hasSearch: boolean
  onSelfCancel: (activation: GameModifierActivation) => void
}

export function ActiveModifiersSection({
  groups,
  activationsCount,
  definitionsById,
  currentUserId,
  canSelfCancel,
  isCancelling,
  hasSearch,
  onSelfCancel,
}: ActiveModifiersSectionProps) {
  const { t } = useTranslation()
  return (
    <SectionCard sx={{ p: { xs: 1.25, sm: 1.5 } }}>
      <ModifierSectionHeading title={t('gameModifiers.activeTitle')} count={activationsCount} />

      {groups.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.25 }}>
          {hasSearch ? t('common.modifiers.emptySearch') : t('gameModifiers.activeEmpty')}
        </Typography>
      ) : (
        <List disablePadding component="ul" sx={{ mt: 0.55 }}>
          {groups.map((group) => {
            const definition = definitionsById.get(group.modifierId)

            return (
              <ActiveModifierRow
                key={group.modifierId}
                group={group}
                {...(definition ? { definition } : {})}
                currentUserId={currentUserId}
                canSelfCancel={canSelfCancel}
                isCancelling={isCancelling}
                onSelfCancel={onSelfCancel}
              />
            )
          })}
        </List>
      )}
    </SectionCard>
  )
}

function ActiveModifierRow({
  group,
  definition,
  currentUserId,
  canSelfCancel,
  isCancelling,
  onSelfCancel,
}: {
  group: ActiveModifierGroup
  definition?: GameModifierDefinition
  currentUserId: string | null
  canSelfCancel: boolean
  isCancelling: boolean
  onSelfCancel: (activation: GameModifierActivation) => void
}) {
  const { t } = useTranslation()
  const ownActivations = currentUserId
    ? group.activations.filter((item) => item.activatedByUserId === currentUserId)
    : []

  return (
    <Box
      component="li"
      sx={(theme) => ({
        listStyle: 'none',
        py: 1.05,
        '&:not(:last-child)': {
          borderBottom: `1px solid ${alpha(theme.palette.divider, 0.36)}`,
        },
      })}
    >
      <Stack direction="row" spacing={1} alignItems="flex-start">
        <ModifierIcon emoji={definition?.iconEmoji} />
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={{ xs: 0.35, sm: 0.8 }}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
          >
            <Typography variant="subtitle2">{group.modifierName}</Typography>
            <InlineMetaPill label={t('gameModifiers.activeTag')} tone="success" />
          </Stack>

          {definition?.description ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
              {definition.description}
            </Typography>
          ) : null}

          <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.65 }}>
            <InlineMetaPill
              label={t('gameModifiers.activeGroupCount', { count: group.activationsCount })}
            />
            <InlineMetaPill
              label={t('gameModifiers.costShortLabel', { cost: group.activationCost })}
              tone="warning"
            />
            {definition ? (
              <InlineMetaPill label={getCategoryLabel(t, definition.category)} />
            ) : null}
            {definition ? (
              <InlineMetaPill
                label={t(
                  `gameCatalog.modifiers.roundSummaryType.${
                    deriveModifierRoundSummaryMeta(definition).type
                  }`,
                )}
              />
            ) : null}
          </Stack>

          <Box
            sx={(theme) => ({
              mt: 0.7,
              borderLeft: `2px solid ${alpha(theme.palette.primary.main, 0.48)}`,
              backgroundColor: alpha(theme.palette.primary.main, 0.055),
              borderRadius: '0 8px 8px 0',
              px: 0.8,
              py: 0.65,
            })}
          >
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 750 }}>
              {t('gameModifiers.activatorsLabel')}
            </Typography>
            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.4 }}>
              {group.activators.map((activator) => (
                <InlineMetaPill
                  key={activator.userId}
                  label={
                    activator.activationsCount > 1
                      ? t('gameModifiers.activeGroupActivatorWithCount', {
                          player: activator.displayName,
                          count: activator.activationsCount,
                        })
                      : activator.displayName
                  }
                />
              ))}
            </Stack>
          </Box>

          {canSelfCancel && ownActivations.length > 0 ? (
            <Stack spacing={0.5} sx={{ mt: 0.75 }}>
              {ownActivations.map((item) => (
                <AppButton
                  key={item.activationId}
                  tone="dangerSecondary"
                  size="small"
                  disabled={isCancelling}
                  onClick={() => onSelfCancel(item)}
                >
                  {t('gameModifiers.selfCancelActionWithCost', { cost: item.activationCost })}
                </AppButton>
              ))}
            </Stack>
          ) : null}
        </Box>
      </Stack>
    </Box>
  )
}
