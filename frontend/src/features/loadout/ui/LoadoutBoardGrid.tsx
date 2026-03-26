import { Box, Typography } from '@mui/material'
import { Fragment } from 'react'
import { useTranslation } from 'react-i18next'
import type { LoadoutBoard, LoadoutCell } from '../../../shared/api/contracts/index.ts'
import { LOADOUT_CARD_THUMB_ASPECT_RATIO } from '../../../shared/loadout/constants.ts'
import { findLoadoutCell } from '../lib/findLoadoutCell.ts'

interface LoadoutBoardGridProps {
  board: LoadoutBoard
  isCellOpened: (cellId: string) => boolean
  onCellClick: (cell: LoadoutCell | undefined) => void
}

export function LoadoutBoardGrid({
  board,
  isCellOpened,
  onCellClick,
}: LoadoutBoardGridProps) {
  const { t } = useTranslation()

  return (
    <Box
      sx={{
        mt: 2,
        flexGrow: 1,
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          flexGrow: 1,
          minHeight: 0,
          overflow: 'auto',
        }}
      >
        <Box
          sx={{
            minWidth: { xs: 640, sm: 'auto' },
          }}
        >
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: `auto repeat(${board.colLabels.length}, 1fr)`,
              columnGap: 0.5,
              rowGap: 0.5,
              mb: 1,
              alignItems: 'center',
            }}
          >
            <Box sx={{ textAlign: 'center', fontWeight: 700 }}>💀</Box>
            {board.colLabels.map((col) => (
              <Box
                key={col}
                sx={{
                  textAlign: 'center',
                  fontWeight: 600,
                  fontSize: { xs: '0.7rem', sm: '0.8rem' },
                  px: 0.5,
                }}
              >
                {col}
              </Box>
            ))}
          </Box>

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: `auto repeat(${board.colLabels.length}, 1fr)`,
              columnGap: 0.5,
              rowGap: 0.5,
              alignItems: 'stretch',
            }}
          >
            {board.rowLabels.map((rowLabel, rowIndex) => (
              <Fragment key={rowLabel}>
                <Box
                  sx={{
                    textAlign: 'center',
                    fontWeight: 600,
                    fontSize: { xs: '0.7rem', sm: '0.8rem' },
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    px: 0.5,
                  }}
                >
                  {rowLabel}
                </Box>
                {board.colLabels.map((_, colIndex) => {
                  const cell = findLoadoutCell(board, rowIndex, colIndex)
                  const isOpened = cell ? isCellOpened(cell.id) : false

                  return (
                    <Box
                      key={`${rowLabel}-${colIndex}`}
                      onClick={() => onCellClick(cell)}
                      sx={{
                        cursor: cell ? 'pointer' : 'default',
                        border: isOpened ? '2px solid #90caf9' : '1px solid rgba(255,255,255,0.12)',
                        borderRadius: 1,
                        position: 'relative',
                        backgroundColor: 'inherit',
                        overflow: 'hidden',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        aspectRatio: LOADOUT_CARD_THUMB_ASPECT_RATIO,
                      }}
                    >
                      {cell && cell.imageUrl && isOpened ? (
                        <Box
                          component="img"
                          src={cell.imageUrl}
                          alt={cell.label}
                          sx={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'cover',
                            filter: 'none',
                          }}
                        />
                      ) : (
                        <Typography
                          variant="subtitle2"
                          color="text.secondary"
                          sx={{ textTransform: 'uppercase', letterSpacing: '0.08em' }}
                        >
                          {t('loadout.hiddenCellLabel')}
                        </Typography>
                      )}
                    </Box>
                  )
                })}
              </Fragment>
            ))}
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
