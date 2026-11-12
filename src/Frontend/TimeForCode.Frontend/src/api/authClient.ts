import createClient from 'openapi-fetch'
import type { paths } from './authorization'
import { csrfHeader, routeThroughFrontendOrigin } from './frontendOrigin'

const prefix = '/api/auth/'

// `fetch` is looked up per request, so tests can stub it (see donationClient).
export const authClient = createClient<paths>({
  baseUrl: window.location.origin,
  fetch: (request) => globalThis.fetch(request),
})
authClient.use(routeThroughFrontendOrigin(prefix), csrfHeader)

// Sign-in is a full-page navigation, not a fetch: the Authorization API redirects to GitHub, and GitHub back to the
// callback on this origin, which sets the HttpOnly token cookies and redirects to `returnTo`. `returnTo` must be on
// the Authorization API's list of valid redirect URIs.
export function signInUrl(returnTo: string): string {
  const query = new URLSearchParams({ IdentityProvider: 'Github', RedirectUri: returnTo })
  return `${prefix}v1/Authentication/login?${query.toString()}`
}
