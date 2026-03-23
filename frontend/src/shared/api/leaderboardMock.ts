import type { LeaderboardSummary, LeaderboardTeam, TeamId } from './types.ts'

// TODO: заменить mock‑реализацию на HTTP‑клиент к backend /api/leaderboard,
// сохранив те же интерфейсы функций для фронта.

let teams: LeaderboardTeam[] = [
  { id: 't1', name: 'Team Alpha', colorHex: '#ff7043', score: 120, penalty: 0 },
  { id: 't2', name: 'Team Bravo', colorHex: '#42a5f5', score: 95, penalty: 10 },
  { id: 't3', name: 'Team Charlie', colorHex: '#66bb6a', score: 80, penalty: 0 },
]

function sortTeams(data: LeaderboardTeam[]): LeaderboardTeam[] {
  return [...data].sort((a, b) => {
    const aTotal = a.score - a.penalty
    const bTotal = b.score - b.penalty
    return bTotal - aTotal
  })
}

export async function getLeaderboard(): Promise<LeaderboardSummary> {
  // небольшая задержка, как будто идёт запрос к серверу
  await new Promise((resolve) => setTimeout(resolve, 200))

  return {
    updatedAt: new Date().toISOString(),
    teams: sortTeams(teams),
  }
}

export async function updateTeamScore(params: {
  teamId: TeamId
  scoreDelta?: number
  penaltyDelta?: number
}): Promise<LeaderboardSummary> {
  const { teamId, scoreDelta = 0, penaltyDelta = 0 } = params

  teams = teams.map((team) =>
    team.id === teamId
      ? {
          ...team,
          score: team.score + scoreDelta,
          penalty: Math.max(0, team.penalty + penaltyDelta),
        }
      : team,
  )

  return getLeaderboard()
}


