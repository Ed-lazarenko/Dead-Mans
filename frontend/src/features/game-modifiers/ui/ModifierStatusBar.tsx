import { Box, Stack, Tooltip, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useTranslation } from 'react-i18next'
import type { GameModifierState } from '../../../shared/api/contracts/index.ts'
import { AppButton, FormTextField, ParticipantNamesList } from '../../../shared/ui/index.ts'

interface ModifierStatusBarProps {
  state: GameModifierState
  search: string
  onSearchChange: (value: string) => void
  currentTeamLabel: string
  currentTeamParticipantNames: readonly string[]
  currentTeamParticipantsEmptyLabel: string
  activeCardLabel: string
  canOpenActiveCard: boolean
  onOpenActiveCard: () => void
}

export function ModifierStatusBar({
  state,
  search,
  onSearchChange,
  currentTeamLabel,
  currentTeamParticipantNames,
  currentTeamParticipantsEmptyLabel,
  activeCardLabel,
  canOpenActiveCard,
  onOpenActiveCard,
}: ModifierStatusBarProps) {
  const { t } = useTranslation()
  const activeRoundSpentPoints = state.activeModifiers.reduce(
    (total, activation) => total + activation.activationCost,
    0,
  )

  return (
    <Box
      component="section"
      aria-label={t('gameModifiers.summaryTitle')}
      sx={(theme) => ({
        mt: 1.25,
        border: `1px solid ${alpha(theme.palette.primary.main, 0.34)}`,
        borderRadius: '12px',
        backgroundColor: alpha(theme.palette.background.paper, 0.5),
        px: { xs: 1.25, sm: 1.5 },
        py: { xs: 1.15, sm: 1.25 },
      })}
    >
      <Box
        data-testid="modifier-summary-row"
        sx={(theme) => ({
          display: 'flex',
          flexDirection: 'row',
          flexWrap: 'nowrap',
          alignItems: 'stretch',
          width: '100%',
          overflowX: 'auto',
          overscrollBehaviorX: 'contain',
          scrollbarWidth: 'thin',
          pb: 0.2,
          '& > *': {
            borderLeft: `1px solid ${alpha(theme.palette.divider, 0.58)}`,
            alignSelf: 'stretch',
          },
          '& > :first-of-type': {
            borderLeft: 0,
          },
        })}
      >
        <StatusMetric
          label={t('gameModifiers.summaryTitle')}
          value={
            state.isOrderingOpen
              ? t('gameModifiers.orderingOpen')
              : t('gameModifiers.orderingClosed')
          }
          tone={state.isOrderingOpen ? 'success' : 'error'}
          description={state.isOrderingOpen ? undefined : t('gameModifiers.orderingClosedSummary')}
          tooltip={t('gameModifiers.summaryOrderingStatusTooltip')}
        />
        <StatusMetric
          label={t('gameModifiers.summaryAvailablePoints')}
          value={t('gameModifiers.myPointsValue', { points: state.availableQuizPoints })}
          tooltip={t('gameModifiers.summaryAvailablePointsTooltip')}
        />
        <StatusMetric
          label={t('gameModifiers.summarySpentPoints')}
          value={t('gameModifiers.myPointsValue', { points: state.spentQuizPoints })}
          tooltip={t('gameModifiers.summarySpentPointsTooltip')}
        />
        <StatusMetric
          label={t('gameModifiers.summaryRoundSpentPoints')}
          value={t('gameModifiers.myPointsValue', { points: activeRoundSpentPoints })}
          tooltip={t('gameModifiers.summaryRoundSpentPointsTooltip')}
        />
        <TeamContextSummary
          label={t('gameModifiers.summaryCurrentTeam')}
          teamName={currentTeamLabel}
          participantNames={currentTeamParticipantNames}
          participantsEmptyLabel={currentTeamParticipantsEmptyLabel}
          tooltip={t('gameModifiers.summaryCurrentTeamTooltip')}
        />
        <CardContextSummary
          label={t('gameModifiers.summaryActiveCard')}
          cardName={activeCardLabel}
          canOpen={canOpenActiveCard}
          onOpen={onOpenActiveCard}
          openLabel={t('common.actions.viewCard')}
          tooltip={t('gameModifiers.summaryActiveCardTooltip')}
        />
      </Box>

      <FormTextField
        value={search}
        label={t('common.modifiers.searchLabel')}
        onChange={(event) => onSearchChange(event.target.value)}
        sx={{ mt: 1 }}
      />
    </Box>
  )
}

function TeamContextSummary({
  label,
  teamName,
  participantNames,
  participantsEmptyLabel,
  tooltip,
}: {
  label: string
  teamName: string
  participantNames: readonly string[]
  participantsEmptyLabel: string
  tooltip: string
}) {
  return (
    <Tooltip title={tooltip} arrow describeChild enterDelay={150} enterTouchDelay={0}>
      <Stack
        tabIndex={0}
        spacing={0.12}
        alignItems="center"
        justifyContent="center"
        sx={{
          minWidth: 220,
          flex: '1.35 0 220px',
          px: 1.25,
          textAlign: 'center',
          cursor: 'help',
          '&:focus-visible': {
            outline: '2px solid',
            outlineColor: 'primary.main',
            outlineOffset: 2,
          },
        }}
      >
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ fontWeight: 750, letterSpacing: '0.02em', lineHeight: 1.2 }}
        >
          {label}
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 850, lineHeight: 1.25 }}>
          {teamName}
        </Typography>
        <Box
          sx={(theme) => ({
            minWidth: 0,
            width: '100%',
            pt: 0.1,
            color: 'primary.light',
            '& ul': { alignItems: 'center' },
            '& li': {
              px: 0.25,
              fontWeight: 700,
              textAlign: 'center',
            },
            '& li + li': {
              pl: 1,
              borderLeft: `1px solid ${alpha(theme.palette.primary.main, 0.42)}`,
            },
          })}
        >
          <ParticipantNamesList
            names={participantNames}
            emptyLabel={participantsEmptyLabel}
            variant="caption"
            dense
            direction="row"
          />
        </Box>
      </Stack>
    </Tooltip>
  )
}

function CardContextSummary({
  label,
  cardName,
  canOpen,
  onOpen,
  openLabel,
  tooltip,
}: {
  label: string
  cardName: string
  canOpen: boolean
  onOpen: () => void
  openLabel: string
  tooltip: string
}) {
  return (
    <Tooltip title={tooltip} arrow describeChild enterDelay={150} enterTouchDelay={0}>
      <Stack
        tabIndex={canOpen ? undefined : 0}
        spacing={0.12}
        alignItems="center"
        justifyContent="center"
        sx={{
          minWidth: 200,
          flex: '1 0 200px',
          px: 1.25,
          textAlign: 'center',
          cursor: 'help',
          '&:focus-visible': {
            outline: '2px solid',
            outlineColor: 'primary.main',
            outlineOffset: 2,
          },
        }}
      >
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ fontWeight: 750, letterSpacing: '0.02em', lineHeight: 1.2 }}
        >
          {label}
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 850, lineHeight: 1.25 }}>
          {cardName}
        </Typography>
        {canOpen ? (
          <Box sx={{ pt: 0.6 }}>
            <AppButton
              tone="secondary"
              size="small"
              onClick={onOpen}
              sx={{ minWidth: 148, flexShrink: 0 }}
            >
              {openLabel}
            </AppButton>
          </Box>
        ) : null}
      </Stack>
    </Tooltip>
  )
}

function StatusMetric({
  label,
  value,
  tooltip,
  tone = 'default',
  description,
}: {
  label: string
  value: string
  tooltip: string
  tone?: 'default' | 'success' | 'error'
  description?: string
}) {
  return (
    <Tooltip title={tooltip} arrow describeChild enterDelay={150} enterTouchDelay={0}>
      <Stack
        tabIndex={0}
        spacing={0.1}
        sx={{
          minWidth: 120,
          flex: '1 0 120px',
          px: 1.25,
          alignItems: 'center',
          justifyContent: 'center',
          textAlign: 'center',
          cursor: 'help',
          borderRadius: 1,
          '&:focus-visible': {
            outline: '2px solid',
            outlineColor: 'primary.main',
            outlineOffset: 2,
          },
        }}
      >
        <Typography variant="caption" color="text.secondary" noWrap>
          {label}
        </Typography>
        <Box
          role={tone === 'error' || tone === 'success' ? 'status' : undefined}
          sx={(theme) => ({
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            ...(tone === 'error' || tone === 'success'
              ? {
                  px: 0.9,
                  py: 0.55,
                  border: `1px solid ${alpha(theme.palette[tone].main, 0.68)}`,
                  borderRadius: 1,
                  backgroundColor: alpha(theme.palette[tone].main, 0.14),
                }
              : {}),
          })}
        >
          <Typography
            variant="body2"
            sx={(theme) => ({
              color: tone === 'default' ? theme.palette.text.primary : theme.palette[tone].light,
              fontWeight: 750,
            })}
            noWrap
          >
            {value}
          </Typography>
          {description ? (
            <Typography
              variant="caption"
              sx={(theme) => ({
                mt: 0.25,
                maxWidth: 360,
                color:
                  tone === 'default'
                    ? theme.palette.text.secondary
                    : tone === 'error'
                      ? alpha(theme.palette.text.primary, 0.84)
                      : theme.palette[tone].light,
                fontWeight: 650,
                lineHeight: 1.25,
              })}
            >
              {description}
            </Typography>
          ) : null}
        </Box>
      </Stack>
    </Tooltip>
  )
}
