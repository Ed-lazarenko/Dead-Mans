import fs from 'node:fs/promises'
import path from 'node:path'
import ts from 'typescript'

const sourceDir = path.resolve(process.cwd(), 'src')

async function findModules(directory) {
  const entries = await fs.readdir(directory, { withFileTypes: true })
  const modules = []
  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name)
    if (entry.isDirectory()) modules.push(...(await findModules(entryPath)))
    else if (entry.name.endsWith('-translations.ts')) modules.push(entryPath)
  }
  return modules.sort()
}

async function loadModule(filePath) {
  const source = await fs.readFile(filePath, 'utf8')
  const code = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ES2022, target: ts.ScriptTarget.ES2022 },
  }).outputText
  return (await import(`data:text/javascript;base64,${Buffer.from(code).toString('base64')}`))
    .default
}

function flatten(value, prefix = '', result = []) {
  for (const [key, nested] of Object.entries(value)) {
    const pathKey = prefix ? `${prefix}.${key}` : key
    if (typeof nested === 'string') result.push([pathKey, nested])
    else if (nested && typeof nested === 'object') flatten(nested, pathKey, result)
  }
  return result
}

const allValues = new Map()
for (const filePath of await findModules(sourceDir)) {
  const translations = await loadModule(filePath)
  const moduleName = path.basename(filePath, '-translations.ts')
  const localeValues = Object.fromEntries(
    Object.entries(translations).map(([language, locale]) => [language, new Map(flatten(locale))]),
  )
  for (const [key, value] of localeValues.en) {
    const tuple = ['en', 'ru', 'uk', 'pl'].map((language) =>
      localeValues[language].get(key).trim().replace(/\s+/g, ' '),
    )
    const tupleKey = JSON.stringify(tuple)
    const group = allValues.get(tupleKey) ?? { values: tuple, keys: [] }
    group.keys.push(`${moduleName}.${key}`)
    allValues.set(tupleKey, group)
  }
}

const duplicates = [...allValues.entries()]
  .map(([, group]) => group)
  .filter(({ keys }) => keys.length > 1)
  .sort(
    (left, right) =>
      right.keys.length - left.keys.length || left.values[0].localeCompare(right.values[0]),
  )

for (const { values, keys } of duplicates) {
  console.log(JSON.stringify(values))
  for (const key of keys) console.log(`  ${key}`)
}
console.log(`Duplicate values: ${duplicates.length}`)
