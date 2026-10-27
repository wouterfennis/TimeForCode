import { render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { axeViolations } from '../../../tests/axe'
import { ProjectsPage } from './ProjectsPage'

const project = {
  id: '1',
  name: 'widgets',
  fullName: 'acme/widgets',
  description: 'Widgets for everyone',
  githubUrl: 'https://github.com/acme/widgets',
  language: 'C#',
  ownerLogin: 'acme',
  ownerAvatarUrl: 'https://avatars.example/acme.png',
}

function mockApi(response: () => Promise<Response>) {
  const fetchMock = vi.fn<(request: Request) => Promise<Response>>(() => response())
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve(Response.json(body, { status }))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ProjectsPage', () => {
  it('shows a loading message while the projects are requested', () => {
    mockApi(() => new Promise<Response>(() => {}))

    render(<ProjectsPage />)

    expect(screen.getByRole('status')).toHaveTextContent('Loading projects…')
  })

  it('lists the active projects returned by the API', async () => {
    mockApi(() =>
      jsonResponse({
        projects: [project, { ...project, id: '2', name: 'gadgets', fullName: 'acme/gadgets', description: null, language: null }],
        totalCount: 2,
        pageNumber: 1,
        pageSize: 20,
      }),
    )

    render(<ProjectsPage />)

    const items = within(await screen.findByRole('list', { name: 'Projects' })).getAllByRole('listitem')
    expect(items).toHaveLength(2)
    expect(within(items[0]!).getByRole('link', { name: 'acme/widgets' })).toHaveAttribute('href', project.githubUrl)
    expect(items[0]).toHaveTextContent('Widgets for everyone')
    expect(items[0]).toHaveTextContent('C#')
    expect(items[1]).toHaveTextContent('acme/gadgets')
  })

  it('requests the projects from the front-end origin only', async () => {
    const fetchMock = mockApi(() => jsonResponse({ projects: [], totalCount: 0, pageNumber: 1, pageSize: 20 }))

    render(<ProjectsPage />)
    await screen.findByText('There are no active projects yet.')

    const url = new URL(fetchMock.mock.calls[0]![0].url)
    expect(url.origin).toBe(window.location.origin)
    expect(url.pathname).toBe('/api/donation/v1/Project')
  })

  it('shows an empty state when there are no active projects', async () => {
    mockApi(() => jsonResponse({ projects: [], totalCount: 0, pageNumber: 1, pageSize: 20 }))

    render(<ProjectsPage />)

    expect(await screen.findByText('There are no active projects yet.')).toBeInTheDocument()
    expect(screen.queryByRole('list', { name: 'Projects' })).not.toBeInTheDocument()
  })

  it('shows an error when the API answers with a failure', async () => {
    mockApi(() => jsonResponse({ title: 'Boom' }, 500))

    render(<ProjectsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('The projects could not be loaded. Try again later.')
  })

  it('shows an error when the request cannot be made', async () => {
    mockApi(() => Promise.reject(new TypeError('Failed to fetch')))

    render(<ProjectsPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('The projects could not be loaded. Try again later.')
  })

  it('has no automated accessibility violations in the list state', async () => {
    mockApi(() => jsonResponse({ projects: [project], totalCount: 1, pageNumber: 1, pageSize: 20 }))

    const { container } = render(<ProjectsPage />)
    await screen.findByRole('list', { name: 'Projects' })

    expect(await axeViolations(container)).toEqual([])
  })
})
