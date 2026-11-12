import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { axeViolations } from '../../../tests/axe'
import { SessionControl } from './SessionControl'

const signedIn = { signedIn: true, login: 'octocat', avatarUrl: 'https://avatars.example/octocat.png', isAdmin: false }
const anonymous = { signedIn: false, login: null, avatarUrl: null, isAdmin: false }

type Answer = (request: Request) => Promise<Response>

function mockApi(answer: Answer) {
  const fetchMock = vi.fn<Answer>((request) => answer(request))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function json(body: unknown, status = 200) {
  return Promise.resolve(Response.json(body, { status }))
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('SessionControl', () => {
  it('offers an anonymous visitor to sign in with GitHub, returning to this site', async () => {
    mockApi(() => json(anonymous))

    render(<SessionControl />)

    const link = await screen.findByRole('link', { name: 'Sign in with GitHub' })
    const href = new URL(link.getAttribute('href')!, window.location.origin)
    expect(href.origin).toBe(window.location.origin)
    expect(href.pathname).toBe('/api/auth/v1/Authentication/login')
    expect(href.searchParams.get('IdentityProvider')).toBe('Github')
    expect(href.searchParams.get('RedirectUri')).toBe(window.location.origin)
  })

  it('asks the front-end origin who is signed in', async () => {
    const fetchMock = mockApi(() => json(anonymous))

    render(<SessionControl />)
    await screen.findByRole('link', { name: 'Sign in with GitHub' })

    const url = new URL(fetchMock.mock.calls[0]![0].url)
    expect(url.origin).toBe(window.location.origin)
    expect(url.pathname).toBe('/api/session')
  })

  it('shows the signed-in user with a sign-out action', async () => {
    mockApi(() => json(signedIn))

    render(<SessionControl />)

    expect(await screen.findByText('Signed in as octocat')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Sign in with GitHub' })).not.toBeInTheDocument()
  })

  it('signs out through the front-end with the CSRF header and returns to the anonymous state', async () => {
    const fetchMock = mockApi((request) =>
      new URL(request.url).pathname === '/api/session' ? json(signedIn) : Promise.resolve(new Response(null, { status: 204 })),
    )
    render(<SessionControl />)

    fireEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('link', { name: 'Sign in with GitHub' })).toBeInTheDocument()
    const logout = fetchMock.mock.calls.map(([request]) => request).find((request) => request.method === 'POST')!
    expect(new URL(logout.url).pathname).toBe('/api/auth/v1/Authentication/logout')
    expect(logout.headers.get('X-Requested-With')).toBe('XMLHttpRequest')
  })

  it('sends one sign-out request when the button is clicked twice', async () => {
    const fetchMock = mockApi((request) =>
      new URL(request.url).pathname === '/api/session' ? json(signedIn) : Promise.resolve(new Response(null, { status: 204 })),
    )
    render(<SessionControl />)
    const button = await screen.findByRole('button', { name: 'Sign out' })

    fireEvent.click(button)
    fireEvent.click(button)

    await screen.findByRole('link', { name: 'Sign in with GitHub' })
    expect(fetchMock.mock.calls.filter(([request]) => request.method === 'POST')).toHaveLength(1)
  })

  it('keeps the user signed in and says so when signing out fails', async () => {
    mockApi((request) =>
      new URL(request.url).pathname === '/api/session' ? json(signedIn) : json({ title: 'Boom' }, 500),
    )
    render(<SessionControl />)

    fireEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Signing out failed. Try again.')
    expect(screen.getByText('Signed in as octocat')).toBeInTheDocument()
  })

  it('offers to sign in when the session cannot be checked', async () => {
    mockApi(() => Promise.reject(new TypeError('Failed to fetch')))

    render(<SessionControl />)

    expect(await screen.findByRole('link', { name: 'Sign in with GitHub' })).toBeInTheDocument()
  })

  it('has no automated accessibility violations when signed in', async () => {
    mockApi(() => json(signedIn))

    const { container } = render(<SessionControl />)
    await screen.findByText('Signed in as octocat')

    expect(await axeViolations(container)).toEqual([])
  })
})
