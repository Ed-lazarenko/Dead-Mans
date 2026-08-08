import { Box, Typography } from '@mui/material'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameBoardCell, GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'
import { resolveBackendMediaUrl } from '../../../shared/api/media-url.ts'
import { BoardMatrix } from '../../../shared/ui/index.ts'
import { createBoardCellSx } from '../theme/board-cell-sx.ts'

interface GameBoardGridProps {
  snapshot: GameBoardSnapshot
  canOpenCells: boolean
  onCellRequestOpen: (cell: GameBoardCell) => void
  onCellPreviewMedia: (cell: GameBoardCell) => void
}

export function GameBoardGrid({
  snapshot,
  canOpenCells,
  onCellRequestOpen,
  onCellPreviewMedia,
}: GameBoardGridProps) {
  const { t } = useTranslation()
  const cellMap = useMemo(() => {
    return new Map(snapshot.cells.map((cell) => [`${cell.row}:${cell.col}`, cell] as const))
  }, [snapshot.cells])

  return (
    <Box sx={{ mt: 1.25 }}>
      <BoardMatrix
        colLabels={snapshot.colLabels}
        rowLabels={snapshot.rowLabels}
        minWidth={520}
        gap={0.35}
        leadColumnWidth={48}
        leadCell={<Box />}
        renderColumnLabel={(col) => (
          <Box
            sx={{
              textAlign: 'center',
              fontWeight: 750,
              fontSize: { xs: '0.68rem', sm: '0.76rem' },
              color: 'text.secondary',
              px: 0.35,
            }}
          >
            {col}
          </Box>
        )}
        renderRowLabel={(rowLabel) => (
          <Box
            sx={{
              textAlign: 'center',
              fontWeight: 750,
              fontSize: { xs: '0.68rem', sm: '0.76rem' },
              color: 'text.secondary',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              px: 0.35,
            }}
          >
            {rowLabel}
          </Box>
        )}
        renderCell={(rowIndex, colIndex) => {
          const cell = cellMap.get(`${rowIndex}:${colIndex}`)
          const isOpen = cell?.state === 'open'
          const isClickable = Boolean(cell) && !isOpen && canOpenCells
          const previewMediaUrl = isOpen ? resolveBackendMediaUrl(cell?.media[0]?.url) : ''
          const hasPreviewMedia = previewMediaUrl.length > 0
          const isPreviewable = Boolean(cell) && isOpen
          const isInteractive = isClickable || isPreviewable

          return (
            <Box
              role={isInteractive ? 'button' : undefined}
              tabIndex={isInteractive ? 0 : undefined}
              aria-disabled={isInteractive ? undefined : true}
              aria-label={
                cell
                  ? isPreviewable
                    ? t('gameBoard.cellMediaPreviewAction', {
                        title: cell.title || t('gameBoard.cellLabel'),
                      })
                    : t('gameBoard.openConfirmDescription', {
                        cost: cell.cost,
                        row: cell.row,
                        col: cell.col,
                      })
                  : undefined
              }
              onClick={() => {
                if (cell && !isOpen && canOpenCells) {
                  onCellRequestOpen(cell)
                  return
                }

                if (cell && isPreviewable) {
                  onCellPreviewMedia(cell)
                }
              }}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault()
                  if (cell && !isOpen && canOpenCells) {
                    onCellRequestOpen(cell)
                    return
                  }

                  if (cell && isPreviewable) {
                    onCellPreviewMedia(cell)
                  }
                }
              }}
              sx={createBoardCellSx({ isOpen, isInteractive })}
            >
              {hasPreviewMedia ? (
                <Box
                  component="img"
                  src={previewMediaUrl}
                  alt={cell?.title || t('gameBoard.cellMediaDialogTitle')}
                  loading="lazy"
                  decoding="async"
                  sx={{
                    position: 'absolute',
                    inset: 0,
                    width: '100%',
                    height: '100%',
                    objectFit: 'cover',
                    opacity: 0.24,
                    filter: 'saturate(0.96)',
                    pointerEvents: 'none',
                  }}
                />
              ) : null}
              {isOpen ? (
                <Box
                  sx={(theme) => ({
                    position: 'absolute',
                    inset: 0,
                    background: hasPreviewMedia
                      ? `linear-gradient(180deg, rgba(7,10,16,0.08) 0%, rgba(7,10,16,0.26) 52%, ${theme.palette.background.paper} 100%)`
                      : 'transparent',
                    pointerEvents: 'none',
                  })}
                />
              ) : null}
              <Box
                sx={{
                  position: 'relative',
                  zIndex: 1,
                  textAlign: 'center',
                  width: '100%',
                  minWidth: 0,
                  px: 0.35,
                  pointerEvents: 'none',
                }}
              >
                {cell ? (
                  <>
                    {isOpen ? (
                      <Typography
                        variant="body2"
                        color="text.primary"
                        sx={{
                          display: '-webkit-box',
                          WebkitBoxOrient: 'vertical',
                          WebkitLineClamp: 2,
                          overflow: 'hidden',
                          fontWeight: 750,
                          lineHeight: 1.2,
                        }}
                      >
                        {cell.title || t('gameBoard.cellLabel')}
                      </Typography>
                    ) : null}
                    {!isOpen ? (
                      <Typography
                        variant="h6"
                        color="text.primary"
                        sx={{ fontWeight: 850, lineHeight: 1 }}
                      >
                        {cell.cost}
                      </Typography>
                    ) : null}
                    <Box
                      sx={{
                        mt: 0.45,
                        display: 'flex',
                        justifyContent: 'center',
                        gap: 0.35,
                        flexWrap: 'wrap',
                      }}
                    >
                      {isOpen ? (
                        <Typography variant="caption" color="text.secondary">
                          {t('gameBoard.costLabel', { cost: cell.cost })}
                        </Typography>
                      ) : null}
                      {hasPreviewMedia ? (
                        <Typography variant="caption" color="text.secondary">
                          {t('gameBoard.cellMediaCountLabel', {
                            count: cell.media.length,
                          })}
                        </Typography>
                      ) : null}
                    </Box>
                  </>
                ) : (
                  <Typography variant="caption" color="text.disabled">
                    —
                  </Typography>
                )}
              </Box>
            </Box>
          )
        }}
      />
    </Box>
  )
}
