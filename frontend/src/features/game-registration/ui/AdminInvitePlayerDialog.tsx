import { Stack, TextField, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameRegistrationAdminSnapshot,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, AppDialog, SectionCard } from '../../../shared/ui/index.ts'

export type AdminInviteTeamTarget = {
  slot: GameRegistrationAdminSnapshot['slots'][number]
  team: RegistrationTeam
}

interface AdminInvitePlayerDialogProps {
  target: AdminInviteTeamTarget | null
  availablePlayers: readonly RegistrationPlayer[]
  isBusy: boolean
  onClose: () => void
  onInvite: (slotId: string, invitedUserId: string, teamId: string) => void
}

const maxVisibleSearchResults = 18
const minimumSearchLength = 2

function sortPlayers(players: readonly RegistrationPlayer[]) {
  return [...players].sort((left, right) => {
    const displayNameOrder = left.displayName.localeCompare(right.displayName)
    if (displayNameOrder !== 0) {
      return displayNameOrder
    }

    return left.login.localeCompare(right.login)
  })
}

export function AdminInvitePlayerDialog({
  target,
  availablePlayers,
  isBusy,
  onClose,
  onInvite,
}: AdminInvitePlayerDialogProps) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')

  const normalizedQuery = query.trim().toLowerCase()
  const visiblePlayers = useMemo(() => {
    if (normalizedQuery.length > 0 && normalizedQuery.length < minimumSearchLength) {
      return []
    }

    const sortedPlayers = sortPlayers(availablePlayers)
    const matchingPlayers =
      normalizedQuery.length === 0
        ? sortedPlayers
        : sortedPlayers.filter((player) => {
            const displayName = player.displayName.toLowerCase()
            const login = player.login.toLowerCase()
            return displayName.includes(normalizedQuery) || login.includes(normalizedQuery)
          })

    return matchingPlayers.slice(0, maxVisibleSearchResults)
  }, [availablePlayers, normalizedQuery])

  const handleClose = () => {
    setQuery('')
    onClose()
  }

  if (target === null) {
    return null
  }

  return (
    <AppDialog
      open
      onClose={isBusy ? undefined : handleClose}
      title={t('gameApplication.adminPanel.inviteDialogTitle', {
        slot: target.team.slotIndex,
      })}
      description={t('gameApplication.adminPanel.inviteDialogDescription')}
      actions={
        <AppButton tone="ghost" onClick={handleClose} disabled={isBusy}>
          {t('gameApplication.adminPanel.inviteDialogClose')}
        </AppButton>
      }
    >
      <Stack spacing={1.5}>
        <TextField
          fullWidth
          size="small"
          label={t('gameApplication.adminPanel.inviteDialogSearchLabel')}
          placeholder={t('gameApplication.adminPanel.playerSearchPlaceholder')}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />

        <Typography variant="caption" color="text.secondary">
          {normalizedQuery.length > 0 && normalizedQuery.length < minimumSearchLength
            ? t('gameApplication.adminPanel.playerSearchMin', { min: minimumSearchLength })
            : t('gameApplication.adminPanel.inviteDialogResults', {
                count: visiblePlayers.length,
              })}
        </Typography>

        <Stack spacing={1}>
          {visiblePlayers.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {availablePlayers.length === 0
                ? t('gameApplication.adminPanel.inviteDialogNoAvailablePlayers')
                : t('gameApplication.adminPanel.noPlayersMatched')}
            </Typography>
          ) : (
            visiblePlayers.map((player) => (
              <SectionCard key={player.userId} inset>
                <Stack
                  direction="row"
                  spacing={1}
                  alignItems="center"
                  justifyContent="space-between"
                >
                  <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                    <Typography variant="body2" fontWeight={700} noWrap>
                      {player.displayName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary" noWrap>
                      @{player.login}
                    </Typography>
                  </Stack>
                  <AppButton
                    size="small"
                    disabled={isBusy}
                    onClick={() => {
                      setQuery('')
                      onInvite(target.slot.slotId, player.userId, target.team.teamId)
                    }}
                  >
                    {t('gameApplication.adminPanel.inviteDialogSend')}
                  </AppButton>
                </Stack>
              </SectionCard>
            ))
          )}
        </Stack>
      </Stack>
    </AppDialog>
  )
}
