import { CircularProgress, Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { useTranslation } from 'react-i18next'
import { useLeaderboardPage } from './useLeaderboardPage.ts'

export function LeaderboardPage() {
  const { t } = useTranslation()
  const { data, isLoading, isError } = useLeaderboardPage()

  if (isLoading) {
    return (
      <Paper sx={{ p: 3, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Paper>
    )
  }

  if (isError || !data) {
    return (
      <Paper sx={{ p: 3 }}>
        <Typography color="error">{t('leaderboard.errorLoading')}</Typography>
      </Paper>
    )
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h5" gutterBottom>
        {t('nav.leaderboard')}
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>
        {t('leaderboard.mockUpdatedAt', {
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
          {data.teams.map((team, index) => {
            const total = team.score - team.penalty
            return (
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
                <TableCell align="right">{total}</TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>
    </Paper>
  )
}

