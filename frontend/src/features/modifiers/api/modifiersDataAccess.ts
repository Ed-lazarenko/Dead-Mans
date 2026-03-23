import { modifiersApi } from '../../../shared/api/modifiers.ts'

export function getModifiersSnapshot() {
  return modifiersApi.getModifiers()
}

export function activateModifier(modifierId: string, triggeredBy: string) {
  return modifiersApi.activateModifier({ modifierId, triggeredBy })
}
