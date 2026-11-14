import { render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { axeViolations } from '../../tests/axe'
import { App } from './App'

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn((input: RequestInfo | URL) => {
      const url = input instanceof Request ? input.url : String(input)
      return Promise.resolve(
        url.includes('/bff/session')
          ? new Response(null, { status: 401 })
          : Response.json({ projects: [], totalCount: 0, pageNumber: 1, pageSize: 20 }),
      )
    }),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('App', () => {
  it('shows the title and the project list in the main landmark', async () => {
    render(<App />)

    const main = screen.getByRole('main')
    expect(main).toContainElement(screen.getByRole('heading', { level: 1, name: 'TimeForCode' }))
    expect(main).toContainElement(await screen.findByText('There are no active projects yet.'))
  })

  it('has no automated accessibility violations', async () => {
    const { container } = render(<App />)
    await screen.findByText('There are no active projects yet.')

    expect(await axeViolations(container)).toEqual([])
  })
})
