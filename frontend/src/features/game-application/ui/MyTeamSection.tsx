import { useMemo, useState } from 'react'
import { Chip, Stack, TextField, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type {
  RegistrationInvitation,
  RegistrationPlayer,
  RegistrationTeam,
} from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { searchRegistrationPlayers } from '../../game-registration/model/player-search.ts'
import { formatRegistrationTeamStatus } from '../../game-registration/model/registration-team-status.ts'
import { RegistrationTeamNameEditor } from '../../game-registration/ui/RegistrationTeamNameEditor.tsx'
import { TeamSummary } from './TeamSummary.tsx'

const minimumInviteSearchLength = 3
const maximumInviteSearchResults = 6

interface MyTeamSectionProps {
  team: RegistrationTeam
  canInvitePlayers: boolean
  invitablePlayers: RegistrationPlayer[]
  outgoingInvitations: RegistrationInvitation[]
  onInvitePlayer: (userId: string) => void
  isInvitingPlayer: boolean
  onCancelInvitation: (invitationId: string) => void
  isCancellingInvitation: boolean
  onLeave: () => void
  isLeaving: boolean
  onRequestDisband: () => void
  isRequestingDisband: boolean
  onUpdateName: (name?: string) => void
  isUpdatingName: boolean
}

export function MyTeamSection({
  team,
  canInvitePlayers,
  invitablePlayers,
  outgoingInvitations,
  onInvitePlayer,
  isInvitingPlayer,
  onCancelInvitation,
  isCancellingInvitation,
  onLeave,
  isLeaving,
  onRequestDisband,
  isRequestingDisband,
  onUpdateName,
  isUpdatingName,
}: MyTeamSectionProps) {
  const { t } = useTranslation()
  const [inviteQuery, setInviteQuery] = useState('')
  const isClosedTeam = !team.recruitmentOpen
  const isConfirmedTeam = team.status === 'confirmed'
  const canEditName = team.status === 'forming'
  const hasDisbandRequest = team.disbandRequestedAtUtc != null
  const pendingOutgoingInvitation = outgoingInvitations[0] ?? null
  const isLeaveBlocked = pendingOutgoingInvitation !== null || isConfirmedTeam
  const inviteSearch = useMemo(
    () =>
      searchRegistrationPlayers(invitablePlayers, {
        query: inviteQuery,
        minQueryLength: minimumInviteSearchLength,
        limit: maximumInviteSearchResults,
        rankStartsWith: true,
      }),
    [inviteQuery, invitablePlayers],
  )
  const inviteSearchReady =
    inviteSearch.normalizedQuery.length >= minimumInviteSearchLength && !inviteSearch.isTooShort
  const filteredPlayers = inviteSearch.visible
  const hiddenMatchesCount = inviteSearch.hiddenCount

  const inviteSearchState = useMemo(() => {
    if (!inviteSearchReady) {
      return 'idle'
    }

    if (filteredPlayers.length === 0) {
      return 'empty'
    }

    return 'results'
  }, [inviteSearchReady, filteredPlayers.length])

  return (
    <SectionCard
      sx={{
        height: '100%',
        background: 'linear-gradient(180deg, rgba(95, 196, 150, 0.12) 0%, rgba(0,0,0,0.08) 100%)',
      }}
    >
      <Stack spacing={2.5}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle1">{t('gameApplication.myTeamTitle')}</Typography>
            <Chip size="small" color="success" label={t('gameApplication.myTeamChip')} />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {t('gameApplication.myTeamDescription')}
          </Typography>
        </Stack>

        <TeamSummary team={team} />

        <SectionCard inset>
          <RegistrationTeamNameEditor
            value={team.name}
            canEdit={canEditName}
            isSaving={isUpdatingName}
            onSave={onUpdateName}
            buttonSx={{ mt: { md: 0.35 }, minWidth: 112 }}
          />
        </SectionCard>

        {isClosedTeam ? (
          <SectionCard inset>
            <Stack spacing={1.5}>
              <Typography variant="subtitle2">
                {t('gameApplication.inviteTeammateTitle')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {canInvitePlayers
                  ? t('gameApplication.inviteTeammateDescription')
                  : pendingOutgoingInvitation
                    ? t('gameApplication.invitePendingDescription', {
                        player:
                          pendingOutgoingInvitation.invitedUserDisplayName ??
                          t('gameApplication.unknownPlayer'),
                      })
                    : t('gameApplication.inviteUnavailableDescription')}
              </Typography>

              {canInvitePlayers ? (
                <Stack spacing={1.25}>
                  <TextField
                    fullWidth
                    size="small"
                    label={t('gameApplication.inviteTeammateField')}
                    placeholder={t('gameApplication.inviteTeammatePlaceholder')}
                    value={inviteQuery}
                    onChange={(event) => setInviteQuery(event.target.value)}
                  />

                  <Typography variant="caption" color="text.secondary">
                    {t('gameApplication.inviteSearchHint', {
                      count: invitablePlayers.length,
                      min: minimumInviteSearchLength,
                    })}
                  </Typography>

                  <SectionCard inset variantStyle="dashed" sx={{ p: 1.25 }}>
                    <Stack spacing={1}>
                      {inviteSearchState === 'idle' ? (
                        <Typography variant="body2" color="text.secondary">
                          {t('gameApplication.inviteSearchStartTyping', {
                            min: minimumInviteSearchLength,
                          })}
                        </Typography>
                      ) : null}

                      {inviteSearchState === 'results'
                        ? filteredPlayers.map((player) => (
                            <Stack
                              key={player.userId}
                              direction={{ xs: 'column', sm: 'row' }}
                              spacing={1}
                              alignItems={{ sm: 'center' }}
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
                                disabled={isInvitingPlayer}
                                onClick={() => onInvitePlayer(player.userId)}
                                sx={{ minWidth: { sm: 140 } }}
                              >
                                {t('gameApplication.inviteTeammateAction')}
                              </AppButton>
                            </Stack>
                          ))
                        : null}

                      {inviteSearchState === 'empty' ? (
                        <Typography variant="body2" color="text.secondary">
                          {t('gameApplication.inviteNoPlayersFound')}
                        </Typography>
                      ) : null}

                      {inviteSearchState === 'results' && hiddenMatchesCount > 0 ? (
                        <Typography variant="caption" color="text.secondary">
                          {t('gameApplication.inviteSearchTooManyResults', {
                            count: hiddenMatchesCount,
                          })}
                        </Typography>
                      ) : null}
                    </Stack>
                  </SectionCard>
                </Stack>
              ) : null}

              {pendingOutgoingInvitation ? (
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1}
                  alignItems={{ sm: 'center' }}
                >
                  <Chip
                    size="small"
                    color="warning"
                    label={t('gameApplication.invitePendingChip', {
                      player:
                        pendingOutgoingInvitation.invitedUserDisplayName ??
                        t('gameApplication.unknownPlayer'),
                    })}
                    sx={{ alignSelf: 'flex-start' }}
                  />
                  <AppButton
                    size="small"
                    tone="secondary"
                    disabled={isCancellingInvitation}
                    onClick={() => onCancelInvitation(pendingOutgoingInvitation.invitationId)}
                  >
                    {t('gameApplication.cancelInvitation')}
                  </AppButton>
                </Stack>
              ) : null}
            </Stack>
          </SectionCard>
        ) : null}

        <SectionCard inset variantStyle="dashed">
          <Stack spacing={1}>
            <Typography variant="body2" color="text.secondary">
              {isConfirmedTeam
                ? hasDisbandRequest
                  ? t('gameApplication.disbandRequestPendingHelper')
                  : t('gameApplication.confirmedTeamLeaveHelper')
                : pendingOutgoingInvitation
                  ? t('gameApplication.leaveTeamBlockedHelper')
                  : t('gameApplication.leaveTeamHelper')}
            </Typography>
            {isConfirmedTeam ? (
              <AppButton
                tone="warningGhost"
                disabled={isRequestingDisband || hasDisbandRequest}
                onClick={onRequestDisband}
                sx={{ alignSelf: 'flex-start' }}
              >
                {hasDisbandRequest
                  ? t('gameApplication.disbandRequestPending')
                  : t('gameApplication.requestDisband')}
              </AppButton>
            ) : null}
          </Stack>
        </SectionCard>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ sm: 'center' }}>
          <Chip size="small" label={formatRegistrationTeamStatus(team.status, t)} />
          {!isConfirmedTeam ? (
            <AppButton tone="warningGhost" disabled={isLeaving || isLeaveBlocked} onClick={onLeave}>
              {t('gameApplication.leaveTeam')}
            </AppButton>
          ) : null}
        </Stack>
      </Stack>
    </SectionCard>
  )
}
