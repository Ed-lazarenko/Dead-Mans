import { Suspense, lazy } from 'react'
import { Box, CircularProgress } from '@mui/material'
import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from './layouts/MainLayout.tsx'
import { appRoutes, defaultRoute, panelRootPath } from './routes/appRoutes.ts'
import { RequireRole } from './shared/auth/RequireRole.tsx'

const AuthLandingPage = lazy(() =>
  import('./features/auth/AuthLandingPage.tsx').then((module) => ({
    default: module.AuthLandingPage,
  })),
)
const TwitchAuthCallbackPage = lazy(() =>
  import('./features/auth/TwitchAuthCallbackPage.tsx').then((module) => ({
    default: module.TwitchAuthCallbackPage,
  })),
)

function AppFallback() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <CircularProgress size={28} />
    </Box>
  )
}

function PanelRouteFallback() {
  return (
    <Box
      sx={{
        minHeight: 240,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <CircularProgress size={28} />
    </Box>
  )
}

function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <Suspense fallback={<AppFallback />}>
            <AuthLandingPage />
          </Suspense>
        }
      />
      <Route
        path="/auth/callback"
        element={
          <Suspense fallback={<AppFallback />}>
            <TwitchAuthCallbackPage />
          </Suspense>
        }
      />
      <Route path={panelRootPath} element={<MainLayout />}>
        <Route index element={<Navigate to={defaultRoute.fullPath} replace />} />
        {appRoutes.map((route) => (
          <Route
            key={route.id}
            path={route.path}
            element={
              <RequireRole allowedRoles={route.allowedRoles}>
                <Suspense fallback={<PanelRouteFallback />}>
                  <route.Component />
                </Suspense>
              </RequireRole>
            }
          />
        ))}
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
