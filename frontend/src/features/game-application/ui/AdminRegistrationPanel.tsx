import { Box, Chip, Divider, Stack, TextField, Typography } from '@mui/material'
import { useMemo, useState, type DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameRegistrationAdminSnapshot,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { formatRegistrationTeamStatus } from '../../game-registration/index.ts'

interface AdminRegistrationPanelProps {
  snapshot: GameRegistrationAdminSnapshot
  isCreatingTeam: boolean
  isAssigningPlayer: boolean
  isMovingTeam: boolean
  isConfirmingTeam: (teamId: string) => boolean
  isRejectingTeam: (teamId: string) => boolean
  onCreateTeam: (recruitmentOpen: boolean, slotId?: string) => void
  onAssignPlayer: (teamId: string, userId: string) => void
  onMoveTeam: (teamId: string, targetSlotId: string) => void
  onConfirmTeam: (teamId: string) => void
  onRejectTeam: (teamId: string) => void
}

type DragPayload =
  | { kind: 'player'; userId: string }
  | { kind: 'team'; teamId: string }

const registrationDragMimeType = 'application/x-deadmans-registration'
const defaultVisiblePlayersCount = 12
const maxVisibleSearchResults = 18
const minimumSearchLength = 2

function writeDragPayload(event: DragEvent<HTMLElement>, payload: DragPayload) {
  event.dataTransfer.effectAllowed = 'move'
  event.dataTransfer.setData(registrationDragMimeType, JSON.stringify(payload))
}

function readDragPayload(event: DragEvent<HTMLElement>): DragPayload | null {
  const rawPayload = event.dataTransfer.getData(registrationDragMimeType)
  if (!rawPayload) {
    return null
  }

  try {
    const parsed = JSON.parse(rawPayload) as Partial<DragPayload>
    if (parsed.kind === 'player' && typeof parsed.userId === 'string') {
      return { kind: 'player', userId: parsed.userId }
    }

    if (parsed.kind === 'team' && typeof parsed.teamId === 'string') {
      return { kind: 'team', teamId: parsed.teamId }
    }
  } catch {
    return null
  }

  return null
}

function sortPlayers(players: readonly RegistrationPlayer[]) {
  return [...players].sort((left, right) => {
    const displayNameOrder = left.displayName.localeCompare(right.displayName)
    if (displayNameOrder !== 0) {
      return displayNameOrder
    }

    return left.login.localeCompare(right.login)
  })
}

function PlayerCard({
  player,
  compact = false,
}: {
  player: RegistrationPlayer
  compact?: boolean
}) {
  return (
    <SectionCard
      inset
      draggable
      onDragStart={(event) => writeDragPayload(event, { kind: 'player', userId: player.userId })}
      sx={{
        p: compact ? 1 : 1.25,
        cursor: 'grab',
        minWidth: 0,
      }}
    >
      <Stack spacing={0.25}>
        <Typography variant="body2" fontWeight={700} noWrap>
          {player.displayName}
        </Typography>
        <Typography variant="caption" color="text.secondary" noWrap>
          @{player.login}
        </Typography>
      </Stack>
    </SectionCard>
  )
}

function TeamHeaderChips({
  team,
  membersCount,
  t,
}: {
  team: RegistrationTeam
  membersCount: number
  t: ReturnType<typeof useTranslation>['t']
}) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center" flexWrap="wrap" useFlexGap>
      <Chip
        label={t('gameApplication.adminPanel.slotLabel', { slot: team.slotIndex })}
        draggable
        onDragStart={(event) => writeDragPayload(event, { kind: 'team', teamId: team.teamId })}
        sx={{ cursor: 'grab' }}
      />
      <Chip size="small" label={formatRegistrationTeamStatus(team.status, t)} />
      <Chip
        size="small"
        color={team.recruitmentOpen ? 'success' : 'default'}
        label={
          team.recruitmentOpen
            ? t('gameApplication.recruitmentOpen')
            : t('gameApplication.recruitmentClosed')
        }
      />
      <Chip
        size="small"
        variant="outlined"
        label={t('gameApplication.adminPanel.membersChip', { count: membersCount })}
      />
    </Stack>
  )
}

export function AdminRegistrationPanel({
  snapshot,
  isCreatingTeam,
  isAssigningPlayer,
  isMovingTeam,
  isConfirmingTeam,
  isRejectingTeam,
  onCreateTeam,
  onAssignPlayer,
  onMoveTeam,
  onConfirmTeam,
  onRejectTeam,
}: AdminRegistrationPanelProps) {
  const { t } = useTranslation()
  const [activeDropTeamId, setActiveDropTeamId] = useState<string | null>(null)
  const [activeDropSlotId, setActiveDropSlotId] = useState<string | null>(null)
  const [playerQuery, setPlayerQuery] = useState('')

  const sortedSlots = useMemo(
    () => [...snapshot.slots].sort((left, right) => left.slotIndex - right.slotIndex),
    [snapshot.slots],
  )

  const teamsById = useMemo(
    () => new Map(snapshot.teams.map((team) => [team.teamId, team])),
    [snapshot.teams],
  )

  const normalizedPlayerQuery = playerQuery.trim().toLowerCase()
  const sortedPlayers = useMemo(() => sortPlayers(snapshot.availablePlayers), [snapshot.availablePlayers])

  const matchingPlayers = useMemo(() => {
    if (normalizedPlayerQuery.length === 0) {
      return sortedPlayers
    }

    if (normalizedPlayerQuery.length < minimumSearchLength) {
      return []
    }

    return sortedPlayers.filter((player) => {
      const displayName = player.displayName.toLowerCase()
      const login = player.login.toLowerCase()
      return displayName.includes(normalizedPlayerQuery) || login.includes(normalizedPlayerQuery)
    })
  }, [normalizedPlayerQuery, sortedPlayers])

  const visiblePlayers = useMemo(() => {
    if (normalizedPlayerQuery.length === 0) {
      return matchingPlayers.slice(0, defaultVisiblePlayersCount)
    }

    if (normalizedPlayerQuery.length < minimumSearchLength) {
      return []
    }

    return matchingPlayers.slice(0, maxVisibleSearchResults)
  }, [matchingPlayers, normalizedPlayerQuery.length])

  const hiddenPlayersCount = Math.max(0, matchingPlayers.length - visiblePlayers.length)
  const teamsCount = snapshot.teams.length
  const openTeamsCount = snapshot.teams.filter((team) => team.recruitmentOpen).length
  const confirmedTeamsCount = snapshot.teams.filter((team) => team.status === 'confirmed').length

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.5}>
        <SectionCard inset sx={{ flex: 1 }}>
          <Stack spacing={0.75}>
            <Typography variant="overline" color="text.secondary">
              {t('gameApplication.adminPanel.summaryTeams')}
            </Typography>
            <Typography variant="subtitle2">
              {t('gameApplication.adminPanel.summaryTeamsValue', {
                total: teamsCount,
                open: openTeamsCount,
              })}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.adminPanel.summaryTeamsDescription')}
            </Typography>
          </Stack>
        </SectionCard>

        <SectionCard inset sx={{ flex: 1 }}>
          <Stack spacing={0.75}>
            <Typography variant="overline" color="text.secondary">
              {t('gameApplication.adminPanel.summaryPlayers')}
            </Typography>
            <Typography variant="subtitle2">
              {t('gameApplication.adminPanel.summaryPlayersValue', {
                count: snapshot.availablePlayers.length,
              })}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.adminPanel.summaryPlayersDescription')}
            </Typography>
          </Stack>
        </SectionCard>

        <SectionCard inset sx={{ flex: 1 }}>
          <Stack spacing={0.75}>
            <Typography variant="overline" color="text.secondary">
              {t('gameApplication.adminPanel.summaryRules')}
            </Typography>
            <Typography variant="subtitle2">
              {t('gameApplication.adminPanel.summaryRulesValue', {
                min: snapshot.minPlayersPerTeam,
                max: snapshot.maxPlayersPerTeam,
              })}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t('gameApplication.adminPanel.summaryRulesDescription', {
                count: confirmedTeamsCount,
              })}
            </Typography>
          </Stack>
        </SectionCard>
      </Stack>

      <SectionCard>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: 'column', lg: 'row' }}
            spacing={2}
            justifyContent="space-between"
            alignItems={{ xs: 'stretch', lg: 'flex-start' }}
          >
            <Stack spacing={0.5}>
              <Typography variant="subtitle1">{t('gameApplication.adminPanel.title')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.adminPanel.description')}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {t('gameApplication.adminPanel.assignHint')}
              </Typography>
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <AppButton disabled={isCreatingTeam} onClick={() => onCreateTeam(true)}>
                {t('gameApplication.adminPanel.createOpenTeam')}
              </AppButton>
              <AppButton
                tone="secondary"
                disabled={isCreatingTeam}
                onClick={() => onCreateTeam(false)}
              >
                {t('gameApplication.adminPanel.createPrivateTeam')}
              </AppButton>
            </Stack>
          </Stack>

          <Divider />

          <Stack direction={{ xs: 'column', xl: 'row' }} spacing={2} alignItems="stretch">
            <SectionCard
              inset
              sx={{
                width: { xs: '100%', xl: 320 },
                flexShrink: 0,
                alignSelf: 'stretch',
                background:
                  'linear-gradient(180deg, rgba(198, 160, 95, 0.12) 0%, rgba(0, 0, 0, 0.16) 100%)',
              }}
            >
              <Stack spacing={1.5}>
                <Stack spacing={0.5}>
                  <Typography variant="subtitle2">
                    {t('gameApplication.adminPanel.availablePlayers')}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {t('gameApplication.adminPanel.availablePlayersDescription')}
                  </Typography>
                </Stack>

                <TextField
                  fullWidth
                  size="small"
                  label={t('gameApplication.adminPanel.playerSearchLabel')}
                  placeholder={t('gameApplication.adminPanel.playerSearchPlaceholder')}
                  value={playerQuery}
                  onChange={(event) => setPlayerQuery(event.target.value)}
                />

                <Typography variant="caption" color="text.secondary">
                  {normalizedPlayerQuery.length === 0
                    ? t('gameApplication.adminPanel.playerSearchIdle', {
                        count: snapshot.availablePlayers.length,
                        visible: Math.min(defaultVisiblePlayersCount, snapshot.availablePlayers.length),
                      })
                    : normalizedPlayerQuery.length < minimumSearchLength
                      ? t('gameApplication.adminPanel.playerSearchMin', {
                          min: minimumSearchLength,
                        })
                      : t('gameApplication.adminPanel.playerSearchResults', {
                          count: matchingPlayers.length,
                        })}
                </Typography>

                <Stack spacing={1}>
                  {visiblePlayers.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      {snapshot.availablePlayers.length === 0
                        ? t('gameApplication.adminPanel.noAvailablePlayers')
                        : t('gameApplication.adminPanel.noPlayersMatched')}
                    </Typography>
                  ) : (
                    visiblePlayers.map((player) => <PlayerCard key={player.userId} player={player} />)
                  )}
                </Stack>

                {hiddenPlayersCount > 0 ? (
                  <Typography variant="caption" color="text.secondary">
                    {t('gameApplication.adminPanel.hiddenPlayersHint', {
                      count: hiddenPlayersCount,
                    })}
                  </Typography>
                ) : null}
              </Stack>
            </SectionCard>

            <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
              {sortedSlots.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.adminPanel.emptyTeams')}
                </Typography>
              ) : null}

              {sortedSlots.map((slot) => {
                const team = slot.teamId ? teamsById.get(slot.teamId) ?? null : null
                const membersCount = team?.members.length ?? 0
                const isTeamReady =
                  team != null &&
                  team.status === 'forming' &&
                  membersCount >= snapshot.minPlayersPerTeam &&
                  membersCount <= snapshot.maxPlayersPerTeam
                const isSlotDropActive = activeDropSlotId === slot.slotId
                const isTeamDropActive = team != null && activeDropTeamId === team.teamId

                return (
                  <SectionCard
                    key={slot.slotId}
                    inset
                    sx={{
                      borderStyle: isSlotDropActive || isTeamDropActive ? 'solid' : undefined,
                      borderColor: isSlotDropActive || isTeamDropActive ? 'primary.main' : undefined,
                      background:
                        team == null
                          ? 'linear-gradient(180deg, rgba(255,255,255,0.02) 0%, rgba(198, 160, 95, 0.08) 100%)'
                          : undefined,
                    }}
                    onDragOver={(event) => {
                      const payload = readDragPayload(event)
                      if (!payload) {
                        return
                      }

                      if (payload.kind === 'player' && team == null) {
                        return
                      }

                      if (payload.kind === 'team' && team?.teamId === payload.teamId) {
                        return
                      }

                      event.preventDefault()

                      if (payload.kind === 'player' && team != null) {
                        setActiveDropTeamId(team.teamId)
                        setActiveDropSlotId(null)
                      }

                      if (payload.kind === 'team') {
                        setActiveDropSlotId(slot.slotId)
                        setActiveDropTeamId(null)
                      }
                    }}
                    onDragLeave={() => {
                      setActiveDropTeamId((current) => (current === team?.teamId ? null : current))
                      setActiveDropSlotId((current) => (current === slot.slotId ? null : current))
                    }}
                    onDrop={(event) => {
                      event.preventDefault()
                      const payload = readDragPayload(event)
                      setActiveDropTeamId(null)
                      setActiveDropSlotId(null)

                      if (!payload) {
                        return
                      }

                      if (payload.kind === 'player' && team != null) {
                        onAssignPlayer(team.teamId, payload.userId)
                        return
                      }

                      if (payload.kind === 'team' && payload.teamId !== team?.teamId) {
                        onMoveTeam(payload.teamId, slot.slotId)
                      }
                    }}
                  >
                    {team == null ? (
                      <Stack
                        direction={{ xs: 'column', lg: 'row' }}
                        spacing={1.5}
                        justifyContent="space-between"
                        alignItems={{ xs: 'stretch', lg: 'center' }}
                      >
                        <Stack spacing={0.75}>
                          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                            <Chip label={t('gameApplication.adminPanel.slotLabel', { slot: slot.slotIndex })} />
                            <Chip
                              size="small"
                              variant="outlined"
                              label={t('gameApplication.adminPanel.emptySlotChip')}
                            />
                          </Stack>

                          <Typography variant="subtitle2">
                            {t('gameApplication.adminPanel.emptySlotTitle')}
                          </Typography>

                          <Typography variant="body2" color="text.secondary">
                            {isSlotDropActive
                              ? t('gameApplication.adminPanel.dropTeam')
                              : t('gameApplication.adminPanel.emptySlotDescription')}
                          </Typography>
                        </Stack>

                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                          <AppButton
                            size="small"
                            disabled={isCreatingTeam || !slot.isAvailableForNewTeam}
                            onClick={() => onCreateTeam(true, slot.slotId)}
                          >
                            {t('gameApplication.adminPanel.createOpenTeamInSlot')}
                          </AppButton>
                          <AppButton
                            size="small"
                            tone="secondary"
                            disabled={isCreatingTeam || !slot.isAvailableForNewTeam}
                            onClick={() => onCreateTeam(false, slot.slotId)}
                          >
                            {t('gameApplication.adminPanel.createPrivateTeamInSlot')}
                          </AppButton>
                        </Stack>
                      </Stack>
                    ) : (
                      <Stack spacing={1.5}>
                        <Stack
                          direction={{ xs: 'column', lg: 'row' }}
                          spacing={1.5}
                          justifyContent="space-between"
                          alignItems={{ xs: 'stretch', lg: 'flex-start' }}
                        >
                          <Stack spacing={1}>
                            <TeamHeaderChips team={team} membersCount={membersCount} t={t} />

                            <Typography variant="subtitle2">
                              {t('gameApplication.adminPanel.teamTitle', { slot: team.slotIndex })}
                            </Typography>

                            <Typography variant="body2" color="text.secondary">
                              {isTeamDropActive
                                ? t('gameApplication.adminPanel.dropPlayer')
                                : isSlotDropActive
                                  ? t('gameApplication.adminPanel.dropTeam')
                                  : isTeamReady
                                    ? t('gameApplication.adminPanel.teamReadyHint')
                                    : t('gameApplication.adminPanel.teamNeedsPlayersHint', {
                                        min: snapshot.minPlayersPerTeam,
                                        max: snapshot.maxPlayersPerTeam,
                                      })}
                            </Typography>
                          </Stack>

                          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                            <AppButton
                              size="small"
                              disabled={
                                !isTeamReady ||
                                isAssigningPlayer ||
                                isMovingTeam ||
                                isConfirmingTeam(team.teamId)
                              }
                              onClick={() => onConfirmTeam(team.teamId)}
                            >
                              {t('teamRegistrations.confirm')}
                            </AppButton>
                            <AppButton
                              size="small"
                              tone="warningGhost"
                              disabled={team.status !== 'forming' || isRejectingTeam(team.teamId)}
                              onClick={() => onRejectTeam(team.teamId)}
                            >
                              {t('teamRegistrations.reject')}
                            </AppButton>
                          </Stack>
                        </Stack>

                        <Box
                          sx={{
                            display: 'grid',
                            gap: 1,
                            gridTemplateColumns: {
                              xs: 'minmax(0, 1fr)',
                              md: 'repeat(2, minmax(0, 1fr))',
                            },
                          }}
                        >
                          {team.members.length === 0 ? (
                            <SectionCard inset variantStyle="dashed">
                              <Typography variant="body2" color="text.secondary">
                                {t('gameApplication.adminPanel.emptyTeam')}
                              </Typography>
                            </SectionCard>
                          ) : (
                            team.members.map((member) => (
                              <PlayerCard
                                key={member.player.userId}
                                player={member.player}
                                compact
                              />
                            ))
                          )}
                        </Box>
                      </Stack>
                    )}
                  </SectionCard>
                )
              })}
            </Stack>
          </Stack>
        </Stack>
      </SectionCard>
    </Stack>
  )
}
