import type { GameBoardCellId, GameTeamQueueResult } from '../../../shared/api/contracts/index.ts'
import {
  createApiClient,
  ensureOpenApiSuccess,
  unwrapOpenApiData,
  unwrapOpenApiDataOrNullOn404,
} from '../../../shared/api/client/openApiClient.ts'
import type { paths } from '../../../shared/api/contracts/generated'

const gameBoardApiClient =
  createApiClient<
    Pick<
      paths,
      | '/game'
      | '/game/team-queue'
      | '/game/active-team'
      | '/game/teams/{teamId}/played-state'
      | '/game/cells/{cellId}/open'
    >
  >()

export async function fetchCurrentGameBoardSnapshot() {
  return unwrapOpenApiDataOrNullOn404(gameBoardApiClient.GET('/game'))
}

export async function fetchCurrentGameTeamQueue(): Promise<GameTeamQueueResult> {
  return unwrapOpenApiData(gameBoardApiClient.GET('/game/team-queue'))
}

export async function setActiveGameTeam(teamId: string | null): Promise<void> {
  await ensureOpenApiSuccess(
    gameBoardApiClient.PUT('/game/active-team', {
      body: { teamId },
    }),
  )
}

export async function setGameTeamPlayedState(teamId: string, isPlayed: boolean): Promise<void> {
  await ensureOpenApiSuccess(
    gameBoardApiClient.PUT('/game/teams/{teamId}/played-state', {
      params: {
        path: { teamId },
      },
      body: { isPlayed },
    }),
  )
}

export async function openGameBoardCell(cellId: GameBoardCellId): Promise<void> {
  await ensureOpenApiSuccess(
    gameBoardApiClient.POST('/game/cells/{cellId}/open', {
      params: {
        path: { cellId },
      },
    }),
  )
}
