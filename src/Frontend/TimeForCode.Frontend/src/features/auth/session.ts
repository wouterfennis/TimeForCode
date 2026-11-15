// The browser never sees a token. The BFF keeps the tokens behind one opaque HttpOnly cookie; the SPA only asks it
// who is signed in and sends the browser to its sign-in and sign-out endpoints. See docs/authentication/authentication_flow_design.md.

export type SessionInfo = {
  isNewUser: boolean
  isAdmin?: boolean
  userId: string
  login?: string | null
  avatarUrl?: string | null
}

export type SessionResult =
  | { status: 'signed-in'; session: SessionInfo }
  | { status: 'signed-out' }
  | { status: 'error' }

/** Asks the BFF whether the user is signed in. A failure to ask is an error, never "signed out". */
export async function fetchSession(signal?: AbortSignal): Promise<SessionResult> {
  try {
    const response = await fetch('/bff/session', { credentials: 'same-origin', headers: { Accept: 'application/json' }, signal })
    if (response.status === 401) return { status: 'signed-out' }
    if (!response.ok) return { status: 'error' }
    const body = (await response.json()) as SessionInfo
    return { status: 'signed-in', session: { isNewUser: body.isNewUser, isAdmin: body.isAdmin, userId: body.userId, login: body.login, avatarUrl: body.avatarUrl } }
  } catch {
    return { status: 'error' }
  }
}

/** The address that starts the sign-in; it returns to `returnUrl` (a path on this origin) afterwards. */
export function signInUrl(returnUrl: string): string {
  return `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`
}

/** Signs out. State-changing calls to the BFF carry the X-Requested-With header. */
export async function signOut(): Promise<void> {
  const response = await fetch('/bff/logout', { method: 'POST', credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
  if (!response.ok) throw new Error(`Sign-out failed with status ${response.status}`)
}
