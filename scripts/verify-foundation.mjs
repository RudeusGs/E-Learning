import fs from 'node:fs'
import path from 'node:path'
import process from 'node:process'

const root = process.cwd()
const errors = []
const warnings = []

const requiredFiles = [
  'README.md',
  'docs/PROJECT_SPEC.md',
  'docs/ARCHITECTURE.md',
  'docs/SECURITY.md',
  'docs/DATABASE.md',
  'docs/API_CONTRACT.md',
  'docs/ENGINEERING_STANDARDS.md',
  'docs/TESTING_AND_DOD.md',
  'docs/DECISIONS.md',
  'docs/HANDOVER.md',
  'Directory.Build.props',
  'Directory.Packages.props',
  'Elearning.sln',
  'docker-compose.yml',
  'package.json',
  'package-lock.json',
  'client/package.json',
  'server/src/Elearning.Api/Program.cs',
  '.config/dotnet-tools.json',

  'scripts/prepare-husky.mjs',
  'server/tests/Elearning.IntegrationTests/HealthEndpointTests.cs',
]

for (const relativePath of requiredFiles) {
  if (!fs.existsSync(path.join(root, relativePath))) {
    errors.push(`Missing required file: ${relativePath}`)
  }
}

for (const relativePath of ['package.json', 'client/package.json', '.config/dotnet-tools.json']) {
  try {
    JSON.parse(fs.readFileSync(path.join(root, relativePath), 'utf8'))
  } catch (error) {
    errors.push(`Invalid JSON: ${relativePath}: ${error.message}`)
  }
}

const stalePatterns = [
  ['cd frontend', 'legacy frontend directory'],
  ['cd backend', 'legacy backend directory'],
  ['docker compose up -d db', 'wrong PostgreSQL service name'],
  ['npm run test:unit', 'non-existent client test script'],
]

const markdownFiles = []
const walk = (directory) => {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === '.git' || entry.name === 'node_modules') continue
    const fullPath = path.join(directory, entry.name)
    if (entry.isDirectory()) walk(fullPath)
    else if (entry.name.endsWith('.md')) markdownFiles.push(fullPath)
  }
}
walk(root)

for (const filePath of markdownFiles) {
  const content = fs.readFileSync(filePath, 'utf8')
  for (const [pattern, reason] of stalePatterns) {
    if (content.includes(pattern)) {
      errors.push(`${path.relative(root, filePath)} contains ${reason}: ${pattern}`)
    }
  }

  for (const match of content.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const target = match[1].trim()
    if (!target || target.startsWith('#') || /^[a-z]+:/i.test(target)) continue
    const cleanTarget = target.split('#')[0]
    if (!cleanTarget) continue
    const resolved = path.resolve(path.dirname(filePath), cleanTarget)
    if (!fs.existsSync(resolved)) {
      errors.push(`Broken Markdown link in ${path.relative(root, filePath)}: ${target}`)
    }
  }
}

const programPath = path.join(root, 'server/src/Elearning.Api/Program.cs')
if (fs.existsSync(programPath)) {
  const program = fs.readFileSync(programPath, 'utf8')
  if (!program.includes('public partial class Program { }')) {
    errors.push(
      'Program.cs must expose `public partial class Program { }` for WebApplicationFactory.',
    )
  }
}

// Business files are expected to be added in future slices, so we no longer ban them here.

if (errors.length > 0) {
  console.error('Foundation verification failed:')
  for (const error of errors) console.error(`- ${error}`)
  process.exit(1)
}

for (const warning of warnings) console.warn(`Foundation warning: ${warning}`)
console.log('Foundation verification passed.')
