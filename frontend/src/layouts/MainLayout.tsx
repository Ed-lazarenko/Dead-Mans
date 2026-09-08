import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { ModifierCatalogRealtimeSync } from '../features/modifier-history/ModifierCatalogRealtimeSync.tsx'
import { uiTokens } from '../shared/theme/tokens.ts'
import { PanelNavigation } from './PanelNavigation.tsx'

export function MainLayout() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <PanelNavigation />
      <ModifierCatalogRealtimeSync />

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          py: { xs: 2, sm: 3 },
          px: { xs: 2, sm: 3 },
          pb: uiTokens.spacing.page.md,
          display: 'flex',
          flexDirection: 'column',
          width: '100%',
        }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
