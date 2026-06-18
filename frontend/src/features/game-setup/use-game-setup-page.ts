import type { GameSetupDraftState } from './model/game-setup-draft.ts'
import { useGameSetupCellMedia } from './use-game-setup-cell-media.ts'
import { useGameSetupDraft } from './use-game-setup-draft.ts'
import { useGameSetupSave } from './use-game-setup-save.ts'

/**
 * Composition root for the admin game-setup page. It wires the three focused
 * orchestration hooks — draft state ({@link useGameSetupDraft}), persistence and
 * conflicts ({@link useGameSetupSave}), and cell media ({@link useGameSetupCellMedia})
 * — into the single surface the page renders against. Draft lifecycle actions
 * are wrapped here so they also reconcile save status.
 */
export function useGameSetupPage() {
  const draft = useGameSetupDraft()
  const save = useGameSetupSave({
    draft: draft.draft,
    snapshot: draft.snapshot,
    snapshotDraftKey: draft.snapshotDraftKey,
    isDirty: draft.isDirty,
    applyLoadedDraftState: draft.applyLoadedDraftState,
    setDraftOverride: draft.setDraftOverride,
    setRemoteChangeNotice: draft.setRemoteChangeNotice,
  })
  const cellMedia = useGameSetupCellMedia(draft.snapshot, {
    flushDraftSave: save.flushDraftSave,
  })

  const updateDraft = (updater: (current: GameSetupDraftState) => GameSetupDraftState) => {
    draft.updateDraft(updater)
    save.handleDraftEdited()
  }

  const createDraft: typeof draft.createDraft = async (variables, options) => {
    const result = await draft.createDraft(variables, options)
    save.resetToSaved()
    return result
  }

  const deleteDraft = async () => {
    await draft.deleteDraft()
    save.resetToIdle()
  }

  const reloadFromServer = async () => {
    await draft.reloadFromServer()
    save.resetToSaved()
  }

  const toggleModifier = (modifierCode: string, enabled: boolean) => {
    updateDraft((current) => {
      const currentCodes = current.enabledModifierCodes
      const nextCodes = enabled
        ? currentCodes.includes(modifierCode)
          ? currentCodes
          : [...currentCodes, modifierCode]
        : currentCodes.filter((code) => code !== modifierCode)

      return {
        ...current,
        enabledModifierCodes: nextCodes,
      }
    })
  }

  const toggleQuestion = (questionId: string, enabled: boolean) => {
    updateDraft((current) => {
      const currentIds = current.enabledQuestionIds
      const nextIds = enabled
        ? currentIds.includes(questionId)
          ? currentIds
          : [...currentIds, questionId]
        : currentIds.filter((id) => id !== questionId)

      return {
        ...current,
        enabledQuestionIds: nextIds,
      }
    })
  }

  const setQuestionSelection = (questionIds: readonly string[], enabled: boolean) => {
    updateDraft((current) => {
      if (enabled) {
        const merged = new Set([...current.enabledQuestionIds, ...questionIds])
        return { ...current, enabledQuestionIds: [...merged] }
      }

      const removed = new Set(questionIds)
      return {
        ...current,
        enabledQuestionIds: current.enabledQuestionIds.filter((id) => !removed.has(id)),
      }
    })
  }

  return {
    snapshot: draft.snapshot,
    draft: draft.draft,
    isLoading: draft.isLoading,
    isError: draft.isError,
    isEmpty: draft.isEmpty,
    isDirty: draft.isDirty,
    syncStatus: save.syncStatus,
    remoteChangeNotice: draft.remoteChangeNotice,
    draftRemovedNotice: draft.draftRemovedNotice,
    saveErrorMessage: save.saveErrorMessage,
    resetErrorMessage: draft.resetErrorMessage,
    updateDraft,
    applyLayoutChange: save.applyLayoutChange,
    saveDraft: save.saveDraft,
    reloadFromServer,
    createDraft,
    deleteDraft,
    toggleModifier,
    toggleQuestion,
    setQuestionSelection,
    isCreating: draft.isCreating,
    isResetting: draft.isResetting,
    isSaving: save.isSaving,
    dismissRemoteChangeNotice: draft.dismissRemoteChangeNotice,
    dismissDraftRemovedNotice: draft.dismissDraftRemovedNotice,
    ...cellMedia,
  }
}
