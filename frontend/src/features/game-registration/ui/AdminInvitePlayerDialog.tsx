import { Stack, TextField, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type {
  GameRegistrationAdminSnapshot,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, AppDialog, SectionCard } from '../../../shared/ui/index.ts'
import { searchRegistrationPlayers } from '../model/player-search.ts'

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

export function AdminInvitePlayerDialog({
  target,
  availablePlayers,
  isBusy,
  onClose,
  onInvite,
}: AdminInvitePlayerDialogProps) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const playerSearch = useMemo(
    () =>
      searchRegistrationPlayers(availablePlayers, {
        query,
        minQueryLength: minimumSearchLength,
        limit: maxVisibleSearchResults,
        includeAllWhenQueryEmpty: true,
      }),
    [availablePlayers, query],
  )

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
          {playerSearch.isTooShort
            ? t('gameApplication.adminPanel.playerSearchMin', { min: minimumSearchLength })
            : t('gameApplication.adminPanel.inviteDialogResults', {
                count: playerSearch.visible.length,
              })}
        </Typography>

        <Stack spacing={1}>
          {playerSearch.visible.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {availablePlayers.length === 0
                ? t('gameApplication.adminPanel.inviteDialogNoAvailablePlayers')
                : t('gameApplication.adminPanel.noPlayersMatched')}
            </Typography>
          ) : (
            playerSearch.visible.map((player) => (
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
