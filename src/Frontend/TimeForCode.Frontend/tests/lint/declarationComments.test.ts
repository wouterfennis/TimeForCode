import { spawnSync } from 'node:child_process'
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'
import { afterAll, beforeAll, describe, expect, it } from 'vitest'

const RULE = 'timeforcode(doc-comment-above-declaration)'
const config = resolve(import.meta.dirname, '../../.oxlintrc.json')

const rejected = [
  ['function', '// Adds two numbers.\nexport function add(a: number, b: number): number {\n  return a + b\n}\n'],
  ['exported const', '// The default page size.\nexport const pageSize = 25\n'],
  ['type alias', '// A user id.\nexport type UserId = string\n'],
  ['interface', '// A user.\nexport interface User {\n  id: string\n}\n'],
  ['interface member', 'export interface User {\n  // The id.\n  id: string\n}\n'],
  ['class', '// A cache.\nexport class Cache {\n  size = 0\n}\n'],
  ['class method', 'export class Cache {\n  // Clears it.\n  clear(): void {}\n}\n'],
  ['multi-line comment', '// Adds two numbers,\n// slowly.\nexport function add(): void {}\n'],
  ['default export', '// Adds two numbers.\nexport default function add(): void {}\n'],
  ['ambient declaration', '// Adds two numbers.\ndeclare function add(): void\n'],
  ['function overload', '// Adds a number.\nexport function add(a: number): number\nexport function add(a: string): string\nexport function add(a: number | string): number | string {\n  return a\n}\n'],
  ['index signature', 'export interface Lookup {\n  // Any key.\n  [key: string]: number\n}\n'],
  ['decorated class member', 'declare function track(...args: unknown[]): void\nexport class Cache {\n  // Clears it.\n  @track\n  clear(): void {}\n}\n'],
] as const

const accepted = [
  ['a doc comment above a function', '/** Adds two numbers. */\nexport function add(a: number, b: number): number {\n  return a + b\n}\n'],
  ['a line comment inside a function body', 'export function add(a: number, b: number): number {\n  // Plain addition.\n  return a + b\n}\n'],
  ['a line comment above an object property', 'export const options = {\n  // Slow on purpose.\n  delay: 10,\n}\n'],
  ['a TODO marker', '// TODO(review): decide the name.\nexport function add(): void {}\n'],
  ['a line comment separated by a blank line', '// A section heading.\n\nexport function add(): void {}\n'],
  ['a line comment above an import', "// The side effect is the point.\nimport { add } from './add'\n\nexport const total = add\n"],
  ['a line comment above a local const', 'export function add(): number {\n  // The seed.\n  const seed = 1\n  return seed\n}\n'],
  ['an oxlint directive', '// oxlint-disable-next-line no-unused-vars\nexport function add(unused: number): void {}\n'],
  ['a TypeScript directive', 'declare const value: number\n// @ts-expect-error the string is on purpose\nexport const text: number = \'a\' + value\n'],
  ['a formatter directive', '// prettier-ignore\nexport const matrix = [1, 0, 0, 1]\n'],
] as const

const sampleDirectory = mkdtempSync(join(tmpdir(), 'declaration-comments-'))
const codesByFile = new Map<string, string[]>()

/** Writes every sample to its own file, then lints them all in one oxlint run with the repository's configuration. */
function lintSamples() {
  const samples = [...rejected, ...accepted].map(([, source], index) => ({ file: `sample-${index}.ts`, source }))
  for (const { file, source } of samples) {
    writeFileSync(join(sampleDirectory, file), source)
  }

  const result = spawnSync(`npx --no-install oxlint -c "${config}" --no-ignore -f json "${sampleDirectory}"`, {
    encoding: 'utf8',
    shell: true,
  })
  if (!result.stdout.startsWith('{')) {
    throw new Error(`oxlint gave no JSON report: ${result.stdout}${result.stderr}`)
  }
  const report = JSON.parse(result.stdout) as { diagnostics: { code: string; filename: string }[] }
  for (const { file } of samples) {
    codesByFile.set(
      file,
      report.diagnostics.filter((d) => d.filename.endsWith(file)).map((d) => d.code),
    )
  }
}

/** The rules oxlint reported for the sample at this position of the rejected and accepted lists. */
function codesOf(index: number) {
  return codesByFile.get(`sample-${index}.ts`)
}

beforeAll(lintSamples, 120_000)

afterAll(() => {
  rmSync(sampleDirectory, { recursive: true, force: true })
})

describe('doc-comment-above-declaration', () => {
  it.each(rejected.map(([name], index) => [name, index] as const))('rejects a line comment above a %s', (_, index) => {
    expect(codesOf(index)).toEqual([RULE])
  })

  it.each(accepted.map(([name], index) => [name, rejected.length + index] as const))('accepts %s', (_, index) => {
    expect(codesOf(index)).toEqual([])
  })
})
