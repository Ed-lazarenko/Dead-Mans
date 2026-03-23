import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import { activateModifier, getModifiersSnapshot } from './api/modifiersDataAccess.ts'

export function useModifiersPage() {
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: queryKeys.modifiers,
    queryFn: getModifiersSnapshot,
  })

  const activateMutation = useMutation({
    mutationFn: (modifierId: string) => activateModifier(modifierId, 'Viewer123'),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.modifiers })
    },
  })

  return {
    ...query,
    activateMutation,
  }
}


