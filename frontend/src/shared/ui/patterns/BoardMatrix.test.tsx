import { Box } from '@mui/material'
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { BoardMatrix } from './BoardMatrix.tsx'

describe('BoardMatrix', () => {
  it('renders headers and cells inside one shared grid container', () => {
    render(
      <BoardMatrix
        colLabels={['100', '200']}
        rowLabels={['10', '20']}
        leadCell={<Box>corner</Box>}
        renderColumnLabel={(label, index) => <Box key={`col-${index}`}>{label}</Box>}
        renderRowLabel={(label, index) => <Box key={`row-${index}`}>{label}</Box>}
        renderCell={(rowIndex, colIndex) => <Box>{`${rowIndex}-${colIndex}`}</Box>}
      />,
    )

    const grid = screen.getByTestId('board-matrix-grid')

    expect(grid).toBeInTheDocument()
    expect(grid.children).toHaveLength(9)
    expect(screen.queryAllByTestId('board-matrix-grid')).toHaveLength(1)
  })
})
