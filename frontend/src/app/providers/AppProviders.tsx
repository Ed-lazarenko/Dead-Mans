import type { ReactNode } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n.ts'
import { AuthProvider } from '../../shared/auth/AuthContext.tsx'
import { appQueryClient } from './queryClient.ts'
import { appTheme } from '../theme/appTheme.ts'

interface AppProvidersProps {
  children: ReactNode
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={appQueryClient}>
        <ThemeProvider theme={appTheme}>
          <CssBaseline />
          <BrowserRouter>
            <AuthProvider>{children}</AuthProvider>
          </BrowserRouter>
          <ReactQueryDevtools initialIsOpen={false} />
        </ThemeProvider>
      </QueryClientProvider>
    </I18nextProvider>
  )
}
