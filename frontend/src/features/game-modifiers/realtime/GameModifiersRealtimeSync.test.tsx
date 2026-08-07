import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { GameModifiersRealtimeSync } from './GameModifiersRealtimeSync.tsx'

const realtimeMocks = vi.hoisted(() => ({
  useSignalrHubLifecycle: vi.fn(),
}))

vi.mock('../../../shared/realtime/index.ts', () => ({
  realtimeHubs: {
    gameBoard: {
      events: {
        modifierActivated: 'modifierActivated',
        modifierActivationCancelled: 'modifierActivationCancelled',
      },
    },
  },
  useSignalrHubLifecycle: realtimeMocks.useSignalrHubLifecycle,
}))

function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

describe('GameModifiersRealtimeSync', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('invalidates modifier queries on connect and on modifier realtime events', async () => {
    const queryClient = createQueryClient()
    const invalidateQueries = vi
      .spyOn(queryClient, 'invalidateQueries')
      .mockResolvedValue(undefined)

    render(
      <QueryClientProvider client={queryClient}>
        <GameModifiersRealtimeSync />
      </QueryClientProvider>,
    )

    const options = realtimeMocks.useSignalrHubLifecycle.mock.calls[0]?.[0]
    expect(options).toBeDefined()

    await act(async () => {
      await options.onConnected()
    })

    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: ['gameModifiers'],
    })

    const eventHandlers = new Map<string, () => void>()
    const connection = {
      on: vi.fn((event: string, handler: () => void) => {
        eventHandlers.set(event, handler)
      }),
      off: vi.fn(),
    }

    const unregister = options.registerEventHandlers(connection)

    await act(async () => {
      eventHandlers.get('modifierActivated')?.()
      eventHandlers.get('modifierActivationCancelled')?.()
      await Promise.resolve()
    })

    expect(invalidateQueries).toHaveBeenCalledTimes(3)

    unregister()

    expect(connection.off).toHaveBeenCalledWith('modifierActivated', expect.any(Function))
    expect(connection.off).toHaveBeenCalledWith('modifierActivationCancelled', expect.any(Function))
  })
})
