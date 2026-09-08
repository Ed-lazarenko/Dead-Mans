import type { GameSetupDraftState } from './game-setup-draft.ts'
import {
  GAME_SETUP_MAX_CELL_TITLE_LENGTH,
  GAME_SETUP_MAX_COLUMN_LABEL_LENGTH,
  GAME_SETUP_MAX_ROW_LABEL_LENGTH,
  GAME_SETUP_MAX_TITLE_LENGTH,
} from './game-setup-limits.ts'

export type GameSetupDraftValidationError =
  'invalidTitle' | 'invalidRowLabel' | 'invalidColumnLabel' | 'invalidCellTitle'

export function getGameSetupDraftValidationError(
  draft: GameSetupDraftState,
): GameSetupDraftValidationError | null {
  const title = draft.title.trim()
  if (title.length === 0 || title.length > GAME_SETUP_MAX_TITLE_LENGTH) {
    return 'invalidTitle'
  }

  for (const label of draft.rowLabels) {
    const trimmed = label.trim()
    if (trimmed.length === 0 || trimmed.length > GAME_SETUP_MAX_ROW_LABEL_LENGTH) {
      return 'invalidRowLabel'
    }
  }

  for (const label of draft.colLabels) {
    const trimmed = label.trim()
    if (trimmed.length === 0 || trimmed.length > GAME_SETUP_MAX_COLUMN_LABEL_LENGTH) {
      return 'invalidColumnLabel'
    }
  }

  for (const cell of draft.cells) {
    const trimmedTitle = cell.title.trim()
    if (trimmedTitle.length > GAME_SETUP_MAX_CELL_TITLE_LENGTH) {
      return 'invalidCellTitle'
    }
  }

  return null
}
