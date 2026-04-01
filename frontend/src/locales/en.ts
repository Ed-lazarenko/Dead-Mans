const en = {
  translation: {
    appTitle: "Dead Man's Game",
    auth: {
      title: "Dead Man's Game",
      subtitle: 'Sign in with Twitch to open the game board.',
      description: 'After sign-in the panel loads the current game board from the database.',
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
    gameBoard: {
      title: 'Game board',
      loading: 'Loading game board...',
      errorLoading: 'Failed to load game board.',
      empty: 'No active or finished game in the database yet.',
      cellLabel: 'Cell',
      closedCellLabel: 'Hidden',
      costLabel: '{{cost}} pts',
      statusActive: 'Active',
      statusFinished: 'Finished',
    },
  },
}

export default en
