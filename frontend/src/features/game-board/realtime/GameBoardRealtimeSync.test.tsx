import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { GameBoardRealtimeSync } from './GameBoardRealtimeSync.tsx'

const mocks = vi.hoisted(() => ({
  useSignalrHubLifecycle: vi.fn(),
  fetchSnapshot: vi.fn(),
}))

vi.mock('../../../shared/realtime/index.ts', () => ({
  realtimeHubs: {
    gameBoard: {
      events: {
        cellOpened: 'cellOpened',
        roundStateChanged: 'roundStateChanged',
        modifierActivated: 'modifierActivated',
        modifierActivationCancelled: 'modifierActivationCancelled',
        gameLifecycleChanged: 'gameLifecycleChanged',
      },
    },
  },
  useSignalrHubLifecycle: mocks.useSignalrHubLifecycle,
}))

vi.mock('../api/game-board-data-access.ts', () => ({
  fetchCurrentGameBoardSnapshot: mocks.fetchSnapshot,
  fetchCurrentGameTeamQueue: vi.fn(),
}))

describe('GameBoardRealtimeSync', () => {
  afterEach(() => vi.clearAllMocks())

  it('invalidates every completion-sensitive view on lifecycle change', async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries').mockResolvedValue(undefined)
    render(
      <QueryClientProvider client={queryClient}>
        <GameBoardRealtimeSync />
      </QueryClientProvider>,
    )
    const options = mocks.useSignalrHubLifecycle.mock.calls[0]?.[0]
    const handlers = new Map<string, (event: unknown) => void>()
    const connection = {
      on: vi.fn((name: string, handler: (event: unknown) => void) => handlers.set(name, handler)),
      off: vi.fn(),
    }
    const unregister = options.registerEventHandlers(connection)

    await act(async () => {
      handlers.get('gameLifecycleChanged')?.({
        gameId: 'game-1',
        status: 'finished',
        boardVersion: 8,
        occurredAtUtc: '2026-09-06T00:00:00Z',
      })
      await Promise.resolve()
    })

    for (const queryKey of [
      ['gameBoard', 'currentSnapshot'],
      ['gameRounds', 'active'],
      ['gameHistory'],
      ['gameModifiers'],
      ['gameRegistration'],
      ['gameFinish'],
    ]) {
      expect(invalidate).toHaveBeenCalledWith({ queryKey })
    }

    unregister()
    expect(connection.off).toHaveBeenCalledWith('gameLifecycleChanged', expect.any(Function))
  })
})
