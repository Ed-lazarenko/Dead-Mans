import { Box, Chip, Divider, Drawer, IconButton, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameRegistrationAdminSnapshot } from '../../../shared/api/contracts/index.ts'
import { AppButton, ConfirmDialog, SectionCard } from '../../../shared/ui/index.ts'

interface AdminGameLaunchDrawerProps {
  snapshot: GameRegistrationAdminSnapshot
  isStartingGame: boolean
  onStartGame: () => void
}

function AdminGameLaunchDrawer({
  snapshot,
  isStartingGame,
  onStartGame,
}: AdminGameLaunchDrawerProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)

  const launchState = useMemo(() => {
    const confirmedTeams = snapshot.teams.filter((team) => team.status === 'confirmed')
    const formingTeamsCount = snapshot.teams.filter((team) => team.status === 'forming').length
    const pendingInvitationsCount = snapshot.teams.reduce(
      (count, team) => count + (team.pendingInvitations?.length ?? 0),
      0,
    )
    const disbandRequestsCount = snapshot.teams.filter(
      (team) => team.disbandRequestedAtUtc != null,
    ).length
    const invalidConfirmedRostersCount = confirmedTeams.filter(
      (team) =>
        team.members.length < snapshot.minPlayersPerTeam ||
        team.members.length > snapshot.maxPlayersPerTeam,
    ).length

    const blockers: string[] = []
    if (confirmedTeams.length === 0) {
      blockers.push(t('gameApplication.adminPanel.launchBlockerNoConfirmedTeams'))
    }

    if (formingTeamsCount > 0) {
      blockers.push(
        t('gameApplication.adminPanel.launchBlockerUnconfirmedTeams', {
          count: formingTeamsCount,
        }),
      )
    }

    if (pendingInvitationsCount > 0) {
      blockers.push(
        t('gameApplication.adminPanel.launchBlockerPendingInvitations', {
          count: pendingInvitationsCount,
        }),
      )
    }

    if (disbandRequestsCount > 0) {
      blockers.push(
        t('gameApplication.adminPanel.launchBlockerDisbandRequests', {
          count: disbandRequestsCount,
        }),
      )
    }

    if (invalidConfirmedRostersCount > 0) {
      blockers.push(
        t('gameApplication.adminPanel.launchBlockerInvalidRosters', {
          count: invalidConfirmedRostersCount,
        }),
      )
    }

    return {
      blockers,
      confirmedTeamsCount: confirmedTeams.length,
      pendingInvitationsCount,
      disbandRequestsCount,
    }
  }, [snapshot, t])

  const canStartGame = launchState.blockers.length === 0

  return (
    <>
      <AppButton
        size="small"
        aria-label={t('gameApplication.adminPanel.launchPanelOpen')}
        onClick={() => setIsOpen(true)}
        sx={{ minHeight: 40 }}
      >
        <Stack direction="row" spacing={1} alignItems="center">
          <Box component="span">{t('gameApplication.adminPanel.launchPanelOpen')}</Box>
          <Chip
            aria-hidden
            size="small"
            color={canStartGame ? 'success' : 'warning'}
            label={
              canStartGame
                ? t('gameApplication.adminPanel.launchPanelReadyChip')
                : t('gameApplication.adminPanel.launchPanelBlockedChip', {
                    count: launchState.blockers.length,
                  })
            }
            sx={{ pointerEvents: 'none' }}
          />
        </Stack>
      </AppButton>

      <Drawer anchor="right" open={isOpen} onClose={() => setIsOpen(false)}>
        <Box
          sx={{
            width: { xs: '100vw', sm: 380 },
            maxWidth: '100vw',
            p: 2,
          }}
          role="presentation"
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              spacing={1.5}
              alignItems="flex-start"
              justifyContent="space-between"
            >
              <Stack spacing={0.5}>
                <Typography variant="h6">
                  {t('gameApplication.adminPanel.launchPanelTitle')}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.adminPanel.launchPanelDescription')}
                </Typography>
              </Stack>
              <IconButton
                size="small"
                aria-label={t('gameApplication.adminPanel.launchPanelClose')}
                onClick={() => setIsOpen(false)}
              >
                <Box component="span" aria-hidden sx={{ fontSize: 20, lineHeight: 1 }}>
                  ×
                </Box>
              </IconButton>
            </Stack>

            <Chip
              color={canStartGame ? 'success' : 'warning'}
              label={
                canStartGame
                  ? t('gameApplication.adminPanel.launchPanelReadyChip')
                  : t('gameApplication.adminPanel.launchPanelBlockedChip', {
                      count: launchState.blockers.length,
                    })
              }
              sx={{ alignSelf: 'flex-start' }}
            />

            <SectionCard inset>
              <Stack spacing={1}>
                <Typography variant="subtitle2">
                  {t('gameApplication.adminPanel.launchPanelValidationTitle')}
                </Typography>
                <Divider />
                <Typography variant="body2">
                  {t('gameApplication.adminPanel.launchPanelConfirmedTeams', {
                    count: launchState.confirmedTeamsCount,
                  })}
                </Typography>
                <Typography variant="body2">
                  {t('gameApplication.adminPanel.launchPanelPendingInvitations', {
                    count: launchState.pendingInvitationsCount,
                  })}
                </Typography>
                <Typography variant="body2">
                  {t('gameApplication.adminPanel.launchPanelDisbandRequests', {
                    count: launchState.disbandRequestsCount,
                  })}
                </Typography>
              </Stack>
            </SectionCard>

            <SectionCard inset variantStyle={canStartGame ? 'default' : 'dashed'}>
              {canStartGame ? (
                <Typography variant="body2" color="text.secondary">
                  {t('gameApplication.adminPanel.launchPanelReadyDescription')}
                </Typography>
              ) : (
                <Stack component="ul" spacing={1} sx={{ m: 0, pl: 2.5 }}>
                  {launchState.blockers.map((blocker) => (
                    <Typography key={blocker} component="li" variant="body2" color="text.secondary">
                      {blocker}
                    </Typography>
                  ))}
                </Stack>
              )}
            </SectionCard>

            <AppButton
              fullWidth
              disabled={!canStartGame || isStartingGame}
              onClick={() => setIsConfirmOpen(true)}
            >
              {t('gameApplication.adminPanel.launchGame')}
            </AppButton>
          </Stack>
        </Box>
      </Drawer>

      <ConfirmDialog
        open={isConfirmOpen}
        onClose={() => setIsConfirmOpen(false)}
        onConfirm={() => {
          onStartGame()
          setIsConfirmOpen(false)
          setIsOpen(false)
        }}
        isBusy={isStartingGame}
        title={t('gameApplication.adminPanel.launchConfirmTitle')}
        description={t('gameApplication.adminPanel.launchConfirmDescription')}
        cancelLabel={t('gameApplication.adminPanel.launchConfirmCancel')}
        confirmLabel={t('gameApplication.adminPanel.launchConfirmAction')}
      />
    </>
  )
}

export function AdminGameLaunchPanel(props: AdminGameLaunchDrawerProps) {
  const { t } = useTranslation()

  const blockersCount = useMemo(() => {
    const confirmedTeams = props.snapshot.teams.filter((team) => team.status === 'confirmed')
    const formingTeamsCount = props.snapshot.teams.filter(
      (team) => team.status === 'forming',
    ).length
    const pendingInvitationsCount = props.snapshot.teams.reduce(
      (count, team) => count + (team.pendingInvitations?.length ?? 0),
      0,
    )
    const disbandRequestsCount = props.snapshot.teams.filter(
      (team) => team.disbandRequestedAtUtc != null,
    ).length
    const invalidConfirmedRostersCount = confirmedTeams.filter(
      (team) =>
        team.members.length < props.snapshot.minPlayersPerTeam ||
        team.members.length > props.snapshot.maxPlayersPerTeam,
    ).length

    return (
      (confirmedTeams.length === 0 ? 1 : 0) +
      (formingTeamsCount > 0 ? 1 : 0) +
      (pendingInvitationsCount > 0 ? 1 : 0) +
      (disbandRequestsCount > 0 ? 1 : 0) +
      (invalidConfirmedRostersCount > 0 ? 1 : 0)
    )
  }, [props.snapshot])

  const canStartGame = blockersCount === 0

  return (
    <Box
      sx={(theme) => ({
        width: '100%',
        maxWidth: 1180,
        border: `1px solid ${alpha(
          canStartGame ? theme.palette.success.main : theme.palette.warning.main,
          0.58,
        )}`,
        background: canStartGame
          ? alpha(theme.palette.success.main, 0.12)
          : alpha(theme.palette.warning.main, 0.14),
        boxShadow: `0 12px 28px ${alpha(theme.palette.common.black, 0.24)}`,
        px: { xs: 2, sm: 2.5, md: 3 },
        py: { xs: 1.75, sm: 2 },
      })}
    >
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.5}
        alignItems={{ xs: 'stretch', md: 'center' }}
        justifyContent="space-between"
      >
        <Stack spacing={0.75} sx={{ minWidth: 0 }}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
            <Typography variant="subtitle1" fontWeight={800}>
              {t('gameApplication.adminPanel.launchPanelCardTitle')}
            </Typography>
            <Chip
              size="small"
              color={canStartGame ? 'success' : 'warning'}
              label={
                canStartGame
                  ? t('gameApplication.adminPanel.launchPanelReadyChip')
                  : t('gameApplication.adminPanel.launchPanelBlockedChip', {
                      count: blockersCount,
                    })
              }
            />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {canStartGame
              ? t('gameApplication.adminPanel.launchPanelCardReadyMessage')
              : t('gameApplication.adminPanel.launchPanelCardBlockedMessage', {
                  count: blockersCount,
                })}
          </Typography>
        </Stack>

        <Box sx={{ flexShrink: 0, alignSelf: { xs: 'flex-start', md: 'center' } }}>
          <AdminGameLaunchDrawer {...props} />
        </Box>
      </Stack>
    </Box>
  )
}
