import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiProxyTarget = env.VITE_API_PROXY_TARGET ?? 'http://localhost:5285'

  return {
    plugins: [react()],
    build: {
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (!id.includes('node_modules')) return

            if (id.includes('@mui/') || id.includes('@emotion/')) {
              return 'mui'
            }

            if (id.includes('@tanstack/')) {
              return 'react-query'
            }

            if (id.includes('@microsoft/signalr')) {
              return 'signalr'
            }

            if (id.includes('/zod/') || id.includes('\\zod\\')) {
              return 'zod'
            }

            if (id.includes('react-hook-form') || id.includes('@hookform/')) {
              return 'forms'
            }

            if (
              id.includes('react-router') ||
              id.includes('/react/') ||
              id.includes('\\react\\') ||
              id.includes('react-dom')
            ) {
              return 'react-vendor'
            }

            if (id.includes('i18next')) {
              return 'i18n'
            }
          },
        },
      },
    },
    server: {
      watch: {
        // Vitest deletes and recreates this generated directory. Ignoring it prevents
        // stale Windows file watchers without affecting source-file HMR.
        ignored: ['**/coverage', '**/coverage/**'],
      },
      proxy: {
        '/api': {
          target: apiProxyTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
