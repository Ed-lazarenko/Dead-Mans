import type {
  GameModifierActivation,
  GameModifierAvailability,
} from '../../../shared/api/contracts/index.ts'

interface GroupedActiveModifier {
  modifierId: string
  modifierName: string
  activationsCount: number
  totalActivationCost: number
  lastActivatedAtUtc: string
  lastActivatedByDisplayName: string
  activators: readonly GroupedModifierActivator[]
  activations: readonly GameModifierActivation[]
}

interface GroupedModifierActivator {
  displayName: string
  activationsCount: number
  lastActivatedAtUtc: string
}

interface GroupedAvailableModifierCategory {
  category: GameModifierAvailability['modifier']['category']
  items: readonly GameModifierAvailability[]
}

export function groupActiveGameModifiers(
  activations: readonly GameModifierActivation[],
): GroupedActiveModifier[] {
  const groups = new Map<string, GameModifierActivation[]>()

  for (const activation of activations) {
    const currentGroup = groups.get(activation.modifierId)
    if (currentGroup) {
      currentGroup.push(activation)
      continue
    }

    groups.set(activation.modifierId, [activation])
  }

  return Array.from(groups.entries())
    .map(([modifierId, groupActivations]) => {
      const sortedActivations = [...groupActivations].sort((left, right) =>
        right.activatedAtUtc.localeCompare(left.activatedAtUtc),
      )
      const latestActivation = sortedActivations[0]

      if (!latestActivation) {
        throw new Error(`Modifier activation group "${modifierId}" is empty`)
      }

      return {
        modifierId,
        modifierName: latestActivation.modifierName,
        activationsCount: sortedActivations.length,
        totalActivationCost: sortedActivations.reduce(
          (total, activation) => total + activation.activationCost,
          0,
        ),
        lastActivatedAtUtc: latestActivation.activatedAtUtc,
        lastActivatedByDisplayName: latestActivation.activatedByDisplayName,
        activators: groupModifierActivators(sortedActivations),
        activations: sortedActivations,
      }
    })
    .sort((left, right) => right.lastActivatedAtUtc.localeCompare(left.lastActivatedAtUtc))
}

export function groupAvailableGameModifiers(
  items: readonly GameModifierAvailability[],
): GroupedAvailableModifierCategory[] {
  const groups = new Map<
    GameModifierAvailability['modifier']['category'],
    GameModifierAvailability[]
  >()

  for (const item of items) {
    const currentGroup = groups.get(item.modifier.category)
    if (currentGroup) {
      currentGroup.push(item)
      continue
    }

    groups.set(item.modifier.category, [item])
  }

  return Array.from(groups.entries()).map(([category, categoryItems]) => ({
    category,
    items: [...categoryItems].sort(compareAvailability),
  }))
}

function compareAvailability(
  left: GameModifierAvailability,
  right: GameModifierAvailability,
): number {
  if (left.canActivate !== right.canActivate) {
    return left.canActivate ? -1 : 1
  }

  if (left.modifier.activationCost !== right.modifier.activationCost) {
    return left.modifier.activationCost - right.modifier.activationCost
  }

  return left.modifier.name.localeCompare(right.modifier.name)
}

function groupModifierActivators(
  activations: readonly GameModifierActivation[],
): GroupedModifierActivator[] {
  const activators = new Map<string, GroupedModifierActivator>()

  for (const activation of activations) {
    const currentActivator = activators.get(activation.activatedByDisplayName)
    if (currentActivator) {
      currentActivator.activationsCount += 1
      if (activation.activatedAtUtc.localeCompare(currentActivator.lastActivatedAtUtc) > 0) {
        currentActivator.lastActivatedAtUtc = activation.activatedAtUtc
      }

      continue
    }

    activators.set(activation.activatedByDisplayName, {
      displayName: activation.activatedByDisplayName,
      activationsCount: 1,
      lastActivatedAtUtc: activation.activatedAtUtc,
    })
  }

  return Array.from(activators.values()).sort((left, right) =>
    right.lastActivatedAtUtc.localeCompare(left.lastActivatedAtUtc),
  )
}
