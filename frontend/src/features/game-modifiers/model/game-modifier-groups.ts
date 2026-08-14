import type {
  GameModifierActivation,
  GameModifierAvailability,
} from '../../../shared/api/contracts/index.ts'

const CATEGORY_ORDER = ['preparation', 'round', 'result'] as const

interface GroupedActiveModifier {
  modifierId: string
  modifierName: string
  activationCost: number
  activationsCount: number
  lastActivatedAtUtc: string
  lastActivatedByDisplayName: string
  activators: readonly GroupedModifierActivator[]
  activations: readonly GameModifierActivation[]
}

interface GroupedModifierActivator {
  userId: string
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
  locale?: string,
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
        activationCost: latestActivation.activationCost,
        activationsCount: sortedActivations.length,
        lastActivatedAtUtc: latestActivation.activatedAtUtc,
        lastActivatedByDisplayName: latestActivation.activatedByDisplayName,
        activators: groupModifierActivators(sortedActivations),
        activations: sortedActivations,
      }
    })
    .sort((left, right) => compareActiveModifierGroup(left, right, locale))
}

export function groupAvailableGameModifiers(
  items: readonly GameModifierAvailability[],
  locale?: string,
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

  return Array.from(groups.entries())
    .map(([category, categoryItems]) => ({
      category,
      items: [...categoryItems].sort((left, right) => compareAvailability(left, right, locale)),
    }))
    .sort(compareAvailabilityCategory)
}

function compareAvailability(
  left: GameModifierAvailability,
  right: GameModifierAvailability,
  locale?: string,
): number {
  const leftRank = getModifierAvailabilitySortRank(left)
  const rightRank = getModifierAvailabilitySortRank(right)

  if (leftRank !== rightRank) {
    return leftRank - rightRank
  }

  if (left.modifier.activationCost !== right.modifier.activationCost) {
    return left.modifier.activationCost - right.modifier.activationCost
  }

  return left.modifier.name.localeCompare(right.modifier.name, locale)
}

function compareActiveModifierGroup(
  left: GroupedActiveModifier,
  right: GroupedActiveModifier,
  locale?: string,
): number {
  if (left.activationCost !== right.activationCost) {
    return left.activationCost - right.activationCost
  }

  return left.modifierName.localeCompare(right.modifierName, locale)
}

function compareAvailabilityCategory(
  left: GroupedAvailableModifierCategory,
  right: GroupedAvailableModifierCategory,
): number {
  return CATEGORY_ORDER.indexOf(left.category) - CATEGORY_ORDER.indexOf(right.category)
}

function getModifierAvailabilitySortRank(availability: GameModifierAvailability): number {
  if (availability.canActivate) {
    return 0
  }

  if (
    availability.blockedReason === 'conflict_active' ||
    availability.blockedReason === 'limit_reached'
  ) {
    return 2
  }

  return 1
}

function groupModifierActivators(
  activations: readonly GameModifierActivation[],
): GroupedModifierActivator[] {
  const activators = new Map<string, GroupedModifierActivator>()

  for (const activation of activations) {
    const currentActivator = activators.get(activation.activatedByUserId)
    if (currentActivator) {
      currentActivator.activationsCount += 1
      if (activation.activatedAtUtc.localeCompare(currentActivator.lastActivatedAtUtc) > 0) {
        currentActivator.lastActivatedAtUtc = activation.activatedAtUtc
      }

      continue
    }

    activators.set(activation.activatedByUserId, {
      userId: activation.activatedByUserId,
      displayName: activation.activatedByDisplayName,
      activationsCount: 1,
      lastActivatedAtUtc: activation.activatedAtUtc,
    })
  }

  return Array.from(activators.values()).sort((left, right) =>
    right.lastActivatedAtUtc.localeCompare(left.lastActivatedAtUtc),
  )
}
