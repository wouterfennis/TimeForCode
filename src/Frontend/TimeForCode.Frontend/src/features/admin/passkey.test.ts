import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { registerAdminPasskey, signInAdmin } from './passkey'

type FetchCall = [string, RequestInit | undefined]

const parsedOptions = { challenge: new Uint8Array([1]) }
const credential = { toJSON: () => ({ id: 'credential-id', type: 'public-key' }) }

function stubBrowser(options: { create?: () => Promise<unknown>; get?: () => Promise<unknown>; supported?: boolean } = {}) {
  const create = vi.fn<() => Promise<unknown>>(options.create ?? (() => Promise.resolve(credential)))
  const get = vi.fn<() => Promise<unknown>>(options.get ?? (() => Promise.resolve(credential)))
  if (options.supported !== false) {
    vi.stubGlobal('PublicKeyCredential', {
      parseCreationOptionsFromJSON: vi.fn<() => typeof parsedOptions>(() => parsedOptions),
      parseRequestOptionsFromJSON: vi.fn<() => typeof parsedOptions>(() => parsedOptions),
    })
  } else {
    vi.stubGlobal('PublicKeyCredential', undefined)
  }
  vi.stubGlobal('navigator', { credentials: { create, get } })
  return { create, get }
}

function stubFetch(responses: Response[]) {
  const queue = [...responses]
  const fetchMock = vi.fn<(url: string, init?: RequestInit) => Promise<Response>>(() => Promise.resolve(queue.shift() ?? new Response(null, { status: 500 })))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const optionsResponse = () => Response.json({ ceremonyId: 'ceremony-1', optionsJson: JSON.stringify({ challenge: 'abc' }) })
const sessionResponse = () => Response.json({ isSignedIn: true, isNewUser: false, isAdmin: true, userId: 'admin', login: null, avatarUrl: null })

const bodyOf = (call: FetchCall | undefined) => JSON.parse(String(call?.[1]?.body)) as Record<string, unknown>

beforeEach(() => {
  stubBrowser()
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('signInAdmin', () => {
  it('asks for options, lets the browser sign and sends the assertion with the ceremony id', async () => {
    const fetchMock = stubFetch([optionsResponse(), sessionResponse()])

    const result = await signInAdmin()

    expect(result).toEqual({
      status: 'signed-in',
      session: { isNewUser: false, isAdmin: true, userId: 'admin', login: null, avatarUrl: null },
    })
    const calls = fetchMock.mock.calls as FetchCall[]
    expect(calls[0]?.[0]).toBe('/bff/admin/authentication/options')
    expect(calls[1]?.[0]).toBe('/bff/admin/authentication')
    expect(bodyOf(calls[1])).toEqual({ ceremonyId: 'ceremony-1', credentialJson: JSON.stringify({ id: 'credential-id', type: 'public-key' }) })
  })

  it('sends the CSRF header and no token on every call', async () => {
    const fetchMock = stubFetch([optionsResponse(), sessionResponse()])

    await signInAdmin()

    for (const call of fetchMock.mock.calls as FetchCall[]) {
      expect(call[1]?.method).toBe('POST')
      expect(call[1]?.credentials).toBe('same-origin')
      expect(call[1]?.headers).toMatchObject({ 'X-Requested-With': 'XMLHttpRequest', 'Content-Type': 'application/json' })
      expect(JSON.stringify(call[1])).not.toMatch(/bearer|authorization/i)
    }
  })

  it('reports that no passkey is registered yet when the BFF refuses the options', async () => {
    stubFetch([new Response(null, { status: 400 })])

    expect(await signInAdmin()).toEqual({ status: 'refused' })
  })

  it('reports a refused sign-in when the BFF rejects the assertion', async () => {
    stubFetch([optionsResponse(), new Response(null, { status: 400 })])

    expect(await signInAdmin()).toEqual({ status: 'refused' })
  })

  it('reports cancelled when the user dismisses the browser prompt', async () => {
    stubBrowser({ get: () => Promise.reject(new DOMException('dismissed', 'NotAllowedError')) })
    stubFetch([optionsResponse()])

    expect(await signInAdmin()).toEqual({ status: 'cancelled' })
  })

  it('reports unsupported when the browser has no passkey API', async () => {
    stubBrowser({ supported: false })
    const fetchMock = stubFetch([])

    expect(await signInAdmin()).toEqual({ status: 'unsupported' })
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('reports an error when the network fails', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('offline'))))

    expect(await signInAdmin()).toEqual({ status: 'error' })
  })
})

describe('registerAdminPasskey', () => {
  it('creates the passkey and sends it with the bootstrap secret and the ceremony id', async () => {
    const { create } = stubBrowser()
    const fetchMock = stubFetch([optionsResponse(), sessionResponse()])

    const result = await registerAdminPasskey('bootstrap-secret')

    expect(result.status).toBe('signed-in')
    expect(create).toHaveBeenCalledTimes(1)
    const calls = fetchMock.mock.calls as FetchCall[]
    expect(calls[0]?.[0]).toBe('/bff/admin/registration/options')
    expect(bodyOf(calls[0])).toEqual({ bootstrapSecret: 'bootstrap-secret' })
    expect(calls[1]?.[0]).toBe('/bff/admin/registration')
    expect(bodyOf(calls[1])).toEqual({
      bootstrapSecret: 'bootstrap-secret',
      ceremonyId: 'ceremony-1',
      credentialJson: JSON.stringify({ id: 'credential-id', type: 'public-key' }),
    })
  })

  it('reports refused for a wrong secret without calling the browser', async () => {
    const { create } = stubBrowser()
    stubFetch([new Response(null, { status: 400 })])

    expect(await registerAdminPasskey('wrong')).toEqual({ status: 'refused' })
    expect(create).not.toHaveBeenCalled()
  })

  it('reports cancelled when the user dismisses the browser prompt', async () => {
    stubBrowser({ create: () => Promise.reject(new DOMException('dismissed', 'NotAllowedError')) })
    stubFetch([optionsResponse()])

    expect(await registerAdminPasskey('bootstrap-secret')).toEqual({ status: 'cancelled' })
  })
})
