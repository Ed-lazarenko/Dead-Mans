import { Chip, Stack, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import type { RegistrationTeam } from '../../../shared/api/contracts/index.ts'
import { AppButton, SectionCard } from '../../../shared/ui/index.ts'
import { TeamSummary } from './TeamSummary.tsx'

interface OpenTeamsSectionProps {
  teams: RegistrationTeam[]
  canJoinTeams: boolean
  onJoin: (teamId: string) => void
  joiningTeamId: string | undefined
}

export function OpenTeamsSection({
  teams,
  canJoinTeams,
  onJoin,
  joiningTeamId,
}: OpenTeamsSectionProps) {
  const { t } = useTranslation()
  const sortedTeams = [...teams].sort((left, right) => left.slotIndex - right.slotIndex)
  const joinableTeamsCount = sortedTeams.filter(
    (team) => team.status === 'forming' && team.recruitmentOpen,
  ).length

  return (
    <SectionCard sx={{ height: '100%' }}>
      <Stack spacing={2.5}>
        <Stack spacing={1}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
            <Typography variant="subtitle1">{t('gameApplication.createdTeamsTitle')}</Typography>
            <Chip
              size="small"
              label={t('gameApplication.teamsCountChip', { count: teams.length })}
            />
            <Chip
              size="small"
              color={joinableTeamsCount > 0 ? 'success' : 'default'}
              label={t('gameApplication.joinableTeamsChip', { count: joinableTeamsCount })}
            />
          </Stack>
          <Typography variant="body2" color="text.secondary">
            {canJoinTeams
              ? t('gameApplication.createdTeamsDescription')
              : t('gameApplication.createdTeamsReadOnlyDescription')}
          </Typography>
        </Stack>

        <Stack spacing={1.5}>
          {sortedTeams.length === 0 ? (
            <SectionCard inset variantStyle="dashed">
              <Typography variant="body2" color="text.secondary">
                {t('gameApplication.noCreatedTeams')}
              </Typography>
            </SectionCard>
          ) : (
            sortedTeams.map((team) => {
              const canJoinTeam = canJoinTeams && team.status === 'forming' && team.recruitmentOpen

              return (
                <SectionCard
                  key={team.teamId}
                  inset
                  sx={{
                    display: 'flex',
                    flexDirection: { xs: 'column', md: 'row' },
                    gap: 1.5,
                    alignItems: { xs: 'stretch', md: 'center' },
                    justifyContent: 'space-between',
                  }}
                >
                  <TeamSummary team={team} />
                  {canJoinTeam ? (
                    <AppButton
                      fullWidth={false}
                      disabled={joiningTeamId === team.teamId}
                      onClick={() => onJoin(team.teamId)}
                      sx={{ alignSelf: { xs: 'stretch', md: 'center' }, minWidth: 132 }}
                    >
                      {t('gameApplication.joinTeam')}
                    </AppButton>
                  ) : (
                    <Chip
                      size="small"
                      label={
                        team.recruitmentOpen && team.status !== 'forming'
                          ? t('gameApplication.joinUnavailableChip')
                          : t('gameApplication.joinNotAvailableChip')
                      }
                      sx={{ alignSelf: { xs: 'flex-start', md: 'center' } }}
                    />
                  )}
                </SectionCard>
              )
            })
          )}
        </Stack>
      </Stack>
    </SectionCard>
  )
}
