import { httpClient } from './client/httpClient.ts'
import type { ActivateModifierInput, ModifiersSnapshot } from './contracts/index.ts'

export interface ModifiersApi {
  getModifiers: () => Promise<ModifiersSnapshot>
  activateModifier: (input: ActivateModifierInput) => Promise<ModifiersSnapshot>
}

export const modifiersApi: ModifiersApi = {
  getModifiers: () => httpClient.get<ModifiersSnapshot>('/modifiers'),
  activateModifier: (input) => httpClient.post<ModifiersSnapshot>('/modifiers/activate', input),
}
