import { useCallback, useState } from 'react'
import type { LoadoutCellId } from '../../../shared/api/contracts.ts'
import {
  clearOpenedLoadoutCellIds,
  readOpenedLoadoutCellIds,
  writeOpenedLoadoutCellIds,
} from '../../../shared/session/loadoutOpenedCardsStorage.ts'

export function useOpenedLoadoutCells() {
  const [openedCells, setOpenedCells] = useState<Set<LoadoutCellId>>(
    () => new Set(readOpenedLoadoutCellIds()),
  )

  const openCell = useCallback((cellId: LoadoutCellId) => {
    setOpenedCells((current) => {
      if (current.has(cellId)) {
        return current
      }

      const next = new Set(current)
      next.add(cellId)
      writeOpenedLoadoutCellIds(next)
      return next
    })
  }, [])

  const isCellOpened = useCallback(
    (cellId: LoadoutCellId) => openedCells.has(cellId),
    [openedCells],
  )

  const clearAllOpenedCells = useCallback(() => {
    clearOpenedLoadoutCellIds()
    setOpenedCells(new Set())
  }, [])

  return {
    openedCells,
    openCell,
    isCellOpened,
    clearAllOpenedCells,
  }
}
