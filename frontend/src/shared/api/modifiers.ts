import * as modifiersMockApi from './modifiersMock.ts'
import type { ModifiersSnapshot } from './contracts.ts'

export interface ActivateModifierInput {
  modifierId: string
  triggeredBy: string
}

export interface ModifiersApi {
  getModifiers: () => Promise<ModifiersSnapshot>
  activateModifier: (input: ActivateModifierInput) => Promise<ModifiersSnapshot>
}

export const modifiersApi: ModifiersApi = {
  getModifiers: () => modifiersMockApi.getModifiers(),
  activateModifier: (input) => modifiersMockApi.activateModifier(input),
}
