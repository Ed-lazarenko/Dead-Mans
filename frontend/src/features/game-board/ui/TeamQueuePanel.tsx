import {
  Box,
  ButtonBase,
  Chip,
  Divider,
  Drawer,
  IconButton,
  Stack,
  Typography,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameTeamQueueItem } from '../../../shared/api/contracts/index.ts'
import { SectionCard } from '../../../shared/ui/index.ts'
import { formatTeamNameWithFallback } from '../../game-registration/model/team-name.ts'

interface TeamQueuePanelProps {
  teams: readonly GameTeamQueueItem[]
  isLoading: boolean
  isError: boolean
  activeTeamId?: string | null
}

export function TeamQueuePanel({ teams, isLoading, isError, activeTeamId }: TeamQueuePanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const { remainingTeams, playedTeams } = groupTeamQueueTeams(teams)

  return (
    <>
      <ButtonBase
        aria-label={t('gameBoard.teamQueueOpen')}
        onClick={() => setIsOpen(true)}
        sx={(theme) => ({
          position: 'fixed',
          left: 0,
          top: '50%',
          transform: 'translateY(-50%)',
          zIndex: theme.zIndex.drawer - 1,
          minHeight: 148,
          width: 56,
          overflow: 'hidden',
          borderRadius: `0 20px 20px 0`,
          border: `1px solid ${alpha(theme.palette.warning.main, 0.34)}`,
          borderLeft: 0,
          background: `linear-gradient(180deg, ${alpha(theme.palette.warning.main, 0.2)} 0%, ${alpha(
            theme.palette.background.paper,
            0.96,
          )} 34%, ${alpha(theme.palette.info.main, 0.18)} 100%)`,
          boxShadow: `0 18px 34px ${alpha(theme.palette.common.black, 0.3)}, inset 0 1px 0 ${alpha(
            theme.palette.common.white,
            0.14,
          )}`,
          color: theme.palette.text.primary,
          '&:hover': {
            backgroundColor: alpha(theme.palette.primary.main, 0.14),
            width: 60,
          },
          '&:focus-visible': {
            outline: `2px solid ${theme.palette.primary.main}`,
            outlineOffset: 2,
          },
        })}
      >
        <Stack spacing={1} alignItems="center" sx={{ py: 1.25 }}>
          <Typography component="span" aria-hidden variant="body2" fontWeight={900}>
            {isOpen ? '<' : '>'}
          </Typography>
          <Typography
            component="span"
            variant="caption"
            fontWeight={800}
            sx={{
              writingMode: 'vertical-rl',
              transform: 'rotate(180deg)',
              letterSpacing: 0,
            }}
          >
            {t('gameBoard.teamQueueTabLabel')}
          </Typography>
        </Stack>
      </ButtonBase>

      <Drawer
        anchor="left"
        open={isOpen}
        onClose={() => setIsOpen(false)}
        ModalProps={{ keepMounted: true }}
        PaperProps={{
          sx: {
            width: { xs: 'calc(100vw - 40px)', sm: 380 },
            maxWidth: '100vw',
          },
        }}
      >
        <SectionCard
          component="aside"
          aria-label={t('gameBoard.teamQueueTitle')}
          sx={{
            width: '100%',
            minHeight: '100%',
            borderRadius: 0,
            border: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 2,
            overflowY: 'auto',
          }}
        >
          <Stack
            direction="row"
            spacing={1.5}
            alignItems="flex-start"
            justifyContent="space-between"
            sx={(theme) => ({
              mx: -3,
              mt: -3,
              px: 3,
              pt: 3,
              pb: 2,
              background: `linear-gradient(180deg, ${alpha(theme.palette.warning.main, 0.18)} 0%, ${alpha(
                theme.palette.info.main,
                0.12,
              )} 58%, transparent 100%)`,
              borderBottom: `1px solid ${alpha(theme.palette.warning.main, 0.18)}`,
            })}
          >
            <Typography variant="h6" sx={{ minWidth: 0 }}>
              {t('gameBoard.teamQueueTitle')}
            </Typography>
            <IconButton
              size="small"
              aria-label={t('gameBoard.teamQueueClose')}
              onClick={() => setIsOpen(false)}
            >
              <Box component="span" aria-hidden sx={{ fontSize: 18, lineHeight: 1 }}>
                x
              </Box>
            </IconButton>
          </Stack>

          <Divider />

          {isLoading ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameBoard.teamQueueLoading')}
            </Typography>
          ) : isError ? (
            <Typography variant="body2" color="error">
              {t('gameBoard.teamQueueError')}
            </Typography>
          ) : teams.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {t('gameBoard.teamQueueEmpty')}
            </Typography>
          ) : (
            <Stack spacing={1.5}>
              <TeamQueueSection
                title={t('gameBoard.teamQueueRemainingTitle')}
                count={remainingTeams.length}
                emptyMessage={t('gameBoard.teamQueueRemainingEmpty')}
              >
                {remainingTeams.map(({ team }) => (
                  <TeamQueueCard
                    key={team.teamId}
                    team={team}
                    isActive={team.teamId === activeTeamId}
                  />
                ))}
              </TeamQueueSection>

              <TeamQueueSection
                title={t('gameBoard.teamQueuePlayedTitle')}
                count={playedTeams.length}
                emptyMessage={t('gameBoard.teamQueuePlayedEmpty')}
              >
                {playedTeams.map(({ team, playedOrder }) => (
                  <TeamQueueCard
                    key={team.teamId}
                    team={team}
                    isActive={team.teamId === activeTeamId}
                    playedOrder={playedOrder}
                  />
                ))}
              </TeamQueueSection>
            </Stack>
          )}
        </SectionCard>
      </Drawer>
    </>
  )
}

interface OrderedTeamQueueItem {
  team: GameTeamQueueItem
  originalIndex: number
  playedOrder?: number
}

function groupTeamQueueTeams(teams: readonly GameTeamQueueItem[]) {
  const indexedTeams = teams.map((team, originalIndex) => ({ team, originalIndex }))
  const remainingTeams = indexedTeams.filter(({ team }) => !team.isPlayed)
  const playedTeams = indexedTeams
    .filter(({ team }) => team.isPlayed)
    .sort(comparePlayedTeams)
    .map((item, index) => ({
      ...item,
      playedOrder: index + 1,
    }))

  return { remainingTeams, playedTeams }
}

function comparePlayedTeams(left: OrderedTeamQueueItem, right: OrderedTeamQueueItem) {
  const leftPlayedAt = parseOptionalTime(left.team.playedAtUtc)
  const rightPlayedAt = parseOptionalTime(right.team.playedAtUtc)

  if (leftPlayedAt !== null && rightPlayedAt !== null && leftPlayedAt !== rightPlayedAt) {
    return leftPlayedAt - rightPlayedAt
  }

  if (leftPlayedAt !== null && rightPlayedAt === null) {
    return -1
  }

  if (leftPlayedAt === null && rightPlayedAt !== null) {
    return 1
  }

  return left.originalIndex - right.originalIndex
}

function parseOptionalTime(value: string | null | undefined) {
  if (!value) {
    return null
  }

  const timestamp = Date.parse(value)
  return Number.isNaN(timestamp) ? null : timestamp
}

function TeamQueueSection({
  title,
  count,
  emptyMessage,
  children,
}: {
  title: string
  count: number
  emptyMessage: string
  children: ReactNode
}) {
  return (
    <Stack spacing={0.85}>
      <Stack direction="row" spacing={0.75} alignItems="center" justifyContent="space-between">
        <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 900 }}>
          {title}
        </Typography>
        <Chip size="small" variant="outlined" label={count} />
      </Stack>

      {count === 0 ? (
        <Box
          sx={(theme) => ({
            borderRadius: 2,
            border: `1px dashed ${alpha(theme.palette.divider, 0.72)}`,
            backgroundColor: alpha(theme.palette.background.default, 0.32),
            px: 1.2,
            py: 1,
          })}
        >
          <Typography variant="body2" color="text.secondary">
            {emptyMessage}
          </Typography>
        </Box>
      ) : (
        <Stack spacing={1}>{children}</Stack>
      )}
    </Stack>
  )
}

function TeamQueueCard({
  team,
  isActive,
  playedOrder,
}: {
  team: GameTeamQueueItem
  isActive: boolean
  playedOrder?: number
}) {
  const { t } = useTranslation()
  const isPlayed = team.isPlayed ?? false

  return (
    <Box
      sx={(theme) => ({
        position: 'relative',
        overflow: 'hidden',
        borderRadius: 2,
        border: `1px solid ${alpha(
          isActive
            ? theme.palette.warning.main
            : isPlayed
              ? theme.palette.success.main
              : theme.palette.divider,
          isActive || isPlayed ? 0.7 : 0.9,
        )}`,
        background: isActive
          ? `linear-gradient(140deg, ${alpha(theme.palette.warning.main, 0.22)}, ${alpha(
              theme.palette.info.main,
              0.16,
            )})`
          : isPlayed
            ? `linear-gradient(140deg, ${alpha(theme.palette.success.main, 0.16)}, ${alpha(
                theme.palette.background.default,
                0.42,
              )})`
            : `linear-gradient(140deg, ${alpha(
                theme.palette.background.default,
                0.46,
              )}, ${alpha(theme.palette.common.black, 0.12)})`,
        boxShadow: isActive
          ? `0 18px 32px ${alpha(theme.palette.common.black, 0.24)}`
          : `0 10px 22px ${alpha(theme.palette.common.black, 0.16)}`,
        px: 1.35,
        py: 1.25,
        opacity: isPlayed && !isActive ? 0.86 : 1,
        '&::before': {
          content: '""',
          position: 'absolute',
          left: 0,
          top: 16,
          bottom: 16,
          width: 4,
          borderRadius: 999,
          backgroundColor: isActive
            ? alpha(theme.palette.warning.main, 0.92)
            : isPlayed
              ? alpha(theme.palette.success.main, 0.82)
              : alpha(theme.palette.primary.main, 0.36),
        },
      })}
    >
      <Stack spacing={1}>
        <Stack direction="row" spacing={1.15} alignItems="center">
          <Box
            sx={(theme) => ({
              display: 'grid',
              placeItems: 'center',
              width: 44,
              height: 44,
              flexShrink: 0,
              borderRadius: '16px',
              border: `1px solid ${alpha(
                isActive ? theme.palette.warning.main : theme.palette.primary.main,
                0.34,
              )}`,
              backgroundColor: alpha(
                isActive ? theme.palette.warning.main : theme.palette.common.black,
                isActive ? 0.18 : 0.18,
              ),
              boxShadow: `inset 0 1px 0 ${alpha(theme.palette.common.white, 0.1)}`,
            })}
          >
            <Typography variant="h6" fontWeight={900} sx={{ lineHeight: 1 }}>
              {team.teamSlotIndex}
            </Typography>
          </Box>

          <Stack spacing={0.5} sx={{ minWidth: 0, flex: 1 }}>
            <Stack direction="row" spacing={0.8} alignItems="center" flexWrap="wrap" useFlexGap>
              <Typography variant="subtitle2" fontWeight={900}>
                {formatTeamQueueName(t, team.teamName, team.teamSlotIndex)}
              </Typography>
              {playedOrder ? (
                <Chip
                  size="small"
                  color="success"
                  variant="filled"
                  label={t('gameBoard.teamQueuePlayedOrderLabel', { order: playedOrder })}
                />
              ) : null}
              {isActive ? (
                <Chip size="small" color="warning" label={t('gameBoard.teamQueueActiveChip')} />
              ) : null}
            </Stack>
          </Stack>
        </Stack>

        <Stack spacing={0.7} sx={{ pl: 0.4 }}>
          {team.participants.map((participant, index) => (
            <Box
              key={participant.userId}
              sx={(theme) => ({
                display: 'grid',
                gridTemplateColumns: '18px 1fr',
                gap: 1.25,
                alignItems: 'center',
                minWidth: 0,
                px: 1,
                py: 0.75,
                borderRadius: 2,
                backgroundColor: alpha(theme.palette.common.black, 0.12),
                border: `1px solid ${alpha(theme.palette.common.white, 0.06)}`,
              })}
            >
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ textAlign: 'right', fontWeight: 700 }}
              >
                {index + 1}.
              </Typography>
              <Typography variant="body2" noWrap title={participant.displayName} fontWeight={600}>
                {participant.displayName}
              </Typography>
            </Box>
          ))}
        </Stack>
      </Stack>
    </Box>
  )
}

function formatTeamQueueName(
  t: ReturnType<typeof useTranslation>['t'],
  teamName: string | null | undefined,
  teamSlotIndex: number,
) {
  return formatTeamNameWithFallback(
    teamName,
    t('gameBoard.teamQueueTeamTitle', { slot: teamSlotIndex }),
  )
}
