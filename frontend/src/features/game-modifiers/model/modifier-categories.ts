export const modifierCategoryCodes = ['preparation', 'round', 'result'] as const

export type ModifierCategoryCode = (typeof modifierCategoryCodes)[number]
