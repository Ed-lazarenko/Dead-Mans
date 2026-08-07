import { useMutation } from '@tanstack/react-query'
import { useCallback, useMemo, useState } from 'react'
import type { ErrorResponse, GameSetupSnapshot } from '../../shared/api/contracts/index.ts'
import { API_ERROR_CODES } from '../../shared/api/errors/api-error-codes.ts'
import { ApiError } from '../../shared/api/errors/ApiError.ts'
import { saveDraftGameSetup } from './api/game-setup-api.ts'
import { buildUpdateGameSetupRequest, type GameSetupDraftState } from './model/game-setup-draft.ts'
import {
  getGameSetupDraftValidationError,
  type GameSetupDraftValidationError,
} from './model/game-setup-draft-validation.ts'
import {
  createLoadedDraftState,
  loadGameSetupDraftQueryState,
} from './model/game-setup-query-state.ts'
import type { GameSetupDraftController } from './use-game-setup-draft.ts'

export type GameSetupSyncStatus = 'idle' | 'saving' | 'saved' | 'error' | 'conflict'
export type GameSetupSaveErrorKey = GameSetupDraftValidationError | 'saveFailed'

function isStaleVersionError(error: unknown): boolean {
  if (!(error instanceof ApiError) || error.status !== 409) {
    return false
  }

  if (!error.details || typeof error.details !== 'object') {
    return false
  }

  return (error.details as ErrorResponse).code === API_ERROR_CODES.gameSetupStaleVersion
}

interface UseGameSetupSaveOptions {
  draft: GameSetupDraftState | null
  snapshot: GameSetupSnapshot | null
  snapshotDraftKey: string | null
  isDirty: boolean
  applyLoadedDraftState: GameSetupDraftController['applyLoadedDraftState']
  setDraftOverride: GameSetupDraftController['setDraftOverride']
  setRemoteChangeNotice: GameSetupDraftController['setRemoteChangeNotice']
}

export function useGameSetupSave({
  draft,
  snapshot,
  snapshotDraftKey,
  isDirty,
  applyLoadedDraftState,
  setDraftOverride,
  setRemoteChangeNotice,
}: UseGameSetupSaveOptions) {
  const [syncStatus, setSyncStatus] = useState<GameSetupSyncStatus>('saved')
  const [saveErrorMessage, setSaveErrorMessage] = useState<GameSetupSaveErrorKey | null>(null)

  const handleSaveConflict = useCallback(async () => {
    const loaded = await loadGameSetupDraftQueryState()
    applyLoadedDraftState(loaded)
    setSyncStatus('conflict')
    setRemoteChangeNotice(true)
  }, [applyLoadedDraftState, setRemoteChangeNotice])

  const { mutateAsync: saveDraftAsync, isPending: isSaving } = useMutation({
    mutationFn: async ({
      draftToSave,
      expectedVersion,
    }: {
      draftToSave: GameSetupDraftState
      expectedVersion: number
    }) => saveDraftGameSetup(buildUpdateGameSetupRequest(draftToSave, expectedVersion)),
    onMutate: () => {
      setSyncStatus('saving')
      setSaveErrorMessage(null)
    },
    onSuccess: (nextSnapshot) => {
      applyLoadedDraftState(createLoadedDraftState(nextSnapshot))
      setSyncStatus('saved')
      setRemoteChangeNotice(false)
    },
    onError: async (error) => {
      if (isStaleVersionError(error)) {
        await handleSaveConflict()
        return
      }

      if (error instanceof ApiError && error.status === 404) {
        applyLoadedDraftState(createLoadedDraftState(null))
        setSyncStatus('idle')
        return
      }

      if (error instanceof ApiError && error.details && typeof error.details === 'object') {
        const payload = error.details as ErrorResponse
        if (payload.code === API_ERROR_CODES.invalidGameSetupTitle) {
          setSaveErrorMessage('invalidTitle')
          setSyncStatus('error')
          return
        }
      }

      setSaveErrorMessage('saveFailed')
      setSyncStatus('error')
    },
  })

  const saveDraft = useCallback(async () => {
    if (!draft || !snapshot || !isDirty) {
      return
    }

    const validationError = getGameSetupDraftValidationError(draft)
    if (validationError) {
      setSaveErrorMessage(validationError)
      setSyncStatus('error')
      return
    }

    await saveDraftAsync({
      draftToSave: draft,
      expectedVersion: snapshot.version,
    })
  }, [draft, isDirty, saveDraftAsync, snapshot])

  const saveDraftWithLayout = useCallback(
    async (nextDraft: GameSetupDraftState) => {
      if (!snapshot) {
        return
      }

      const validationError = getGameSetupDraftValidationError(nextDraft)
      if (validationError) {
        setSaveErrorMessage(validationError)
        setSyncStatus('error')
        return
      }

      await saveDraftAsync({
        draftToSave: nextDraft,
        expectedVersion: snapshot.version,
      })
    },
    [saveDraftAsync, snapshot],
  )

  const flushDraftSave = useCallback(async () => {
    await saveDraft()
  }, [saveDraft])

  const applyLayoutChange = (updater: (current: GameSetupDraftState) => GameSetupDraftState) => {
    if (!draft || !snapshot || !snapshotDraftKey) {
      return
    }

    const previousDraft = draft
    const nextDraft = updater(draft)
    setDraftOverride({
      key: snapshotDraftKey,
      draft: nextDraft,
    })
    void saveDraftWithLayout(nextDraft).catch((error) => {
      if (isStaleVersionError(error)) {
        return
      }

      if (error instanceof ApiError && error.status === 404) {
        return
      }

      setDraftOverride({
        key: snapshotDraftKey,
        draft: previousDraft,
      })
    })
  }

  const resolvedSyncStatus = useMemo((): GameSetupSyncStatus => {
    if (syncStatus === 'saving' || syncStatus === 'error' || syncStatus === 'conflict') {
      return syncStatus
    }

    if (isDirty) {
      return syncStatus === 'saved' ? 'idle' : syncStatus
    }

    return 'saved'
  }, [isDirty, syncStatus])

  const handleDraftEdited = useCallback(() => {
    setSaveErrorMessage(null)
    setSyncStatus((current) => (current === 'saved' ? 'idle' : current))
  }, [])

  const resetToSaved = useCallback(() => {
    setSaveErrorMessage(null)
    setSyncStatus('saved')
  }, [])

  const resetToIdle = useCallback(() => {
    setSaveErrorMessage(null)
    setSyncStatus('idle')
  }, [])

  return {
    syncStatus: resolvedSyncStatus,
    saveErrorMessage,
    isSaving,
    saveDraft,
    applyLayoutChange,
    flushDraftSave,
    handleDraftEdited,
    resetToSaved,
    resetToIdle,
  }
}
