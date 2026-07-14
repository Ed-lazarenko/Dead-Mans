import { Box } from '@mui/material'
import { Fragment, type ReactNode } from 'react'

interface BoardMatrixProps {
  colLabels: readonly string[]
  rowLabels: readonly string[]
  minWidth?: number
  gap?: number
  leadColumnWidth?: number | string
  leadCell?: ReactNode
  renderColumnLabel: (columnLabel: string, columnIndex: number) => ReactNode
  renderRowLabel: (rowLabel: string, rowIndex: number) => ReactNode
  renderCell: (rowIndex: number, colIndex: number, rowLabel: string) => ReactNode
}

export function BoardMatrix({
  colLabels,
  rowLabels,
  minWidth = 680,
  gap = 0.75,
  leadColumnWidth = 132,
  leadCell,
  renderColumnLabel,
  renderRowLabel,
  renderCell,
}: BoardMatrixProps) {
  return (
    <Box sx={{ overflow: 'auto' }}>
      <Box sx={{ minWidth: { xs: minWidth, sm: 'auto' } }}>
        <Box
          data-testid="board-matrix-grid"
          sx={{
            display: 'grid',
            gridTemplateColumns: `${typeof leadColumnWidth === 'number' ? `${leadColumnWidth}px` : leadColumnWidth} repeat(${colLabels.length}, minmax(0, 1fr))`,
            columnGap: gap,
            rowGap: gap,
            alignItems: 'stretch',
          }}
        >
          {leadCell ?? <Box sx={{ minWidth: 0 }} />}
          {colLabels.map((columnLabel, columnIndex) => (
            <Fragment key={`column-${columnIndex}`}>
              {renderColumnLabel(columnLabel, columnIndex)}
            </Fragment>
          ))}
          {rowLabels.map((rowLabel, rowIndex) => (
            <Fragment key={`row-${rowIndex}`}>
              {renderRowLabel(rowLabel, rowIndex)}
              {colLabels.map((_, colIndex) => (
                <Fragment key={`${rowIndex}-${colIndex}`}>
                  {renderCell(rowIndex, colIndex, rowLabel)}
                </Fragment>
              ))}
            </Fragment>
          ))}
        </Box>
      </Box>
    </Box>
  )
}
