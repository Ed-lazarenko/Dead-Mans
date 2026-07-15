import type { LoadedGameSetupDraftState } from '../model/game-setup-query-state.ts'

export function selectNewerGameSetupState(
  current: LoadedGameSetupDraftState | undefined,
  incoming: LoadedGameSetupDraftState,
): LoadedGameSetupDraftState {
  if (current && incoming.requestId < current.requestId) {
    return current
  }

  if (!current?.snapshot || !incoming.snapshot) {
    return incoming
  }

  return incoming.snapshot.version >= current.snapshot.version ? incoming : current
}
