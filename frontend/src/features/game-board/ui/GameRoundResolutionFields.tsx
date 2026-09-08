import { Chip, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { ControlledFormTextField, FormSelect, SectionCard } from '../../../shared/ui/index.ts'
import {
  gameRoundRuleOutcomeStatuses,
  type GameRoundSummaryFormValues,
} from '../model/game-round-summary-form.ts'

type SummaryControl = ReturnType<typeof useForm<GameRoundSummaryFormValues>>['control']

export function GameRoundSummarySection({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  return (
    <Stack spacing={1.5}>
      <Typography variant="subtitle2">{title}</Typography>
      {children}
    </Stack>
  )
}

export function GameRoundRuleGroupCard({
  index,
  control,
}: {
  index: number
  control: SummaryControl
}) {
  const { t } = useTranslation()
  const group = useWatch({ control, name: `ruleGroups.${index}` })
  if (!group) return null

  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
          <Typography variant="subtitle2">{group.modifierName}</Typography>
          <Chip
            size="small"
            color="secondary"
            variant="outlined"
            label={t('gameBoard.roundSummaryModifierStackCount', {
              count: group.memberResultIds.length,
            })}
          />
        </Stack>
        {group.modifierDescription ? (
          <Typography variant="body2" color="text.secondary">
            {group.modifierDescription}
          </Typography>
        ) : null}
        <Typography variant="caption" color="text.secondary">
          {t('gameBoard.roundSummaryRuleMembers', {
            members: group.memberActivationIds
              .map((id, memberIndex) => `#${memberIndex + 1} · ${shortId(id)}`)
              .join(', '),
          })}
        </Typography>
        <Controller
          control={control}
          name={`ruleGroups.${index}.outcomeStatus`}
          render={({ field, fieldState }) => (
            <FormSelect
              label={t('gameBoard.roundSummaryRuleStatus')}
              value={field.value ?? ''}
              onChange={(value) => field.onChange(value || null)}
              error={fieldState.invalid}
              helperText={fieldState.invalid ? t('gameBoard.roundSummaryRequired') : undefined}
              options={gameRoundRuleOutcomeStatuses.map((status) => ({
                value: status,
                label: t(`gameBoard.roundSummaryRuleStatusOption.${status}`),
              }))}
            />
          )}
        />
        {group.outcomeStatus === 'violated' ? (
          <ControlledFormTextField
            control={control}
            name={`ruleGroups.${index}.violationComment`}
            label={t('gameBoard.roundSummaryViolationComment')}
            multiline
            minRows={2}
          />
        ) : null}
      </Stack>
    </SectionCard>
  )
}

export function GameRoundScoringInstanceCard({
  index,
  control,
}: {
  index: number
  control: SummaryControl
}) {
  const { t } = useTranslation()
  const instance = useWatch({ control, name: `scoringInstances.${index}` })
  if (!instance) return null

  return (
    <SectionCard inset>
      <Stack spacing={1.25}>
        {instance.memberResultIds.length > 1 ? (
          <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
            <Typography variant="subtitle2">{instance.modifierName}</Typography>
            <Chip
              size="small"
              variant="outlined"
              label={t('gameBoard.roundSummaryModifierStackCount', {
                count: instance.memberResultIds.length,
              })}
            />
          </Stack>
        ) : (
          <GameRoundModifierHeading
            name={instance.modifierName}
            index={instance.activationIndex}
            count={instance.activationCount}
          />
        )}
        {instance.modifierDescription ? (
          <Typography variant="body2" color="text.secondary">
            {instance.modifierDescription}
          </Typography>
        ) : null}
        {instance.resolutionKind === 'boolean' ? (
          <Controller
            control={control}
            name={`scoringInstances.${index}.isConditionMet`}
            render={({ field, fieldState }) => (
              <FormSelect
                label={t('gameBoard.roundSummaryModifierConditionToggle')}
                value={field.value === null ? '' : String(field.value)}
                onChange={(value) => field.onChange(value === '' ? null : value === 'true')}
                error={fieldState.invalid}
                helperText={fieldState.invalid ? t('gameBoard.roundSummaryRequired') : undefined}
                options={[
                  { value: 'true', label: t('gameBoard.roundSummaryModifierConditionMet') },
                  { value: 'false', label: t('gameBoard.roundSummaryModifierConditionMissed') },
                ]}
              />
            )}
          />
        ) : (
          <ControlledFormTextField
            control={control}
            name={`scoringInstances.${index}.countValue`}
            type="number"
            label={instance.inputLabel ?? t('gameBoard.roundSummaryCountValue')}
            helperText={
              instance.maximumKind === 'activations' && instance.maximumPerActivation !== null
                ? t('gameBoard.roundSummaryActivationCountLimit', {
                    count: instance.memberResultIds.length * instance.maximumPerActivation,
                  })
                : undefined
            }
            inputProps={{
              min: 0,
              ...(instance.maximumKind === 'activations' && instance.maximumPerActivation !== null
                ? { max: instance.memberResultIds.length * instance.maximumPerActivation }
                : {}),
            }}
          />
        )}
      </Stack>
    </SectionCard>
  )
}

export function GameRoundModifierHeading({
  name,
  index,
  count,
}: {
  name: string
  index: number
  count: number
}) {
  const { t } = useTranslation()
  return (
    <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
      <Typography variant="subtitle2">{name}</Typography>
      <Chip
        size="small"
        variant="outlined"
        label={t('gameBoard.roundSummaryActivationLabel', { index, count })}
      />
    </Stack>
  )
}

function shortId(value: string) {
  return value.length <= 8 ? value : value.slice(-8)
}
