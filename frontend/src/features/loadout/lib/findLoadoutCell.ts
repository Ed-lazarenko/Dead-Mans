import type { LoadoutBoard, LoadoutCell } from '../../../shared/api/contracts/index.ts'

export function findLoadoutCell(
  board: LoadoutBoard,
  row: number,
  col: number,
): LoadoutCell | undefined {
  return board.cells.find((cell) => cell.row === row && cell.col === col)
}
