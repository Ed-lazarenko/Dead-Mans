import type { components } from './generated'

export type GameBoardCellId = components['schemas']['GameBoardCellDto']['id']
export type GameBoardCellMedia = components['schemas']['GameBoardCellMediaDto']
export type GameBoardCell = components['schemas']['GameBoardCellDto']
export type GameBoardSnapshot = Omit<components['schemas']['GameBoardSnapshotDto'], 'status'> & {
  status: 'ready' | 'active' | 'finished'
}
export type GameCellOpenedEvent = components['schemas']['GameCellOpenedEventDto']
export type GameModifierActivatedEvent = components['schemas']['GameModifierActivatedEventDto']
export type GameSetupSnapshot = components['schemas']['GameSetupSnapshotDto']
export type CreateGameSetupRequest = components['schemas']['CreateGameSetupRequestDto']
export type UpdateGameSetupRequest = components['schemas']['UpdateGameSetupRequestDto']
export type ErrorResponse = components['schemas']['ErrorResponse']

export type GameModifierDefinition = components['schemas']['GameModifierDefinitionDto']
export type GameModifierActivation = components['schemas']['GameModifierActivationDto']
export type CreateGameModifierRequest = components['schemas']['CreateGameModifierRequestDto']
export type UpdateGameModifierRequest = components['schemas']['UpdateGameModifierRequestDto']

export type GameQuestionCatalogItem = components['schemas']['GameQuestionCatalogItemDto']
export type GameQuestionCategoryItem = components['schemas']['GameQuestionCategoryItemDto']
export type ImportGameQuestionsResult = components['schemas']['ImportGameQuestionsResultDto']
export type ImportGameQuestionSkippedItem =
  components['schemas']['ImportGameQuestionSkippedItemDto']
export type CreateGameQuestionRequest = components['schemas']['CreateGameQuestionRequestDto']
export type CreateGameQuestionCategoryRequest =
  components['schemas']['CreateGameQuestionCategoryRequestDto']
export type UpdateGameQuestionRequest = components['schemas']['UpdateGameQuestionRequestDto']

export type AuthRole = components['schemas']['AuthRole']
export type AuthSession = components['schemas']['AuthSessionDto']

export type RegistrationTeam = components['schemas']['RegistrationTeamDto']
export type RegistrationInvitation = components['schemas']['RegistrationInvitationDto']
export type RegistrationPlayer = components['schemas']['RegistrationPlayerDto']
export type GameRegistrationSnapshot = components['schemas']['GameRegistrationSnapshotDto']
export type GameRegistrationAdminSnapshot =
  components['schemas']['GameRegistrationAdminSnapshotDto']
