import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    testTimeout: 10_000,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary', 'html'],
      include: [
        'src/features/game-setup/model/game-setup-board-ops.ts',
        'src/features/game-setup/model/game-setup-draft.ts',
        'src/features/game-setup/model/game-setup-query-state.ts',
        'src/features/game-setup/use-game-setup-draft.ts',
        'src/features/game-setup/use-game-setup-save.ts',
        'src/features/game-registration/api/game-registration-mutation-options.ts',
        'src/features/game-board/realtime/game-board-realtime-model.ts',
        'src/features/game-setup/realtime/game-setup-realtime-model.ts',
        'src/routes/app-routes.ts',
        'src/routes/RequirePanelRouteAccess.tsx',
        'src/shared/auth/panel-capabilities.ts',
      ],
      thresholds: {
        'src/features/game-setup/model/*.ts': {
          statements: 80,
          branches: 70,
          functions: 80,
          lines: 80,
        },
        'src/features/game-setup/use-game-setup-{draft,save}.ts': {
          statements: 70,
          branches: 70,
          functions: 60,
          lines: 70,
        },
        'src/features/**/realtime/*-realtime-model.ts': {
          statements: 90,
          branches: 80,
          functions: 90,
          lines: 90,
        },
        'src/features/game-registration/api/game-registration-mutation-options.ts': {
          statements: 90,
          branches: 80,
          functions: 90,
          lines: 90,
        },
        'src/routes/app-routes.ts': {
          statements: 80,
          branches: 70,
          functions: 80,
          lines: 80,
        },
        'src/routes/RequirePanelRouteAccess.tsx': {
          statements: 80,
          branches: 70,
          functions: 80,
          lines: 80,
        },
        'src/shared/auth/panel-capabilities.ts': {
          statements: 90,
          branches: 80,
          functions: 90,
          lines: 90,
        },
      },
    },
  },
})
