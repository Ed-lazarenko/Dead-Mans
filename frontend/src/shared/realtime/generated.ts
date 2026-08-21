// Generated from backend/openapi/deadmans.v1.yaml (x-signalr). Do not edit.
// Regenerate: npm --prefix frontend run generate:realtime

export const realtimeHubs = {
  gameBoard: {
    path: '/hubs/game-board',
    events: {
      cellOpened: 'cellOpened',
      modifierActivated: 'modifierActivated',
      modifierActivationCancelled: 'modifierActivationCancelled',
      modifierAvailabilityChanged: 'modifierAvailabilityChanged',
      roundStateChanged: 'roundStateChanged',
      quizStateChanged: 'quizStateChanged',
      userNotificationCreated: 'userNotificationCreated',
    },
  },
  gameSetup: {
    path: '/hubs/game-setup',
    events: {
      draftChanged: 'draftChanged',
    },
  },
} as const

export type RealtimeHubKey = keyof typeof realtimeHubs
export type RealtimeServerEventName<H extends RealtimeHubKey> =
  (typeof realtimeHubs)[H]['events'][keyof (typeof realtimeHubs)[H]['events']]
