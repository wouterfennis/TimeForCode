import { afterEach, describe, expect, it, vi } from 'vitest'
import { fetchSession, signInUrl, signOut } from './session'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('fetchSession', () => {
  it('returns the session of a signed-in user', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(Response.json({ isSignedIn: true, isNewUser: false, userId: 'u1', login: 'octocat', avatarUrl: null }))),
    )

    expect(await fetchSession()).toEqual({
      status: 'signed-in',
      session: { isNewUser: false, userId: 'u1', login: 'octocat', avatarUrl: null },
    })
  })

  it('asks the same origin and sends no token', async () => {
    const fetchMock = vi.fn<(...args: unknown[]) => Promise<Response>>((..._args) => Promise.resolve(Response.json({ isSignedIn: true, isNewUser: false, userId: 'u1' })))
    vi.stubGlobal('fetch', fetchMock)

    await fetchSession()

    expect(fetchMock).toHaveBeenCalledWith('/bff/session', expect.objectContaining({ credentials: 'same-origin', headers: { Accept: 'application/json' } }))
  })

  it('says signed out on 401', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(null, { status: 401 }))))

    expect(await fetchSession()).toEqual({ status: 'signed-out' })
  })

  it.each([500, 502, 429])('reports an error on %i, so a hiccup is not shown as signed out', async (status) => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(null, { status }))))

    expect(await fetchSession()).toEqual({ status: 'error' })
  })

  it('reports an error when the network fails', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('offline'))))

    expect(await fetchSession()).toEqual({ status: 'error' })
  })
})

describe('signInUrl', () => {
  it('goes through the BFF and returns to the page the user was on', () => {
    expect(signInUrl('/projects/42?tab=a b')).toBe('/bff/login?returnUrl=%2Fprojects%2F42%3Ftab%3Da%20b')
  })
})

describe('signOut', () => {
  it('posts to the BFF with the CSRF header', async () => {
    const fetchMock = vi.fn<(...args: unknown[]) => Promise<Response>>((..._args) => Promise.resolve(new Response(null, { status: 204 })))
    vi.stubGlobal('fetch', fetchMock)

    await signOut()

    expect(fetchMock).toHaveBeenCalledWith('/bff/logout', { method: 'POST', credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
  })

  it('fails when the BFF refuses', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(null, { status: 403 }))))

    await expect(signOut()).rejects.toThrow('Sign-out failed')
  })
})
