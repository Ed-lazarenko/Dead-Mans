import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ModifierCatalogRealtimeSync } from './ModifierCatalogRealtimeSync.tsx'

const realtimeMocks = vi.hoisted(() => ({
  useSignalrHubLifecycle: vi.fn(),
}))

vi.mock('../../shared/realtime/index.ts', () => ({
  realtimeHubs: {
    gameBoard: {
      events: { modifierCatalogChanged: 'modifierCatalogChanged' },
    },
  },
  useSignalrHubLifecycle: realtimeMocks.useSignalrHubLifecycle,
}))

describe('ModifierCatalogRealtimeSync', () => {
  afterEach(() => vi.clearAllMocks())

  it('invalidates catalog, revision history and setup after a catalog event', async () => {
    const queryClient = new QueryClient()
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue(undefined)
    render(
      <QueryClientProvider client={queryClient}>
        <ModifierCatalogRealtimeSync />
      </QueryClientProvider>,
    )

    const options = realtimeMocks.useSignalrHubLifecycle.mock.calls[0]?.[0]
    const handlers = new Map<string, () => void>()
    const connection = {
      on: vi.fn((event: string, handler: () => void) => handlers.set(event, handler)),
      off: vi.fn(),
    }
    const unregister = options.registerEventHandlers(connection)

    await act(async () => {
      handlers.get('modifierCatalogChanged')?.()
      await Promise.resolve()
    })

    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['gameModifiers', 'catalog'] })
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['modifierHistory'] })
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['gameSetup', 'draftSnapshot'] })
    unregister()
    expect(connection.off).toHaveBeenCalledWith('modifierCatalogChanged', expect.any(Function))
  })
})
