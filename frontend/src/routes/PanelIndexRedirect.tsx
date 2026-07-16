import { Navigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { gameRegistrationSnapshotQueryOptions } from '../features/game-registration/index.ts'
import { CenteredProgress } from '../shared/ui/index.ts'
import { gameApplicationRoute } from './app-routes.ts'
import { useAccessiblePanelRoutes } from './use-accessible-panel-routes.ts'

export function PanelIndexRedirect() {
  const accessibleRoutes = useAccessiblePanelRoutes()
  const registrationSnapshotQuery = useQuery(gameRegistrationSnapshotQueryOptions)
  const registrationRoute = accessibleRoutes.find((route) => route.id === gameApplicationRoute.id)
  const defaultRoute =
    registrationSnapshotQuery.data && registrationRoute ? registrationRoute : accessibleRoutes[0]

  if (registrationSnapshotQuery.isLoading) {
    return <CenteredProgress minHeight={240} />
  }

  if (!defaultRoute) {
    return <Navigate to="/" replace />
  }

  return <Navigate to={defaultRoute.fullPath} replace />
}
