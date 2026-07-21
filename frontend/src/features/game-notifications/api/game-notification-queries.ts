import { queryOptions } from '@tanstack/react-query'
import { fetchGameNotifications } from './game-notifications-api.ts'

export const gameNotificationQueryKeys = {
  all: ['gameNotifications'] as const,
  list: () => [...gameNotificationQueryKeys.all, 'list'] as const,
}

export const gameNotificationsQueryOptions = queryOptions({
  queryKey: gameNotificationQueryKeys.list(),
  queryFn: fetchGameNotifications,
  staleTime: 0,
  refetchOnWindowFocus: true,
  refetchOnReconnect: true,
})
