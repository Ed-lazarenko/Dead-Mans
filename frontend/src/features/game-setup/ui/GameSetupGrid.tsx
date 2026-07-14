import { Box } from '@mui/material'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameSetupSnapshot } from '../../../shared/api/contracts/index.ts'
import { BoardMatrix, FormTextField, SectionCard } from '../../../shared/ui/index.ts'
import {
  getGameSetupCellAt,
  upsertGameSetupCellDraft,
  type GameSetupDraftState,
} from '../model/game-setup-draft.ts'
import type { GameSetupCellMediaDisplayState } from '../model/game-setup-cell-media-display.ts'
import { resolveGameSetupCellImageUrl } from '../model/game-setup-cell-media-display.ts'
import {
  GAME_SETUP_MAX_CELL_TITLE_LENGTH,
  GAME_SETUP_MAX_COLUMN_LABEL_LENGTH,
  GAME_SETUP_MAX_ROW_LABEL_LENGTH,
} from '../model/game-setup-limits.ts'
import { setupCellCardSx } from '../theme/setup-cell-sx.ts'
import { GameSetupCellImage } from './GameSetupCellImage.tsx'

interface GameSetupGridProps {
  snapshot: GameSetupSnapshot
  draft: GameSetupDraftState
  onDraftChange: (updater: (current: GameSetupDraftState) => GameSetupDraftState) => void
  cellMediaDisplayByCellId: Record<string, GameSetupCellMediaDisplayState>
  isCellMediaBusy: (cellId: string | undefined) => boolean
  onUploadCellMedia: (cellId: string | undefined, file: File) => void
  onDeleteCellMedia: (cellId: string | undefined) => void
}

export function GameSetupGrid({
  snapshot,
  draft,
  onDraftChange,
  cellMediaDisplayByCellId,
  isCellMediaBusy,
  onUploadCellMedia,
  onDeleteCellMedia,
}: GameSetupGridProps) {
  const { t } = useTranslation()
  const mediaUrlByCellId = useMemo(() => {
    return new Map(
      snapshot.cells
        .filter((cell) => cell.media[0]?.url)
        .map((cell) => [cell.id, cell.media[0]!.url] as const),
    )
  }, [snapshot.cells])

  return (
    <Box sx={{ flex: 1, minWidth: 0 }}>
      <BoardMatrix
        colLabels={draft.colLabels}
        rowLabels={draft.rowLabels}
        minWidth={680}
        gap={0.75}
        leadColumnWidth={120}
        renderColumnLabel={(columnLabel, columnIndex) => (
          <FormTextField
            layout="centered"
            label={t('gameSetup.columnLabel', { column: columnIndex + 1 })}
            value={columnLabel}
            onChange={(event) => {
              const nextValue = event.target.value
              onDraftChange((current) => ({
                ...current,
                colLabels: current.colLabels.map((label, index) =>
                  index === columnIndex ? nextValue : label,
                ),
              }))
            }}
            inputProps={{
              maxLength: GAME_SETUP_MAX_COLUMN_LABEL_LENGTH,
            }}
            sx={{
              mt: 1.25,
              '& .MuiInputLabel-root': {
                lineHeight: 1.2,
              },
              '& .MuiInputBase-input': {
                px: 1.25,
              },
            }}
          />
        )}
        renderRowLabel={(rowLabel, rowIndex) => (
          <FormTextField
            layout="compact"
            label={t('gameSetup.rowLabel', { row: rowIndex + 1 })}
            value={rowLabel}
            onChange={(event) => {
              const nextValue = event.target.value
              onDraftChange((current) => ({
                ...current,
                rowLabels: current.rowLabels.map((label, index) =>
                  index === rowIndex ? nextValue : label,
                ),
              }))
            }}
            inputProps={{
              maxLength: GAME_SETUP_MAX_ROW_LABEL_LENGTH,
            }}
            sx={{
              alignSelf: 'center',
              '& .MuiInputBase-root': {
                minHeight: 72,
              },
              '& .MuiInputBase-input': {
                px: 1,
                py: 2,
                textAlign: 'center',
                fontWeight: 600,
              },
            }}
          />
        )}
        renderCell={(rowIndex, colIndex, rowLabel) => {
          const cellDraft = getGameSetupCellAt(draft, rowIndex, colIndex)
          const cellId = cellDraft?.id
          const serverImageUrl = cellId ? mediaUrlByCellId.get(cellId) : undefined
          const cellDisplay = cellId ? cellMediaDisplayByCellId[cellId] : undefined
          const imageUrl = resolveGameSetupCellImageUrl(serverImageUrl, cellDisplay)
          const cellImageBusy = isCellMediaBusy(cellId)
          const imageKey = cellId
            ? `${cellId}:${cellDisplay?.cacheRevision ?? 0}:${cellDisplay?.phase ?? 'idle'}`
            : undefined

          return (
            <SectionCard variantStyle="dashed" sx={setupCellCardSx}>
              <GameSetupCellImage
                imageUrl={imageUrl}
                imageKey={imageKey}
                alt={cellDraft?.title ?? rowLabel}
                cellId={cellId}
                phase={cellDisplay?.phase ?? 'idle'}
                canManageMedia
                isBusy={cellImageBusy}
                onUpload={onUploadCellMedia}
                onDelete={onDeleteCellMedia}
              />
              <FormTextField
                label={t('gameSetup.cellTitleLabel')}
                value={cellDraft?.title ?? ''}
                onChange={(event) => {
                  const nextValue = event.target.value
                  onDraftChange((current) =>
                    upsertGameSetupCellDraft(current, rowIndex, colIndex, {
                      title: nextValue,
                    }),
                  )
                }}
                inputProps={{ maxLength: GAME_SETUP_MAX_CELL_TITLE_LENGTH }}
              />
              <FormTextField
                label={t('gameSetup.cellPriceLabel')}
                value={cellDraft?.cost ?? 0}
                onChange={(event) => {
                  const parsed = Number.parseInt(event.target.value, 10)
                  const nextCost = Number.isNaN(parsed) ? 0 : Math.max(0, parsed)
                  onDraftChange((current) =>
                    upsertGameSetupCellDraft(current, rowIndex, colIndex, {
                      cost: nextCost,
                    }),
                  )
                }}
                type="number"
                inputProps={{ min: 0 }}
                sx={{
                  '& .MuiInputBase-input': {
                    textAlign: 'center',
                  },
                }}
              />
            </SectionCard>
          )
        }}
      />
    </Box>
  )
}
