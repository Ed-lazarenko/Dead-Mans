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
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameTeamQueueItem } from '../../../shared/api/contracts/index.ts'
import { SectionCard } from '../../../shared/ui/index.ts'

interface TeamQueuePanelProps {
  teams: readonly GameTeamQueueItem[]
  isLoading: boolean
  isError: boolean
  activeTeamId?: string | null
}

export function TeamQueuePanel({ teams, isLoading, isError, activeTeamId }: TeamQueuePanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)

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
          minHeight: 112,
          width: 38,
          borderRadius: `0 ${theme.shape.borderRadius}px ${theme.shape.borderRadius}px 0`,
          border: `1px solid ${alpha(theme.palette.primary.main, 0.42)}`,
          borderLeft: 0,
          backgroundColor: alpha(theme.palette.background.paper, 0.96),
          boxShadow: `0 10px 26px ${alpha(theme.palette.common.black, 0.28)}`,
          color: theme.palette.text.primary,
          '&:hover': {
            backgroundColor: alpha(theme.palette.primary.main, 0.14),
          },
          '&:focus-visible': {
            outline: `2px solid ${theme.palette.primary.main}`,
            outlineOffset: 2,
          },
        })}
      >
        <Stack spacing={0.75} alignItems="center">
          <Typography component="span" aria-hidden variant="body2" fontWeight={900}>
            &gt;
          </Typography>
          <Typography
            component="span"
            variant="caption"
            fontWeight={800}
            sx={{ writingMode: 'vertical-rl', transform: 'rotate(180deg)' }}
          >
            {t('gameBoard.teamQueueTabLabel')}
          </Typography>
          {teams.length > 0 ? <Chip size="small" label={teams.length} sx={{ height: 20 }} /> : null}
        </Stack>
      </ButtonBase>

      <Drawer
        anchor="left"
        open={isOpen}
        onClose={() => setIsOpen(false)}
        ModalProps={{ keepMounted: true }}
        PaperProps={{
          sx: {
            width: { xs: 'calc(100vw - 48px)', sm: 340 },
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
          >
            <Stack spacing={0.75} sx={{ minWidth: 0 }}>
              <Typography variant="h6">{t('gameBoard.teamQueueTitle')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.teamQueueDescription')}
              </Typography>
            </Stack>
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
            <Stack spacing={1.25}>
              {teams.map((team) => {
                const isActive = team.teamId === activeTeamId

                return (
                  <Box
                    key={team.teamId}
                    sx={(theme) => ({
                      border: `1px solid ${alpha(
                        isActive ? theme.palette.warning.main : theme.palette.divider,
                        isActive ? 0.7 : 0.9,
                      )}`,
                      backgroundColor: isActive
                        ? alpha(theme.palette.warning.main, 0.12)
                        : alpha(theme.palette.background.default, 0.32),
                      px: 1.25,
                      py: 1.1,
                    })}
                  >
                    <Stack spacing={1}>
                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                        <Typography variant="subtitle2" fontWeight={800}>
                          {t('gameBoard.teamQueueTeamTitle', {
                            slot: team.teamSlotIndex,
                          })}
                        </Typography>
                        {isActive ? (
                          <Chip
                            size="small"
                            color="warning"
                            label={t('gameBoard.teamQueueActiveChip')}
                          />
                        ) : null}
                      </Stack>

                      <Stack spacing={0.65}>
                        {team.participants.map((participant, index) => (
                          <Stack
                            key={participant.userId}
                            direction="row"
                            spacing={0.75}
                            alignItems="center"
                            sx={{ minWidth: 0 }}
                          >
                            <Typography
                              variant="caption"
                              color="text.secondary"
                              sx={{ width: 18, flexShrink: 0, textAlign: 'right' }}
                            >
                              {index + 1}.
                            </Typography>
                            <Typography variant="body2" noWrap title={participant.displayName}>
                              {participant.displayName}
                            </Typography>
                          </Stack>
                        ))}
                      </Stack>
                    </Stack>
                  </Box>
                )
              })}
            </Stack>
          )}
        </SectionCard>
      </Drawer>
    </>
  )
}
