import { gameBoardApi } from '../../../shared/api/game-board.ts'
import { ApiError } from '../../../shared/api/errors/ApiError.ts'
import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'

/**
 * Loads the current DB-backed board. `404` means no active or finished game — not a transport error.
 */
export async function fetchCurrentGameBoardSnapshot(): Promise<GameBoardSnapshot | null> {
  try {
    return await gameBoardApi.getCurrentSnapshot()
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null
    }
    throw error
  }
}
