import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from './layouts/MainLayout.tsx'
import { appRoutes, defaultRoute, panelRootPath } from './routes/appRoutes.ts'
import { RequireRole } from './shared/auth/RequireRole.tsx'
import { AuthLandingPage } from './features/auth/AuthLandingPage.tsx'

function App() {
  return (
    <Routes>
      <Route path="/" element={<AuthLandingPage />} />
      <Route path={panelRootPath} element={<MainLayout />}>
        <Route index element={<Navigate to={defaultRoute.fullPath} replace />} />
        {appRoutes.map((route) => (
          <Route
            key={route.id}
            path={route.path}
            element={
              <RequireRole allowedRoles={route.allowedRoles}>
                <route.Component />
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
