import type { LoadoutBoard, LoadoutCell, LoadoutCellId } from '../contracts/index.ts'

const defaultLoadoutConfig = {
  rowLabels: ['100', '125', '150', '175', '200'],
  colLabels: ['Бомбардир', 'Пиромант', 'Токсик', 'Вампир', 'Аватар', 'Всё могу x2'],
  imageBasePath: '/mock-loadouts/elements',
} as const

let board: LoadoutBoard = createInitialBoard(defaultLoadoutConfig)

function createInitialBoard(config: typeof defaultLoadoutConfig): LoadoutBoard {
  const rows = config.rowLabels.length
  const cols = config.colLabels.length
  const cells: LoadoutCell[] = []

  for (let r = 0; r < rows; r += 1) {
    for (let c = 0; c < cols; c += 1) {
      const id: LoadoutCellId = `r${r + 1}c${c + 1}`
      cells.push({
        id,
        row: r,
        col: c,
        label: `${config.rowLabels[r]} • ${config.colLabels[c]}`,
        points: Number.parseInt(config.rowLabels[r], 10) || 0,
        imageUrl: `${config.imageBasePath}/${c + 1}-${r + 1}.png`,
        state: 'available',
      })
    }
  }

  return {
    rows,
    cols,
    rowLabels: [...config.rowLabels],
    colLabels: [...config.colLabels],
    cells,
  }
}

export async function getLoadoutBoard(): Promise<LoadoutBoard> {
  await new Promise((resolve) => setTimeout(resolve, 150))
  return board
}

export async function toggleCellPlayed(cellId: LoadoutCellId): Promise<LoadoutBoard> {
  board = {
    ...board,
    cells: board.cells.map((cell) => {
      if (cell.id !== cellId || cell.state === 'locked') return cell

      return {
        ...cell,
        state: cell.state === 'played' ? 'available' : 'played',
      }
    }),
  }

  return getLoadoutBoard()
}
