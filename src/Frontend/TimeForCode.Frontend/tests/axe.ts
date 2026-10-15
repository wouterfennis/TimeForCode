import axe from 'axe-core'

/**
 * Runs the axe accessibility rules against a rendered container.
 * Returns one readable line per violation, so `toEqual([])` prints what is wrong.
 */
export async function axeViolations(container: Element): Promise<string[]> {
  const { violations } = await axe.run(container)
  return violations.map(
    (violation) =>
      `${violation.id}: ${violation.help} (${violation.nodes.map((node) => node.target.join(' ')).join(', ')})`,
  )
}
