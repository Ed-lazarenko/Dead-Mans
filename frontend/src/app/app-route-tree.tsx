import type { ComponentType, LazyExoticComponent } from 'react'
import { Suspense, lazy, createElement } from 'react'
import { Navigate } from 'react-router-dom'
import type { RouteObject } from 'react-router-dom'
import { MainLayout } from '../layouts/MainLayout.tsx'
import { panelRouteConfig } from './panel-route-config.tsx'
import { panelRootPath } from './panel-route-metadata.ts'
import { PanelIndexRedirect } from '../routes/PanelIndexRedirect.tsx'
import { RequirePanelRouteAccess } from '../routes/RequirePanelRouteAccess.tsx'
import { RequireAuth } from '../shared/auth/RequireAuth.tsx'
import { CenteredProgress } from '../shared/ui/index.ts'

const AuthLandingPage = lazy(() =>
  import('../features/auth/AuthLandingPage.tsx').then((module) => ({
    default: module.AuthLandingPage,
  })),
)
const TwitchAuthCallbackPage = lazy(() =>
  import('../features/auth/TwitchAuthCallbackPage.tsx').then((module) => ({
    default: module.TwitchAuthCallbackPage,
  })),
)

function lazyAuthRoute(
  path: string,
  Page: LazyExoticComponent<ComponentType<unknown>>,
): RouteObject {
  return {
    path,
    element: (
      <Suspense fallback={<CenteredProgress minHeight="100vh" />}>
        <Page />
      </Suspense>
    ),
  }
}

function createPanelChildRoute(route: (typeof panelRouteConfig)[number]): RouteObject {
  const { path, Page, Sync } = route

  return {
    path,
    element: (
      <RequirePanelRouteAccess route={route}>
        <Suspense fallback={<CenteredProgress minHeight={240} />}>
          {Sync ? createElement(Sync) : null}
          <Page />
        </Suspense>
      </RequirePanelRouteAccess>
    ),
  }
}

const panelChildRoutes: RouteObject[] = [
  { index: true, element: <PanelIndexRedirect /> },
  ...panelRouteConfig.map(createPanelChildRoute),
]

export const appRoutes: RouteObject[] = [
  lazyAuthRoute('/', AuthLandingPage),
  lazyAuthRoute('/auth/callback', TwitchAuthCallbackPage),
  {
    path: panelRootPath,
    element: (
      <RequireAuth>
        <MainLayout />
      </RequireAuth>
    ),
    children: panelChildRoutes,
  },
  { path: '*', element: <Navigate to="/" replace /> },
]
