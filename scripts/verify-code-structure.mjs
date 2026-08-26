import { readdir, readFile } from 'node:fs/promises'
import path from 'node:path'

const repositoryRoot = path.resolve(import.meta.dirname, '..')
const violations = []

async function filesBelow(directory, extension) {
  const entries = await readdir(directory, { withFileTypes: true })
  const nested = await Promise.all(
    entries
      .filter((entry) => !['bin', 'obj', 'Migrations'].includes(entry.name))
      .map(async (entry) => {
        const absolute = path.join(directory, entry.name)
        if (entry.isDirectory()) return filesBelow(absolute, extension)
        return entry.isFile() && entry.name.endsWith(extension) ? [absolute] : []
      }),
  )
  return nested.flat()
}

async function checkCSharpTypes() {
  const roots = [
    path.join(repositoryRoot, 'server', 'src', 'Elearning.Domain'),
    path.join(repositoryRoot, 'server', 'src', 'Elearning.Application'),
  ]
  for (const root of roots) {
    for (const file of await filesBelow(root, '.cs')) {
      const source = await readFile(file, 'utf8')
      const declarations = [
        ...source.matchAll(
          /public\s+(?:sealed\s+|abstract\s+|partial\s+)*(?:class|interface|enum|record(?:\s+struct)?)\s+(\w+)/g,
        ),
      ].map((match) => match[1])
      if (declarations.length > 1)
        violations.push(`${file}: multiple public types (${declarations.join(', ')})`)
      if (declarations.length === 1 && path.basename(file, '.cs') !== declarations[0]) {
        violations.push(`${file}: filename must match public type ${declarations[0]}`)
      }
    }
  }
}

async function checkTypeScriptModels() {
  const roots = [
    path.join(repositoryRoot, 'client', 'src', 'types'),
    path.join(repositoryRoot, 'client', 'src', 'modules'),
  ]
  for (const root of roots) {
    for (const file of await filesBelow(root, '.ts')) {
      if (
        !file.includes(`${path.sep}models${path.sep}`) &&
        !file.includes(`${path.sep}types${path.sep}`)
      )
        continue
      const source = await readFile(file, 'utf8')
      const declarations = [
        ...source.matchAll(/export\s+(?:interface|type|enum|class)\s+(\w+)/g),
      ].map((match) => match[1])
      if (declarations.length > 1)
        violations.push(`${file}: multiple exported models (${declarations.join(', ')})`)
      if (declarations.length === 1 && path.basename(file, '.ts') !== declarations[0]) {
        violations.push(`${file}: filename must match exported model ${declarations[0]}`)
      }
    }
  }
}

await checkCSharpTypes()
await checkTypeScriptModels()

if (violations.length > 0) {
  console.error(violations.join('\n'))
  process.exitCode = 1
} else {
  console.log('Code structure verification passed: one public model/type per file.')
}
