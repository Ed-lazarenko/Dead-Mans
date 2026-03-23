import { httpClient } from './client/httpClient.ts'
import type { ActivateModifierInput, ModifiersSnapshot } from './contracts/index.ts'
import * as modifiersMockApi from './mocks/modifiersMock.ts'

export interface ModifiersApi {
  getModifiers: () => Promise<ModifiersSnapshot>
  activateModifier: (input: ActivateModifierInput) => Promise<ModifiersSnapshot>
}

function shouldUseMockApi() {
  return (import.meta.env.VITE_API_MODE ?? 'mock') !== 'http'
}

export const modifiersApi: ModifiersApi = {
  getModifiers: () =>
    shouldUseMockApi()
      ? modifiersMockApi.getModifiers()
      : httpClient.get<ModifiersSnapshot>('/modifiers'),
  activateModifier: (input) =>
    shouldUseMockApi()
      ? modifiersMockApi.activateModifier(input)
      : httpClient.post<ModifiersSnapshot>('/modifiers/activate', input),
}
