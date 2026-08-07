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

function getParticipantInitials(displayName: string) {
  return displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

export function TeamQueuePanel({ teams, isLoading, isError, activeTeamId }: TeamQueuePanelProps) {
  const { t } = useTranslation()
  const [isOpen, setIsOpen] = useState(false)
  const activeTeam = teams.find((team) => team.teamId === activeTeamId) ?? null
  const playedTeamsCount = teams.filter((team) => team.isPlayed).length

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
          {activeTeam ? (
            <Box
              sx={(theme) => ({
                minWidth: 30,
                minHeight: 30,
                px: 0.4,
                borderRadius: '999px',
                display: 'grid',
                placeItems: 'center',
                border: `1px solid ${alpha(theme.palette.warning.main, 0.48)}`,
                backgroundColor: alpha(theme.palette.warning.main, 0.16),
                typography: 'caption',
                fontWeight: 900,
              })}
            >
              #{activeTeam.teamSlotIndex}
            </Box>
          ) : null}
          <Typography
            component="span"
            variant="caption"
            fontWeight={800}
            sx={{
              writingMode: 'vertical-rl',
              transform: 'rotate(180deg)',
              letterSpacing: '0.12em',
            }}
          >
            {t('gameBoard.teamQueueTabLabel')}
          </Typography>
          {teams.length > 0 ? (
            <Chip
              size="small"
              label={teams.length}
              sx={(theme) => ({
                height: 22,
                backgroundColor: alpha(theme.palette.common.black, 0.2),
                '& .MuiChip-label': {
                  px: 0.8,
                  fontWeight: 800,
                },
              })}
            />
          ) : null}
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
            <Stack spacing={0.75} sx={{ minWidth: 0 }}>
              <Typography variant="h6">{t('gameBoard.teamQueueTitle')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {t('gameBoard.teamQueueDescription')}
              </Typography>
              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap sx={{ pt: 0.25 }}>
                <Chip size="small" label={teams.length} />
                {activeTeam ? (
                  <Chip
                    size="small"
                    color="warning"
                    variant="filled"
                    label={`#${activeTeam.teamSlotIndex}`}
                  />
                ) : null}
                {playedTeamsCount > 0 ? (
                  <Chip
                    size="small"
                    color="success"
                    variant="outlined"
                    label={t('gameBoard.teamQueuePlayedChip')}
                  />
                ) : null}
              </Stack>
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
                const isPlayed = team.isPlayed ?? false

                return (
                  <Box
                    key={team.teamId}
                    sx={(theme) => ({
                      position: 'relative',
                      overflow: 'hidden',
                      borderRadius: 3,
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
                          <Stack
                            direction="row"
                            spacing={0.8}
                            alignItems="center"
                            flexWrap="wrap"
                            useFlexGap
                          >
                            <Typography variant="subtitle2" fontWeight={900}>
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
                            {isPlayed ? (
                              <Chip
                                size="small"
                                color="success"
                                variant={isActive ? 'filled' : 'outlined'}
                                label={t('gameBoard.teamQueuePlayedChip')}
                              />
                            ) : null}
                          </Stack>

                          <Stack direction="row" spacing={0.55} flexWrap="wrap" useFlexGap>
                            {team.participants.map((participant) => (
                              <Box
                                key={participant.userId}
                                sx={(theme) => ({
                                  display: 'grid',
                                  placeItems: 'center',
                                  width: 24,
                                  height: 24,
                                  borderRadius: '50%',
                                  border: `1px solid ${alpha(theme.palette.common.white, 0.12)}`,
                                  backgroundColor: alpha(
                                    isActive
                                      ? theme.palette.warning.main
                                      : isPlayed
                                        ? theme.palette.success.main
                                        : theme.palette.info.main,
                                    0.22,
                                  ),
                                  typography: 'caption',
                                  fontWeight: 900,
                                })}
                                title={participant.displayName}
                              >
                                {getParticipantInitials(participant.displayName)}
                              </Box>
                            ))}
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
                              gap: 10,
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
                            <Typography
                              variant="body2"
                              noWrap
                              title={participant.displayName}
                              fontWeight={600}
                            >
                              {participant.displayName}
                            </Typography>
                          </Box>
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
