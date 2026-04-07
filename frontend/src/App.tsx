import { Suspense, lazy } from 'react'
import { Box, CircularProgress } from '@mui/material'
import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from './layouts/MainLayout.tsx'
import { defaultRoute, gameBoardRoute, panelRootPath } from './routes/app-routes.ts'
import { RequireAuth } from './shared/auth/RequireAuth.tsx'
import { GameBoardRealtimeSync } from './features/game-board/realtime/GameBoardRealtimeSync.tsx'

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
const GameBoardPage = lazy(() =>
  import('./features/game-board/GameBoardPage.tsx').then((module) => ({
    default: module.GameBoardPage,
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
        <Route
          path={gameBoardRoute.path}
          element={
            <RequireAuth>
              <GameBoardRealtimeSync />
              <Suspense fallback={<PanelRouteFallback />}>
                <GameBoardPage />
              </Suspense>
            </RequireAuth>
          }
        />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
