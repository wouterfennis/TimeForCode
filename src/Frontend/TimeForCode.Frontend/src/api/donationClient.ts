import createClient, { type Middleware } from 'openapi-fetch'
import type { paths } from './donation'

// The generated paths are the Donation API's own (`/api/v1/Project`). The front-end origin serves
// them under `/api/donation/` (nginx now, the BFF from #113), so the prefix is added per request.
const routeThroughFrontendOrigin: Middleware = {
  onRequest({ request }) {
    const url = new URL(request.url)
    url.pathname = url.pathname.replace(/^\/api\//, '/api/donation/')
    return new Request(url, request)
  },
}

export function createDonationClient() {
  const client = createClient<paths>({ baseUrl: window.location.origin })
  client.use(routeThroughFrontendOrigin)
  return client
}
