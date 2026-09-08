import { Chip, Stack, TextField, Tooltip, Typography } from '@mui/material'
import { useMemo, useState, type DragEvent } from 'react'
import { useTranslation } from 'react-i18next'
import type { RegistrationPlayer } from '../../../shared/api/contracts/index.ts'
import { SectionCard } from '../../../shared/ui/index.ts'
import { searchRegistrationPlayers } from '../model/player-search.ts'
import { AdminRegistrationPlayerCard } from './admin-registration-components.tsx'
import {
  defaultVisiblePlayersCount,
  maxVisibleSearchResults,
  minimumSearchLength,
  type RegistrationDragPayload,
} from './admin-registration-support.ts'

interface AdminAvailablePlayersPanelProps {
  players: RegistrationPlayer[]
  onDragStart: (event: DragEvent<HTMLElement>, payload: RegistrationDragPayload) => void
  onDragEnd: () => void
}

export function AdminAvailablePlayersPanel({
  players,
  onDragStart,
  onDragEnd,
}: AdminAvailablePlayersPanelProps) {
  const { t, i18n } = useTranslation()
  const [playerQuery, setPlayerQuery] = useState('')
  const playerSearch = useMemo(
    () =>
      searchRegistrationPlayers(players, {
        query: playerQuery,
        minQueryLength: minimumSearchLength,
        limit:
          playerQuery.trim().length === 0 ? defaultVisiblePlayersCount : maxVisibleSearchResults,
        includeAllWhenQueryEmpty: true,
        locale: i18n.resolvedLanguage,
      }),
    [i18n.resolvedLanguage, playerQuery, players],
  )
  const normalizedPlayerQuery = playerSearch.normalizedQuery
  const visiblePlayers = playerSearch.visible
  const hiddenPlayersCount = playerSearch.hiddenCount

  return (
    <SectionCard
      inset
      sx={{
        width: { xs: '100%', lg: 288 },
        flexShrink: 0,
        alignSelf: { xs: 'auto', lg: 'flex-start' },
        position: { lg: 'sticky' },
        top: { lg: 12 },
        maxHeight: { xs: 360, lg: 'calc(100vh - 112px)' },
        overflowY: 'auto',
        p: 1.25,
      }}
    >
      <Stack spacing={1}>
        <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
          <Typography variant="subtitle2">
            {t('gameApplication.adminPanel.availablePlayers')}
          </Typography>
          <Tooltip
            title={t('gameApplication.adminPanel.availablePlayersDescription')}
            describeChild
            arrow
          >
            <Chip
              size="small"
              variant="outlined"
              label={players.length}
              aria-label={`${t('gameApplication.adminPanel.availablePlayers')}: ${players.length}`}
              tabIndex={0}
            />
          </Tooltip>
        </Stack>

        <TextField
          fullWidth
          size="small"
          label={t('gameApplication.adminPanel.playerSearchLabel')}
          placeholder={t('gameApplication.adminPanel.playerSearchPlaceholder')}
          value={playerQuery}
          onChange={(event) => setPlayerQuery(event.target.value)}
        />

        {normalizedPlayerQuery.length > 0 ? (
          <Typography variant="caption" color="text.secondary">
            {normalizedPlayerQuery.length < minimumSearchLength
              ? t('gameApplication.adminPanel.playerSearchMin', {
                  min: minimumSearchLength,
                })
              : t('gameApplication.adminPanel.playerSearchResults', {
                  count: playerSearch.matches.length,
                })}
          </Typography>
        ) : null}

        <Stack component="ul" spacing={0} sx={{ m: 0, p: 0 }}>
          {visiblePlayers.length === 0 ? (
            <Typography
              component="li"
              variant="body2"
              color="text.secondary"
              sx={{ listStyle: 'none', py: 1 }}
            >
              {players.length === 0
                ? t('gameApplication.adminPanel.noAvailablePlayers')
                : t('gameApplication.adminPanel.noPlayersMatched')}
            </Typography>
          ) : (
            visiblePlayers.map((player) => (
              <AdminRegistrationPlayerCard
                key={player.userId}
                player={player}
                testId={`admin-player-${player.userId}`}
                onDragStart={(event) => {
                  const payload: RegistrationDragPayload = {
                    kind: 'player',
                    userId: player.userId,
                  }
                  onDragStart(event, payload)
                }}
                onDragEnd={onDragEnd}
              />
            ))
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
  )
}
