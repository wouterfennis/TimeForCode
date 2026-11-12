// The BFF's `GET /api/session` answer: who is signed in, from the HttpOnly token cookie the SPA cannot read.
// It only steers the UI; the APIs enforce authorization.
export type SessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'signedIn'; login: string; isAdmin: boolean }

const anonymous: SessionState = { status: 'anonymous' }

export async function fetchSession(signal: AbortSignal): Promise<SessionState> {
  const response = await globalThis.fetch(new Request(new URL('/api/session', window.location.origin), { signal }))
  if (!response.ok) return anonymous
  return toSessionState(await response.json())
}

function toSessionState(body: unknown): SessionState {
  if (typeof body !== 'object' || body === null) return anonymous
  const { signedIn, login, isAdmin } = body as Record<string, unknown>
  if (signedIn !== true || typeof login !== 'string') return anonymous
  return { status: 'signedIn', login, isAdmin: isAdmin === true }
}
