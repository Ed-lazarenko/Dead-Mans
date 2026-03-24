const en = {
  translation: {
    appTitle: "Dead Man's Game",
    layout: {
      openNavigation: 'Open navigation',
    },
    nav: {
      loadout: 'Loadouts',
      leaderboard: 'Leaderboard',
      modifiers: 'Modifiers',
      controls: 'Controls',
    },
    pages: {
      loadout: 'Loadouts',
      leaderboard: 'Leaderboard',
      modifiers: 'Modifiers',
      controls: 'Controls',
    },
    auth: {
      title: "Dead Man's Loadout",
      subtitle: "Control panel for Dead Man's Loadout game sessions and viewer interaction.",
      description: 'Sign in to manage teams, loadouts and viewer modifiers during your stream.',
      button: 'Sign in with Twitch',
      processing: 'Finishing Twitch sign-in...',
      checkingSession: 'Checking your session...',
      callbackFailedTitle: 'Unable to complete sign-in',
      sessionRestoreFailed:
        'Twitch sign-in succeeded, but the app session could not be restored. Please try again.',
      backToLogin: 'Back to sign-in',
      callbackReasons: {
        access_denied: 'You cancelled the Twitch sign-in flow.',
        account_inactive: 'Your account is disabled. Contact an administrator to restore access.',
        missing_code: 'Twitch did not return an authorization code.',
        missing_state: 'Twitch did not return the security state parameter.',
        state_cookie_missing: 'The local sign-in session expired. Please start again.',
        state_mismatch: 'Security validation failed during sign-in. Please try again.',
        authentication_failed: 'The server could not complete authentication. Please try again later.',
        unknown: 'An unknown error occurred during sign-in.',
      },
    },
    loadout: {
      loading: 'Loading loadout grid...',
      errorLoading: 'Failed to load loadout.',
      hint: 'First click reveals the card, second click opens it fullscreen.',
      hiddenCellLabel: 'Hidden',
    },
    controls: {
      loading: 'Loading game state...',
      errorLoading: 'Failed to load game state.',
      currentState: 'Current game state:',
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
      mockNotice: 'Actions use the current API mode. Live updates via SignalR will be added later.',
    },
    modifiers: {
      loading: 'Loading modifiers...',
      errorLoading: 'Failed to load modifiers.',
      availableTitle: 'Available modifiers',
      availableLabel: '{{name}} (cost: {{cost}})',
      activateButton: 'Activate (-{{cost}})',
      activeTitle: 'Active modifiers',
      emptyActive: 'No active modifiers yet.',
      activeFrom: 'From: {{user}} • {{time}}',
    },
    leaderboard: {
      loading: 'Loading leaderboard...',
      errorLoading: 'Failed to load leaderboard.',
      updatedAt: 'Updated at: {{time}}',
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
        'Your role does not have access to this page. Please sign in as an administrator or moderator.',
    },
  },
}

export default en
