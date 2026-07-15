import type { GameSetupSnapshot } from '../../../shared/api/contracts/index.ts'
import { fetchDraftGameSetupSnapshot } from '../api/game-setup-api.ts'
import { createDraftFromSnapshot, type GameSetupDraftState } from './game-setup-draft.ts'

export interface LoadedGameSetupDraftState {
  requestId: number
  snapshot: GameSetupSnapshot | null
  savedDraft: GameSetupDraftState | null
  initialDraft: GameSetupDraftState | null
}

export function getSnapshotDraftKey(snapshot: GameSetupSnapshot): string {
  return snapshot.gameId
}

export function createLoadedDraftState(
  snapshot: GameSetupSnapshot | null,
  requestId = 0,
): LoadedGameSetupDraftState {
  if (snapshot === null) {
    return {
      requestId,
      snapshot: null,
      savedDraft: null,
      initialDraft: null,
    }
  }

  const serverDraft = createDraftFromSnapshot(snapshot)
  return {
    requestId,
    snapshot,
    savedDraft: serverDraft,
    initialDraft: serverDraft,
  }
}

export async function loadGameSetupDraftQueryState(
  requestId = 0,
): Promise<LoadedGameSetupDraftState> {
  return createLoadedDraftState(await fetchDraftGameSetupSnapshot(), requestId)
}
