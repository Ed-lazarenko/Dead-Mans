import { Box, Chip, Typography } from '@mui/material'
import { Fragment, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { GameBoardSnapshot } from '../../../shared/api/contracts/index.ts'

interface GameBoardGridProps {
  snapshot: GameBoardSnapshot
}

export function GameBoardGrid({ snapshot }: GameBoardGridProps) {
  const { t } = useTranslation()
  const cellMap = useMemo(() => {
    return new Map(snapshot.cells.map((cell) => [`${cell.row}:${cell.col}`, cell] as const))
  }, [snapshot.cells])

  return (
    <Box sx={{ mt: 2, overflow: 'auto' }}>
      <Box sx={{ minWidth: { xs: 480, sm: 'auto' } }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: `auto repeat(${snapshot.colLabels.length}, 1fr)`,
            columnGap: 0.5,
            rowGap: 0.5,
            mb: 1,
            alignItems: 'center',
          }}
        >
          <Box sx={{ textAlign: 'center', fontWeight: 700 }}> </Box>
          {snapshot.colLabels.map((col) => (
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
            gridTemplateColumns: `auto repeat(${snapshot.colLabels.length}, 1fr)`,
            columnGap: 0.5,
            rowGap: 0.5,
            alignItems: 'stretch',
          }}
        >
          {snapshot.rowLabels.map((rowLabel, rowIndex) => (
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
              {snapshot.colLabels.map((_, colIndex) => {
                const cell = cellMap.get(`${rowIndex}:${colIndex}`)
                const isOpen = cell?.state === 'open'
                const url = isOpen ? cell?.media[0]?.url : undefined

                return (
                  <Box
                    key={`${rowLabel}-${colIndex}`}
                    sx={{
                      border: isOpen ? '2px solid #90caf9' : '1px solid rgba(255,255,255,0.12)',
                      borderRadius: 1,
                      position: 'relative',
                      overflow: 'hidden',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      aspectRatio: '5 / 6',
                      gap: 0.5,
                      p: 0.5,
                    }}
                  >
                    {url ? (
                      <Box
                        component="img"
                        src={url}
                        alt={cell?.title ?? rowLabel}
                        loading="lazy"
                        decoding="async"
                        sx={{
                          position: 'absolute',
                          inset: 0,
                          width: '100%',
                          height: '100%',
                          objectFit: 'cover',
                        }}
                      />
                    ) : null}
                    <Box
                      sx={{
                        position: 'relative',
                        zIndex: 1,
                        textAlign: 'center',
                        px: 0.5,
                        pointerEvents: 'none',
                      }}
                    >
                      {cell ? (
                        <>
                          {isOpen && !url ? (
                            <Typography variant="subtitle2" color="text.primary">
                              {cell.title || t('gameBoard.cellLabel')}
                            </Typography>
                          ) : null}
                          {!isOpen ? (
                            <Typography
                              variant="subtitle2"
                              color="text.secondary"
                              sx={{ textTransform: 'uppercase', letterSpacing: '0.08em' }}
                            >
                              {t('gameBoard.closedCellLabel')}
                            </Typography>
                          ) : null}
                          <Chip size="small" label={t('gameBoard.costLabel', { cost: cell.cost })} />
                        </>
                      ) : (
                        <Typography variant="caption" color="text.disabled">
                          —
                        </Typography>
                      )}
                    </Box>
                  </Box>
                )
              })}
            </Fragment>
          ))}
        </Box>
      </Box>
    </Box>
  )
}
