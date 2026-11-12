import type { Middleware } from 'openapi-fetch'

// The generated paths are each API's own (`/api/v1/...`). The front-end origin (the BFF) serves an API
// under its own prefix, for example `/api/donation/`, so the prefix is added per request.
export function routeThroughFrontendOrigin(prefix: string): Middleware {
  return {
    onRequest({ request }) {
      const url = new URL(request.url)
      url.pathname = url.pathname.replace(/^\/api\//, prefix)
      return new Request(url, request)
    },
  }
}

// The BFF refuses a state-changing request without this header (second CSRF layer after SameSite=Strict).
export const csrfHeader: Middleware = {
  onRequest({ request }) {
    if (request.method !== 'GET' && request.method !== 'HEAD') {
      request.headers.set('X-Requested-With', 'XMLHttpRequest')
    }
    return request
  },
}
