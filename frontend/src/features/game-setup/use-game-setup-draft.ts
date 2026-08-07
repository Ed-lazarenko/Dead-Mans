import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { GameSetupSnapshot } from '../../shared/api/contracts/index.ts'
import { createDraftGameSetup, deleteDraftGameSetup } from './api/game-setup-api.ts'
import { gameSetupDraftQueryOptions } from './api/game-setup-queries.ts'
import { isGameSetupDraftDirty, type GameSetupDraftState } from './model/game-setup-draft.ts'
import {
  createLoadedDraftState,
  getSnapshotDraftKey,
  loadGameSetupDraftQueryState,
  type LoadedGameSetupDraftState,
} from './model/game-setup-query-state.ts'

export type GameSetupResetErrorKey = 'resetFailed'

interface DraftOverride {
  key: string
  draft: GameSetupDraftState
}

export function useGameSetupDraft() {
  const queryClient = useQueryClient()
  const [draftOverride, setDraftOverride] = useState<DraftOverride | null>(null)
  const [remoteChangeNotice, setRemoteChangeNotice] = useState(false)
  const [draftRemovedNotice, setDraftRemovedNotice] = useState(false)
  const [resetErrorMessage, setResetErrorMessage] = useState<GameSetupResetErrorKey | null>(null)
  const lastSyncedVersionRef = useRef<number | null>(null)
  const hadSnapshotRef = useRef(false)

  const draftQuery = useQuery(gameSetupDraftQueryOptions)

  const snapshot: GameSetupSnapshot | null = draftQuery.data?.snapshot ?? null
  const savedDraft = draftQuery.data?.savedDraft ?? null
  const snapshotDraftKey = snapshot ? getSnapshotDraftKey(snapshot) : null
  const activeDraftOverride =
    snapshotDraftKey && draftOverride?.key === snapshotDraftKey ? draftOverride : null
  const draft = activeDraftOverride?.draft ?? draftQuery.data?.initialDraft ?? null

  const isDirty = useMemo(() => {
    if (!savedDraft || !draft) {
      return false
    }

    return isGameSetupDraftDirty(savedDraft, draft)
  }, [savedDraft, draft])

  const applyLoadedDraftState = useCallback(
    (loaded: LoadedGameSetupDraftState) => {
      const previous = queryClient.getQueryData<LoadedGameSetupDraftState>(
        gameSetupDraftQueryOptions.queryKey,
      )
      if (previous?.snapshot && !loaded.snapshot) {
        setDraftRemovedNotice(true)
      }

      setDraftOverride(null)
      queryClient.setQueryData(gameSetupDraftQueryOptions.queryKey, loaded)
      lastSyncedVersionRef.current = loaded.snapshot?.version ?? null
    },
    [queryClient],
  )

  useEffect(() => {
    const hadSnapshot = hadSnapshotRef.current
    hadSnapshotRef.current = snapshot !== null

    if (!snapshot) {
      if (hadSnapshot && draftOverride !== null) {
        queueMicrotask(() => setDraftRemovedNotice(true))
      }

      lastSyncedVersionRef.current = null
      queueMicrotask(() => setDraftOverride(null))
      return
    }

    const previousVersion = lastSyncedVersionRef.current
    lastSyncedVersionRef.current = snapshot.version

    if (previousVersion === null || previousVersion === snapshot.version) {
      return
    }

    if (isDirty) {
      queueMicrotask(() => setRemoteChangeNotice(true))
      return
    }

    queueMicrotask(() => setDraftOverride(null))
    queueMicrotask(() => setRemoteChangeNotice(false))
  }, [draftOverride, isDirty, snapshot])

  const updateDraft = (updater: (current: GameSetupDraftState) => GameSetupDraftState) => {
    if (!snapshotDraftKey || !draft) {
      return
    }

    setDraftOverride({
      key: snapshotDraftKey,
      draft: updater(draft),
    })
    setResetErrorMessage(null)
    setRemoteChangeNotice(false)
  }

  const createDraftMutation = useMutation({
    mutationFn: createDraftGameSetup,
    onSuccess: (nextSnapshot) => {
      applyLoadedDraftState(createLoadedDraftState(nextSnapshot))
      setResetErrorMessage(null)
      setDraftRemovedNotice(false)
    },
  })

  const deleteDraftMutation = useMutation({
    mutationFn: deleteDraftGameSetup,
    onSuccess: () => {
      applyLoadedDraftState(createLoadedDraftState(null))
      setDraftRemovedNotice(false)
      setResetErrorMessage(null)
    },
    onError: () => {
      setResetErrorMessage('resetFailed')
    },
  })

  const deleteDraft = async () => {
    await deleteDraftMutation.mutateAsync()
  }

  const reloadFromServer = useCallback(async () => {
    const loaded = await loadGameSetupDraftQueryState()
    applyLoadedDraftState(loaded)
    if (loaded.snapshot) {
      setDraftRemovedNotice(false)
    }
    setRemoteChangeNotice(false)
  }, [applyLoadedDraftState])

  return {
    snapshot,
    draft,
    snapshotDraftKey,
    isLoading: draftQuery.isLoading,
    isError: draftQuery.isError,
    isEmpty: draftQuery.data?.snapshot === null,
    isDirty,
    remoteChangeNotice,
    draftRemovedNotice,
    resetErrorMessage,
    isCreating: createDraftMutation.isPending,
    isResetting: deleteDraftMutation.isPending,
    applyLoadedDraftState,
    setDraftOverride,
    setRemoteChangeNotice,
    updateDraft,
    createDraft: createDraftMutation.mutateAsync,
    deleteDraft,
    reloadFromServer,
    dismissRemoteChangeNotice: () => setRemoteChangeNotice(false),
    dismissDraftRemovedNotice: () => setDraftRemovedNotice(false),
  }
}

export type GameSetupDraftController = ReturnType<typeof useGameSetupDraft>
