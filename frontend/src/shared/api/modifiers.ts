import { httpClient } from './client/httpClient.ts'
import { isHttpApiMode } from './config.ts'
import type { ActivateModifierInput, ModifiersSnapshot } from './contracts/index.ts'
import * as modifiersMockApi from './mocks/modifiersMock.ts'

export interface ModifiersApi {
  getModifiers: () => Promise<ModifiersSnapshot>
  activateModifier: (input: ActivateModifierInput) => Promise<ModifiersSnapshot>
}

export const modifiersApi: ModifiersApi = {
  getModifiers: () =>
    isHttpApiMode()
      ? httpClient.get<ModifiersSnapshot>('/modifiers')
      : modifiersMockApi.getModifiers(),
  activateModifier: (input) =>
    isHttpApiMode()
      ? httpClient.post<ModifiersSnapshot>('/modifiers/activate', input)
      : modifiersMockApi.activateModifier(input),
}
