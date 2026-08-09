import type { GameBoardCell } from '../../../shared/api/contracts/index.ts'
import { PlayedCardPreviewDialog } from '../../../shared/ui/index.ts'
import type { GameBoardCardPlayResultRound } from '../use-card-play-result.ts'

interface GameBoardCardPreviewDialogProps {
  cell: GameBoardCell | null
  playResult: {
    round: GameBoardCardPlayResultRound | null
    isLoading: boolean
    isError: boolean
  }
  onClose: () => void
}

export function GameBoardCardPreviewDialog({
  cell,
  playResult,
  onClose,
}: GameBoardCardPreviewDialogProps) {
  return (
    <PlayedCardPreviewDialog
      card={
        cell
          ? {
              title: cell.title,
              description: cell.description,
              cost: cell.cost,
              media: cell.media,
            }
          : null
      }
      round={playResult.round}
      isLoading={playResult.isLoading}
      isError={playResult.isError}
      onClose={onClose}
    />
  )
}
