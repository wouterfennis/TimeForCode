import createClient from 'openapi-fetch'
import type { paths } from './donation'
import { csrfHeader, routeThroughFrontendOrigin } from './frontendOrigin'

// `fetch` is looked up per request (openapi-fetch otherwise keeps the one present at import), so tests can stub it.
export const donationClient = createClient<paths>({
  baseUrl: window.location.origin,
  fetch: (request) => globalThis.fetch(request),
})
donationClient.use(routeThroughFrontendOrigin('/api/donation/'), csrfHeader)
