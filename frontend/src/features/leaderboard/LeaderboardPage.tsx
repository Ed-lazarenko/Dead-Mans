import { Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { PageStatePanel } from '../../shared/ui/PageStatePanel.tsx'
import { useLeaderboardPage } from './useLeaderboardPage.ts'

export function LeaderboardPage() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useLeaderboardPage()

  if (isLoading) {
    return <PageStatePanel title={t('nav.leaderboard')} message={t('leaderboard.loading')} showSpinner />
  }

  if (isError || !data) {
    return (
      <PageStatePanel
        title={t('nav.leaderboard')}
        message={t('leaderboard.errorLoading')}
        tone="error"
      />
    )
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        {t('nav.leaderboard')}
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        {t('leaderboard.updatedAt', {
          time: new Date(data.updatedAt).toLocaleTimeString(),
        })}
      </Typography>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>{t('leaderboard.columns.position')}</TableCell>
            <TableCell>{t('leaderboard.columns.team')}</TableCell>
            <TableCell align="right">{t('leaderboard.columns.score')}</TableCell>
            <TableCell align="right">{t('leaderboard.columns.penalty')}</TableCell>
            <TableCell align="right">{t('leaderboard.columns.total')}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {data.teams.map((team, index) => (
            <TableRow key={team.id}>
              <TableCell>{index + 1}</TableCell>
              <TableCell>
                <span
                  style={{
                    display: 'inline-block',
                    width: 10,
                    height: 10,
                    borderRadius: '50%',
                    backgroundColor: team.colorHex,
                    marginRight: 8,
                  }}
                />
                {team.name}
              </TableCell>
              <TableCell align="right">{team.score}</TableCell>
              <TableCell align="right">{team.penalty}</TableCell>
              <TableCell align="right">{team.total}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Paper>
  )
}

