import { useCallback, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { LoadoutCell, LoadoutCellId } from '../../shared/api/contracts/index.ts'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import { getLoadoutBoard, toggleLoadoutCellState } from './api/loadoutDataAccess.ts'

export function useLoadoutPage() {
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: queryKeys.loadout.board(),
    queryFn: getLoadoutBoard,
  })
  const [fullscreenCellId, setFullscreenCellId] = useState<LoadoutCellId | null>(null)
  const toggleCellMutation = useMutation({
    mutationFn: toggleLoadoutCellState,
    onSuccess: (board) => {
      queryClient.setQueryData(queryKeys.loadout.board(), board)
    },
  })

  const isCellOpened = useCallback(
    (cellId: LoadoutCellId) =>
      query.data?.cells.find((cell) => cell.id === cellId)?.state === 'open',
    [query.data],
  )

  const handleCellClick = useCallback(
    async (cell: LoadoutCell | undefined) => {
      if (!cell) {
        return
      }

      if (cell.state !== 'open') {
        await toggleCellMutation.mutateAsync(cell.id)
        setFullscreenCellId(null)
        return
      }

      setFullscreenCellId(cell.id)
    },
    [toggleCellMutation],
  )

  const fullscreenCell = useMemo(
    () => query.data?.cells.find((cell) => cell.id === fullscreenCellId) ?? null,
    [query.data, fullscreenCellId],
  )

  return {
    ...query,
    isUpdatingCell: toggleCellMutation.isPending,
    isCellOpened,
    handleCellClick,
    fullscreenCell,
    closeFullscreen: () => setFullscreenCellId(null),
  }
}

