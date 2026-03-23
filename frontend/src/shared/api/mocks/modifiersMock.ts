import type { ActiveModifier, ModifierDefinition, ModifiersSnapshot } from '../contracts/index.ts'

const definitions: ModifierDefinition[] = [
  { id: 'petarda', name: 'петарда', cost: 3, description: '💥 Мощный взрыв на экране' },
  { id: 'steps', name: 'шаги', cost: 2, description: '👣 Кто-то крадётся за спиной' },
  { id: 'flash', name: 'вспышка', cost: 1, description: '⚡ Краткая вспышка экрана' },
]

let activeModifiers: ActiveModifier[] = []

export async function getModifiers(): Promise<ModifiersSnapshot> {
  await new Promise((resolve) => setTimeout(resolve, 150))
  return {
    available: definitions,
    active: activeModifiers,
  }
}

export async function activateModifier(params: {
  modifierId: string
  triggeredBy: string
}): Promise<ModifiersSnapshot> {
  const { modifierId, triggeredBy } = params

  const definition = definitions.find((modifier) => modifier.id === modifierId)
  if (!definition) {
    return getModifiers()
  }

  activeModifiers = [
    {
      id: `${modifierId}-${Date.now()}`,
      modifierId,
      activatedAt: new Date().toISOString(),
      triggeredBy,
    },
    ...activeModifiers,
  ].slice(0, 10)

  return getModifiers()
}
