export const modifierScoreFormulaModes = [
  'flat_per_kill',
  'stacking_per_kill_bonus',
  'custom_expression',
] as const

export type ModifierScoreFormulaMode = (typeof modifierScoreFormulaModes)[number]

export function isCustomModifierScoreFormula(mode: ModifierScoreFormulaMode | null | undefined) {
  return mode === 'custom_expression'
}
