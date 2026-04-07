import type { components } from './generated.ts'

export type GameBoardCellId = components['schemas']['GameBoardCellDto']['id']
export type GameBoardCellMedia = components['schemas']['GameBoardCellMediaDto']
export type GameBoardCell = components['schemas']['GameBoardCellDto']
export type GameBoardSnapshot = components['schemas']['GameBoardSnapshotDto']

export type AuthRole = components['schemas']['AuthRole']
export type AuthSession = components['schemas']['AuthSessionDto']
