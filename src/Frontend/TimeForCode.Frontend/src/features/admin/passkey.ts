// The administrator signs in with a passkey (WebAuthn). The BFF keeps the ceremony state and the tokens: this code only passes the
// options to the browser and the browser's answer back, with an opaque ceremony id. No token is ever read, stored or sent here.
// See docs/authentication/authentication_flow_design.md (Admin login).
import type { SessionInfo } from '../auth/session'

export type PasskeyResult =
  | { status: 'signed-in'; session: SessionInfo }
  | { status: 'refused' }
  | { status: 'cancelled' }
  | { status: 'unsupported' }
  | { status: 'error' }

type Ceremony = { ceremonyId: string; optionsJson: string }

function post(path: string, body: unknown): Promise<Response> {
  return fetch(path, {
    method: 'POST',
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
    body: JSON.stringify(body),
  })
}

function supportsPasskeys(): boolean {
  return typeof PublicKeyCredential !== 'undefined' && typeof navigator !== 'undefined' && navigator.credentials !== undefined
}

async function readSession(response: Response): Promise<PasskeyResult> {
  const body = (await response.json()) as SessionInfo
  return {
    status: 'signed-in',
    session: { isNewUser: body.isNewUser, isAdmin: body.isAdmin, userId: body.userId, login: body.login, avatarUrl: body.avatarUrl },
  }
}

// The user dismissing or timing out the browser prompt is not an error.
function isCancellation(error: unknown): boolean {
  return error instanceof DOMException && (error.name === 'NotAllowedError' || error.name === 'AbortError')
}

async function ceremony(
  startPath: string,
  startBody: unknown,
  finishPath: string,
  finishBody: (ceremonyId: string, credentialJson: string) => unknown,
  perform: (optionsJson: string) => Promise<PublicKeyCredential | null>,
): Promise<PasskeyResult> {
  if (!supportsPasskeys()) return { status: 'unsupported' }
  try {
    const started = await post(startPath, startBody)
    if (started.status === 400) return { status: 'refused' }
    if (!started.ok) return { status: 'error' }
    const { ceremonyId, optionsJson } = (await started.json()) as Ceremony

    const credential = await perform(optionsJson)
    if (!credential) return { status: 'cancelled' }

    const finished = await post(finishPath, finishBody(ceremonyId, JSON.stringify(credential.toJSON())))
    if (finished.status === 400) return { status: 'refused' }
    if (!finished.ok) return { status: 'error' }
    return await readSession(finished)
  } catch (error) {
    return isCancellation(error) ? { status: 'cancelled' } : { status: 'error' }
  }
}

/** Signs the administrator in with the registered passkey. */
export function signInAdmin(): Promise<PasskeyResult> {
  return ceremony(
    '/bff/admin/authentication/options',
    {},
    '/bff/admin/authentication',
    (ceremonyId, credentialJson) => ({ ceremonyId, credentialJson }),
    async (optionsJson) =>
      (await navigator.credentials.get({ publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(optionsJson)) })) as PublicKeyCredential | null,
  )
}

/** Claims the admin role once: registers the passkey of this device and signs in. Needs the bootstrap secret. */
export function registerAdminPasskey(bootstrapSecret: string): Promise<PasskeyResult> {
  return ceremony(
    '/bff/admin/registration/options',
    { bootstrapSecret },
    '/bff/admin/registration',
    (ceremonyId, credentialJson) => ({ bootstrapSecret, ceremonyId, credentialJson }),
    async (optionsJson) =>
      (await navigator.credentials.create({ publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(optionsJson)) })) as PublicKeyCredential | null,
  )
}
