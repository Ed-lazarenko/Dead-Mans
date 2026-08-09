import { Box, Chip, Divider, Drawer, IconButton, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameRegistrationAdminSnapshot } from '../../../shared/api/contracts/index.ts'
import { AppButton, ConfirmDialog, SectionCard } from '../../../shared/ui/index.ts'

interface AdminGameLaunchDrawerProps {
  snapshot: GameRegistrationAdminSnapshot
  isStartingGame: boolean
  onStartGame: () => void
}

export function AdminGameLaunchDrawer({
  snapshot,
  isStartingGame,
  onStartGame,
}: AdminGameLaunchDrawerProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const [isConfirmOpen, setIsConfirmOpen] = useState(false)
  const launchSummary = snapshot.launchSummary

  const blockers = buildLaunchBlockers(t, launchSummary)
  const canStartGame = launchSummary.canStartGame

  return (
    <>
      <AppButton
        size="small"
        aria-label={t('gameApplication.adminPanel.launchPanelOpen')}
        onClick={() => setIsOpen(true)}
        sx={{ minHeight: 40, width: '100%', justifyContent: 'space-between' }}
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
                    count: blockers.length,
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
                      count: blockers.length,
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
                    count: launchSummary.confirmedTeamsCount,
                  })}
                </Typography>
                <Typography variant="body2">
                  {t('gameApplication.adminPanel.launchPanelPendingInvitations', {
                    count: launchSummary.pendingInvitationsCount,
                  })}
                </Typography>
                <Typography variant="body2">
                  {t('gameApplication.adminPanel.launchPanelDisbandRequests', {
                    count: launchSummary.disbandRequestsCount,
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
                  {blockers.map((blocker) => (
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

function buildLaunchBlockers(
  t: ReturnType<typeof useTranslation>['t'],
  summary: GameRegistrationAdminSnapshot['launchSummary'],
) {
  const blockers: string[] = []

  if (summary.confirmedTeamsCount === 0) {
    blockers.push(t('gameApplication.adminPanel.launchBlockerNoConfirmedTeams'))
  }

  if (summary.formingTeamsCount > 0) {
    blockers.push(
      t('gameApplication.adminPanel.launchBlockerUnconfirmedTeams', {
        count: summary.formingTeamsCount,
      }),
    )
  }

  if (summary.pendingInvitationsCount > 0) {
    blockers.push(
      t('gameApplication.adminPanel.launchBlockerPendingInvitations', {
        count: summary.pendingInvitationsCount,
      }),
    )
  }

  if (summary.disbandRequestsCount > 0) {
    blockers.push(
      t('gameApplication.adminPanel.launchBlockerDisbandRequests', {
        count: summary.disbandRequestsCount,
      }),
    )
  }

  if (summary.invalidConfirmedRostersCount > 0) {
    blockers.push(
      t('gameApplication.adminPanel.launchBlockerInvalidRosters', {
        count: summary.invalidConfirmedRostersCount,
      }),
    )
  }

  return blockers
}
