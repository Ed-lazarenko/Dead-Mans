export const queryKeys = {
  leaderboard: {
    all: ['leaderboard'] as const,
    summary: () => ['leaderboard', 'summary'] as const,
  },
  loadout: {
    all: ['loadout'] as const,
    board: (boardId = 'default') => ['loadout', 'board', boardId] as const,
  },
  modifiers: {
    all: ['modifiers'] as const,
    snapshot: (scope = 'default') => ['modifiers', 'snapshot', scope] as const,
  },
  controls: {
    all: ['controls'] as const,
    state: (scope = 'default') => ['controls', 'state', scope] as const,
  },
}


