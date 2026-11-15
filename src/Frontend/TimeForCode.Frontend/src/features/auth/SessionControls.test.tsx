import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { axeViolations } from '../../../tests/axe'
import { SessionControls } from './SessionControls'

afterEach(() => {
  vi.unstubAllGlobals()
})

function stubFetch(handler: (url: string, init?: RequestInit) => Response) {
  const fetchMock = vi.fn<(url: string, init?: RequestInit) => Promise<Response>>((url, init) => Promise.resolve(handler(url, init)))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const signedIn = (isNewUser = false) => Response.json({ isSignedIn: true, isNewUser, userId: 'u1', login: 'octocat' })

describe('SessionControls', () => {
  it('offers to sign in when nobody is signed in', async () => {
    stubFetch(() => new Response(null, { status: 401 }))

    render(<SessionControls />)

    const link = await screen.findByRole('link', { name: 'Sign in' })
    expect(link.getAttribute('href')).toMatch(/^\/bff\/login\?returnUrl=/)
  })

  it('shows who is signed in and offers to sign out', async () => {
    stubFetch(() => signedIn())

    render(<SessionControls />)

    expect(await screen.findByText('Signed in as octocat')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeTruthy()
  })

  it('welcomes a user on their first sign-in', async () => {
    stubFetch(() => signedIn(true))

    render(<SessionControls />)

    expect(await screen.findByText('Welcome! Your profile has been created.')).toBeTruthy()
  })

  it('signs out through the BFF and then offers to sign in again', async () => {
    let isSignedIn = true
    const fetchMock = stubFetch((url) => {
      if (url === '/bff/logout') {
        isSignedIn = false
        return new Response(null, { status: 204 })
      }
      return isSignedIn ? signedIn() : new Response(null, { status: 401 })
    })
    render(<SessionControls />)

    fireEvent.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByRole('link', { name: 'Sign in' })).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledWith('/bff/logout', expect.objectContaining({ method: 'POST' }))
  })

  it('tells the user when the session cannot be checked', async () => {
    stubFetch(() => new Response(null, { status: 500 }))

    render(<SessionControls />)

    expect(await screen.findByRole('alert')).toBeTruthy()
  })

  it('has no automated accessibility violations', async () => {
    stubFetch(() => signedIn(true))
    const { container } = render(<SessionControls />)
    await screen.findByText('Signed in as octocat')

    expect(await axeViolations(container)).toEqual([])
  })
})
