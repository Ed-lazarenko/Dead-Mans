export const panelRootPath = '/panel'

export const gameBoardRoute = {
  path: 'game-board',
  fullPath: `${panelRootPath}/game-board`,
} as const

export const defaultRoute = gameBoardRoute
