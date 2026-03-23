import type { ActiveModifier, ModifierDefinition } from './types.ts'

// TODO: заменить mock на реальные вызовы (например, /api/modifiers и /api/modifiers/activate),
// чтобы модификаторы синхронизировались с backend и ботом.

const definitions: ModifierDefinition[] = [
  { id: 'petarda', name: 'петарда', cost: 3, description: '💥 Мощный взрыв на экране' },
  { id: 'steps', name: 'шаги', cost: 2, description: '👣 Кто-то крадётся за спиной' },
  { id: 'flash', name: 'вспышка', cost: 1, description: '⚡ Краткая вспышка экрана' },
]

let activeModifiers: ActiveModifier[] = []

export async function getModifiers(): Promise<{ available: ModifierDefinition[]; active: ActiveModifier[] }> {
  await new Promise((resolve) => setTimeout(resolve, 150))
  return {
    available: definitions,
    active: activeModifiers,
  }
}

export async function activateModifier(params: {
  modifierId: string
  triggeredBy: string
}): Promise<{ available: ModifierDefinition[]; active: ActiveModifier[] }> {
  const { modifierId, triggeredBy } = params

  const definition = definitions.find((m) => m.id === modifierId)
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


