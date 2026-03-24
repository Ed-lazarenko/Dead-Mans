import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '../../shared/api/queryKeys.ts'
import { useAuth } from '../../shared/auth/useAuth.ts'
import { activateModifier, getModifiersSnapshot } from './api/modifiersDataAccess.ts'

export function useModifiersPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()

  const query = useQuery({
    queryKey: queryKeys.modifiers.snapshot(),
    queryFn: getModifiersSnapshot,
  })

  const activateMutation = useMutation({
    mutationFn: (modifierId: string) =>
      activateModifier(modifierId, user?.displayName ?? user?.role ?? 'viewer'),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.modifiers.all })
    },
  })

  return {
    ...query,
    activateMutation,
  }
}


