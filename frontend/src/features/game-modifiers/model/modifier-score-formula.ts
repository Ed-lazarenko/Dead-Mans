export const modifierScoreFormulaModes = [
  'flat_per_kill',
  'stacking_per_kill_bonus',
  'custom_expression',
] as const

export type ModifierScoreFormulaMode = (typeof modifierScoreFormulaModes)[number]

interface ModifierScoreFormulaDefinition {
  mode: ModifierScoreFormulaMode
  successExpression: string | null
  failureExpression: string | null
}

interface ModifierScoreFormulaContext {
  killsCount: number
  bountyCount: number
  scoreUnit: number
  baseScore: number
  perKillBonus: number
  failurePenaltyPoints: number
  activationCount: number
  totalOutcomeCount: number
}

const supportedFunctions = new Set(['min', 'max', 'round', 'floor', 'ceil', 'abs'])
const supportedVariables = new Set([
  'killscount',
  'bountycount',
  'scoreunit',
  'basescore',
  'perkillbonus',
  'failurepenaltypoints',
  'activationcount',
  'totaloutcomecount',
])

type FormulaToken =
  | { type: 'number'; value: number }
  | { type: 'identifier'; value: string }
  | { type: 'operator'; value: '+' | '-' | '*' | '/' }
  | { type: 'paren'; value: '(' | ')' }
  | { type: 'comma' }

export function isCustomModifierScoreFormula(mode: ModifierScoreFormulaMode | null | undefined) {
  return mode === 'custom_expression'
}

export function evaluateModifierScoreFormulaSuccess(
  formula: ModifierScoreFormulaDefinition,
  context: ModifierScoreFormulaContext,
) {
  switch (formula.mode) {
    case 'stacking_per_kill_bonus':
      return context.killsCount * context.perKillBonus * context.killsCount
    case 'custom_expression':
      return formula.successExpression
        ? evaluateModifierScoreExpression(formula.successExpression, context)
        : 0
    case 'flat_per_kill':
    default:
      return context.killsCount * context.perKillBonus
  }
}

export function evaluateModifierScoreFormulaFailure(
  formula: ModifierScoreFormulaDefinition,
  context: ModifierScoreFormulaContext,
) {
  if (formula.mode !== 'custom_expression' || !formula.failureExpression) {
    return null
  }

  return evaluateModifierScoreExpression(formula.failureExpression, context)
}

export function evaluateModifierScoreExpression(
  expression: string,
  context: ModifierScoreFormulaContext,
) {
  const tokens = tokenizeFormulaExpression(expression)
  let cursor = 0

  const parseExpression = (): number => {
    let value = parseTerm()

    while (cursor < tokens.length) {
      const token = tokens[cursor]
      if (token?.type !== 'operator' || (token.value !== '+' && token.value !== '-')) {
        break
      }

      cursor += 1
      const right = parseTerm()
      value = token.value === '+' ? value + right : value - right
    }

    return value
  }

  const parseTerm = (): number => {
    let value = parseUnary()

    while (cursor < tokens.length) {
      const token = tokens[cursor]
      if (token?.type !== 'operator' || (token.value !== '*' && token.value !== '/')) {
        break
      }

      cursor += 1
      const right = parseUnary()

      if (token.value === '*') {
        value *= right
      } else {
        if (right === 0) {
          throw new Error('Division by zero is not allowed in modifier formulas.')
        }

        value /= right
      }
    }

    return value
  }

  const parseUnary = (): number => {
    const token = tokens[cursor]
    if (token?.type === 'operator' && (token.value === '+' || token.value === '-')) {
      cursor += 1
      const value = parseUnary()
      return token.value === '-' ? -value : value
    }

    return parsePrimary()
  }

  const parsePrimary = (): number => {
    const token = tokens[cursor]
    if (!token) {
      throw new Error('Unexpected end of formula.')
    }

    if (token.type === 'number') {
      cursor += 1
      return token.value
    }

    if (token.type === 'identifier') {
      cursor += 1
      const identifier = token.value
      const next = tokens[cursor]

      if (next?.type === 'paren' && next.value === '(') {
        cursor += 1
        const args: number[] = []

        if (!(tokens[cursor]?.type === 'paren' && tokens[cursor]?.value === ')')) {
          while (true) {
            args.push(parseExpression())

            if (tokens[cursor]?.type === 'comma') {
              cursor += 1
              continue
            }

            break
          }
        }

        const closing = tokens[cursor]
        if (closing?.type !== 'paren' || closing.value !== ')') {
          throw new Error('Expected closing parenthesis in formula function call.')
        }

        cursor += 1
        return executeFormulaFunction(identifier, args)
      }

      return resolveFormulaVariable(identifier, context)
    }

    if (token.type === 'paren' && token.value === '(') {
      cursor += 1
      const value = parseExpression()
      const closing = tokens[cursor]
      if (closing?.type !== 'paren' || closing.value !== ')') {
        throw new Error('Expected closing parenthesis in formula expression.')
      }

      cursor += 1
      return value
    }

    throw new Error('Unexpected token in modifier formula.')
  }

  const value = parseExpression()
  if (cursor < tokens.length) {
    throw new Error('Formula contains unexpected trailing tokens.')
  }

  return value
}

export function validateModifierScoreExpressionSyntax(expression: string) {
  evaluateModifierScoreExpression(expression, {
    killsCount: 1,
    bountyCount: 1,
    scoreUnit: 1,
    baseScore: 1,
    perKillBonus: 1,
    failurePenaltyPoints: 1,
    activationCount: 1,
    totalOutcomeCount: 2,
  })
}

function tokenizeFormulaExpression(expression: string) {
  const tokens: FormulaToken[] = []
  let cursor = 0
  const normalized = expression.trim()

  while (cursor < normalized.length) {
    const char = normalized[cursor]

    if (!char) {
      break
    }

    if (/\s/.test(char)) {
      cursor += 1
      continue
    }

    if (/[0-9.]/.test(char)) {
      let end = cursor + 1
      while (end < normalized.length && /[0-9.]/.test(normalized[end] ?? '')) {
        end += 1
      }

      const value = Number.parseFloat(normalized.slice(cursor, end))
      if (!Number.isFinite(value)) {
        throw new Error('Invalid number in modifier formula.')
      }

      tokens.push({ type: 'number', value })
      cursor = end
      continue
    }

    if (/[A-Za-z_]/.test(char)) {
      let end = cursor + 1
      while (end < normalized.length && /[A-Za-z0-9_]/.test(normalized[end] ?? '')) {
        end += 1
      }

      tokens.push({
        type: 'identifier',
        value: normalized.slice(cursor, end).toLowerCase(),
      })
      cursor = end
      continue
    }

    if (char === '+' || char === '-' || char === '*' || char === '/') {
      tokens.push({ type: 'operator', value: char })
      cursor += 1
      continue
    }

    if (char === '(' || char === ')') {
      tokens.push({ type: 'paren', value: char })
      cursor += 1
      continue
    }

    if (char === ',') {
      tokens.push({ type: 'comma' })
      cursor += 1
      continue
    }

    throw new Error(`Unsupported character "${char}" in modifier formula.`)
  }

  return tokens
}

function resolveFormulaVariable(variableName: string, context: ModifierScoreFormulaContext) {
  if (!supportedVariables.has(variableName)) {
    throw new Error(`Unsupported variable "${variableName}" in modifier formula.`)
  }

  switch (variableName) {
    case 'killscount':
      return context.killsCount
    case 'bountycount':
      return context.bountyCount
    case 'scoreunit':
      return context.scoreUnit
    case 'basescore':
      return context.baseScore
    case 'perkillbonus':
      return context.perKillBonus
    case 'failurepenaltypoints':
      return context.failurePenaltyPoints
    case 'activationcount':
      return context.activationCount
    case 'totaloutcomecount':
      return context.totalOutcomeCount
    default:
      return 0
  }
}

function executeFormulaFunction(functionName: string, args: number[]) {
  if (!supportedFunctions.has(functionName)) {
    throw new Error(`Unsupported function "${functionName}" in modifier formula.`)
  }

  switch (functionName) {
    case 'min':
      if (args.length < 2) {
        throw new Error('Function min requires at least two arguments.')
      }
      return Math.min(...args)
    case 'max':
      if (args.length < 2) {
        throw new Error('Function max requires at least two arguments.')
      }
      return Math.max(...args)
    case 'round':
      if (args.length !== 1) {
        throw new Error('Function round requires exactly one argument.')
      }
      return Math.round(args[0] ?? 0)
    case 'floor':
      if (args.length !== 1) {
        throw new Error('Function floor requires exactly one argument.')
      }
      return Math.floor(args[0] ?? 0)
    case 'ceil':
      if (args.length !== 1) {
        throw new Error('Function ceil requires exactly one argument.')
      }
      return Math.ceil(args[0] ?? 0)
    case 'abs':
      if (args.length !== 1) {
        throw new Error('Function abs requires exactly one argument.')
      }
      return Math.abs(args[0] ?? 0)
    default:
      return 0
  }
}
