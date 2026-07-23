import { Navigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { currentGameBoardQueryOptions } from '../features/game-board/index.ts'
import { gameRegistrationSnapshotQueryOptions } from '../features/game-registration/index.ts'
import { CenteredProgress } from '../shared/ui/index.ts'
import { gameApplicationRoute, gameBoardRoute } from './app-routes.ts'
import { useAccessiblePanelRoutes } from './use-accessible-panel-routes.ts'

export function PanelIndexRedirect() {
  const accessibleRoutes = useAccessiblePanelRoutes()
  const gameBoardQuery = useQuery(currentGameBoardQueryOptions)
  const isRegistrationOpen = gameBoardQuery.data?.status === 'ready'
  const registrationSnapshotQuery = useQuery({
    ...gameRegistrationSnapshotQueryOptions,
    enabled: isRegistrationOpen,
  })
  const registrationRoute = accessibleRoutes.find((route) => route.id === gameApplicationRoute.id)
  const boardRoute = accessibleRoutes.find((route) => route.id === gameBoardRoute.id)
  const defaultRoute =
    isRegistrationOpen && registrationSnapshotQuery.data && registrationRoute
      ? registrationRoute
      : boardRoute ?? accessibleRoutes[0]

  if (gameBoardQuery.isLoading || (isRegistrationOpen && registrationSnapshotQuery.isLoading)) {
    return <CenteredProgress minHeight={240} />
  }

  if (!defaultRoute) {
    return <Navigate to="/" replace />
  }

  return <Navigate to={defaultRoute.fullPath} replace />
}
