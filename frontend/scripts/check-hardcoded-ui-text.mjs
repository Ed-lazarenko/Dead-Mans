import fs from 'node:fs/promises'
import path from 'node:path'
import ts from 'typescript'

const sourceDir = path.resolve(process.cwd(), 'src')
const userFacingPropertyNames = new Set([
  'accessibilitylabel',
  'actionlabel',
  'alt',
  'aria-description',
  'aria-label',
  'cancellabel',
  'caption',
  'confirmlabel',
  'description',
  'defaultvalue',
  'emptylabel',
  'emptymessage',
  'errorlabel',
  'errormessage',
  'fallbacklabel',
  'helpertext',
  'hint',
  'label',
  'loadingmessage',
  'message',
  'nooptionstext',
  'placeholder',
  'statuslabel',
  'subtitle',
  'summary',
  'text',
  'title',
  'tooltip',
])

function hasHumanLanguage(value) {
  return /\p{L}/u.test(value)
}

function hasCyrillic(value) {
  return /\p{Script=Cyrillic}/u.test(value)
}

function normalizeText(value) {
  return value.replace(/\s+/g, ' ').trim()
}

function isTranslationKey(value) {
  return /^[a-z][\w-]*(?:\.[\w-]+)+$/i.test(value)
}

function shouldReport(value) {
  const normalized = normalizeText(value)
  return normalized.length > 0 && hasHumanLanguage(normalized) && !isTranslationKey(normalized)
}

function isInsideJsxAttribute(node) {
  let current = node.parent

  while (current && !ts.isJsxElement(current) && !ts.isJsxSelfClosingElement(current)) {
    if (ts.isJsxAttribute(current)) {
      return true
    }
    current = current.parent
  }

  return false
}

function getLiteralText(node) {
  if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) {
    return node.text
  }

  if (ts.isTemplateExpression(node)) {
    return [node.head.text, ...node.templateSpans.map((span) => span.literal.text)].join(' ')
  }

  return null
}

function collectDisplayLiterals(node) {
  const directValue = getLiteralText(node)
  if (directValue !== null) {
    return [{ node, value: directValue }]
  }

  if (ts.isConditionalExpression(node)) {
    return [...collectDisplayLiterals(node.whenTrue), ...collectDisplayLiterals(node.whenFalse)]
  }

  if (
    ts.isBinaryExpression(node) &&
    [
      ts.SyntaxKind.PlusToken,
      ts.SyntaxKind.AmpersandAmpersandToken,
      ts.SyntaxKind.BarBarToken,
      ts.SyntaxKind.QuestionQuestionToken,
    ].includes(node.operatorToken.kind)
  ) {
    return [...collectDisplayLiterals(node.left), ...collectDisplayLiterals(node.right)]
  }

  if (
    ts.isParenthesizedExpression(node) ||
    ts.isAsExpression(node) ||
    ts.isSatisfiesExpression(node)
  ) {
    return collectDisplayLiterals(node.expression)
  }

  return []
}

function getPropertyName(node) {
  if (!node.name) {
    return null
  }

  if (ts.isIdentifier(node.name) || ts.isStringLiteral(node.name)) {
    return node.name.text.toLowerCase()
  }

  return null
}

function getLineAndColumn(sourceFile, node) {
  const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile))
  return { line: position.line + 1, column: position.character + 1 }
}

function addFinding(findings, sourceFile, node, kind, value) {
  if (!shouldReport(value)) {
    return
  }

  const { line, column } = getLineAndColumn(sourceFile, node)
  findings.push({
    file: path.relative(process.cwd(), sourceFile.fileName),
    line,
    column,
    kind,
    value: normalizeText(value),
  })
}

function inspectSourceFile(sourceFile) {
  const findings = []

  function visit(node) {
    if (ts.isJsxText(node)) {
      addFinding(findings, sourceFile, node, 'JSX text', node.text)
    } else if (ts.isJsxAttribute(node)) {
      const propertyName = node.name.getText(sourceFile).toLowerCase()
      if (userFacingPropertyNames.has(propertyName) && node.initializer) {
        if (ts.isStringLiteral(node.initializer)) {
          addFinding(findings, sourceFile, node, `JSX ${propertyName}`, node.initializer.text)
        } else if (ts.isJsxExpression(node.initializer) && node.initializer.expression) {
          for (const literal of collectDisplayLiterals(node.initializer.expression)) {
            addFinding(findings, sourceFile, literal.node, `JSX ${propertyName}`, literal.value)
          }
        }
      }
    } else if (ts.isJsxExpression(node) && node.expression && !isInsideJsxAttribute(node)) {
      for (const literal of collectDisplayLiterals(node.expression)) {
        addFinding(findings, sourceFile, literal.node, 'JSX expression', literal.value)
      }
    } else if (ts.isPropertyAssignment(node)) {
      const propertyName = getPropertyName(node)
      if (propertyName && userFacingPropertyNames.has(propertyName)) {
        for (const literal of collectDisplayLiterals(node.initializer)) {
          addFinding(findings, sourceFile, literal.node, `property ${propertyName}`, literal.value)
        }
      }
    } else if (
      ts.isCallExpression(node) &&
      node.arguments.length === 0 &&
      ts.isPropertyAccessExpression(node.expression) &&
      ['toLocaleDateString', 'toLocaleString', 'toLocaleTimeString'].includes(
        node.expression.name.text,
      )
    ) {
      addFinding(
        findings,
        sourceFile,
        node,
        'locale-sensitive formatting without an explicit locale',
        node.expression.name.text,
      )
    }

    if (
      (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) &&
      hasCyrillic(node.text)
    ) {
      addFinding(findings, sourceFile, node, 'Cyrillic literal outside translations', node.text)
    }

    ts.forEachChild(node, visit)
  }

  visit(sourceFile)
  return findings
}

async function findSourceFiles(directory) {
  const entries = await fs.readdir(directory, { withFileTypes: true })
  const files = []

  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name)
    if (entry.isDirectory()) {
      files.push(...(await findSourceFiles(entryPath)))
      continue
    }

    if (
      entry.isFile() &&
      /\.tsx?$/.test(entry.name) &&
      !entry.name.endsWith('.test.ts') &&
      !entry.name.endsWith('.test.tsx') &&
      !entry.name.endsWith('.d.ts') &&
      !entry.name.endsWith('-translations.ts')
    ) {
      files.push(entryPath)
    }
  }

  return files.sort()
}

async function main() {
  const sourceFiles = await findSourceFiles(sourceDir)
  const findings = []

  for (const filePath of sourceFiles) {
    const source = await fs.readFile(filePath, 'utf8')
    const scriptKind = filePath.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS
    const sourceFile = ts.createSourceFile(
      filePath,
      source,
      ts.ScriptTarget.Latest,
      true,
      scriptKind,
    )
    findings.push(...inspectSourceFile(sourceFile))
  }

  if (findings.length === 0) {
    console.log(`No hardcoded UI text found in ${sourceFiles.length} production source files.`)
    return
  }

  console.error(`Found ${findings.length} hardcoded UI text candidate(s):`)
  for (const finding of findings) {
    console.error(
      `  ${finding.file}:${finding.line}:${finding.column} [${finding.kind}] ${JSON.stringify(finding.value)}`,
    )
  }
  process.exitCode = 1
}

await main()
