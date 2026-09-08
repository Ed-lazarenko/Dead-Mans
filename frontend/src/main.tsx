import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '@fontsource/cinzel/latin-500.css'
import '@fontsource/cinzel/latin-600.css'
import '@fontsource/cinzel/latin-700.css'
import '@fontsource/cinzel/latin-ext-500.css'
import '@fontsource/cinzel/latin-ext-600.css'
import '@fontsource/cinzel/latin-ext-700.css'
import '@fontsource/source-serif-4/cyrillic-400.css'
import '@fontsource/source-serif-4/cyrillic-400-italic.css'
import '@fontsource/source-serif-4/cyrillic-600.css'
import '@fontsource/source-serif-4/cyrillic-700.css'
import '@fontsource/source-serif-4/latin-400.css'
import '@fontsource/source-serif-4/latin-400-italic.css'
import '@fontsource/source-serif-4/latin-600.css'
import '@fontsource/source-serif-4/latin-700.css'
import '@fontsource/source-serif-4/latin-ext-400.css'
import '@fontsource/source-serif-4/latin-ext-400-italic.css'
import '@fontsource/source-serif-4/latin-ext-600.css'
import '@fontsource/source-serif-4/latin-ext-700.css'
import './index.css'
import App from './app/App.tsx'
import { AppProviders } from './app/providers/AppProviders.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </StrictMode>,
)
