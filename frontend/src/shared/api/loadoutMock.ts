import type { LoadoutBoard, LoadoutCell, LoadoutCellId } from './types.ts'

// TODO: позже заменить на реальные запросы к backend /api/loadout или SignalR‑канал,
// чтобы синхронизировать состояние ячеек между несколькими клиентами и хранить настройки сетки.

/**
 * Базовые настройки сетки лоадаутов (mock).
 * В будущем будут приходить с backend и редактироваться через UI.
 */
const defaultLoadoutConfig = {
  rowLabels: ['100', '125', '150', '175', '200'],
  colLabels: ['Бомбардир', 'Пиромант', 'Токсик', 'Вампир', 'Аватар', 'Всё могу x2'],
  /**
   * Базовый путь до мок-картинок, скопированных из legacy-v1/public/Loadouts/Стихии
   * в папку frontend/public/mock-loadouts/elements.
   */
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

// Оставляем функцию для будущего использования (пометка ячеек как сыгранных),
// но сейчас страницы лоадаутов её не вызывают.
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

