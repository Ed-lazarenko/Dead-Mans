import { useCallback, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { LoadoutCell, LoadoutCellId } from '../../shared/api/contracts/index.ts'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import { getLoadoutBoard } from './api/loadoutDataAccess.ts'
import { useOpenedLoadoutCells } from './model/useOpenedLoadoutCells.ts'

export function useLoadoutPage() {
  const query = useQuery({
    queryKey: queryKeys.loadout.board(),
    queryFn: getLoadoutBoard,
  })

  const { isCellOpened, openCell } = useOpenedLoadoutCells()
  const [fullscreenCellId, setFullscreenCellId] = useState<LoadoutCellId | null>(null)

  const handleCellClick = useCallback(
    (cell: LoadoutCell | undefined) => {
      if (!cell) {
        return
      }

      if (!isCellOpened(cell.id)) {
        openCell(cell.id)
        setFullscreenCellId(null)
        return
      }

      setFullscreenCellId(cell.id)
    },
    [isCellOpened, openCell],
  )

  const fullscreenCell = useMemo(
    () => query.data?.cells.find((cell) => cell.id === fullscreenCellId) ?? null,
    [query.data, fullscreenCellId],
  )

  return {
    ...query,
    isCellOpened,
    handleCellClick,
    fullscreenCell,
    closeFullscreen: () => setFullscreenCellId(null),
  }
}

