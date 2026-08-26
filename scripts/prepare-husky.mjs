import fs from 'node:fs'
import { spawnSync } from 'node:child_process'
import process from 'node:process'

if (process.env.HUSKY === '0') {
  process.exit(0)
}

if (!fs.existsSync('.git')) {
  console.log('Husky setup skipped: .git directory is not present yet.')
  process.exit(0)
}

const result = spawnSync('husky', [], {
  stdio: 'inherit',
  shell: process.platform === 'win32',
})

if (result.error) {
  console.error(`Unable to initialize Husky: ${result.error.message}`)
  process.exit(1)
}

process.exit(result.status ?? 1)
