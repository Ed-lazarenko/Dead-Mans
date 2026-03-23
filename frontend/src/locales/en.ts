const en = {
  translation: {
    appTitle: "Dead Man's Game",
    nav: {
      loadout: 'Loadouts',
      leaderboard: 'Leaderboard',
      modifiers: 'Modifiers',
      controls: 'Controls',
    },
    pages: {
      loadout: 'Loadout page placeholder',
      leaderboard: 'Leaderboard page placeholder',
      modifiers: 'Modifiers page placeholder',
      controls: 'Controls page placeholder',
    },
    auth: {
      title: "Dead Man's Loadout",
      subtitle: "Control panel for Dead Man's Loadout game sessions and viewer interaction.",
      description: 'Sign in to manage teams, loadouts and viewer modifiers during your stream.',
      button: 'Sign in with Twitch',
    },
    loadout: {
      loading: 'Loading loadout grid (mock)...',
      hint: 'Mock data. First click reveals the card, second click opens it fullscreen.',
    },
    controls: {
      loading: 'Loading game state (mock)...',
      currentState: 'Current game state (mock):',
      phase: 'Phase: {{phase}}',
      round: 'Round: {{current}}/{{total}}',
      lastAction: 'Last action: {{time}}',
      quickActions: 'Quick actions:',
      start: 'Start game',
      pause: 'Pause',
      resume: 'Resume',
      nextRound: 'Next round',
      resetAll: 'Reset all',
      closeAllLoadoutCards: 'Close all loadout cards',
      mockNotice:
        'This is mock logic for now. Later these actions will call real backend endpoints and broadcast updates via SignalR.',
    },
    modifiers: {
      loading: 'Loading modifiers (mock)...',
      availableTitle: 'Available modifiers',
      activateButton: 'Activate (-{{cost}})',
      activeTitle: 'Active modifiers',
      emptyActive: 'No active modifiers yet.',
      activeFrom: 'From: {{user}} • {{time}}',
    },
    leaderboard: {
      errorLoading: 'Failed to load leaderboard (mock).',
      mockUpdatedAt: 'Mock data. Updated at: {{time}}',
      columns: {
        position: '#',
        team: 'Team',
        score: 'Score',
        penalty: 'Penalty',
        total: 'Total',
      },
    },
    authErrors: {
      noPermissionTitle: 'Insufficient permissions',
      noPermissionDescription:
        'Your role does not have access to this page. Please sign in as a streamer or moderator.',
    },
  },
}

export default en
