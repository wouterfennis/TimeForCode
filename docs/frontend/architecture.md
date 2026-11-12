# Technical Architecture

Status: Target

This document describes how the new front-end is built and how it talks to the backend. The decision record is [ADR-012](../reference/adr/0012-react-spa-frontend.md).

---

## Stack

| Concern | Choice | Reason |
| --- | --- | --- |
| Runtime for tooling | Node 26 (LTS) | Pinned; see [Project setup](#project-setup) |
| Language | TypeScript 7, `strict` mode | Type safety against the generated API types; the native compiler keeps the type-check fast |
| Framework | React | Largest ecosystem for accessible component libraries and testing |
| Build tool | Vite | Fast dev server, simple configuration |
| Routing | React Router | Mature, supports route-level data loading |
| Server state | TanStack Query | Caching, retries, loading and error states for API calls |
| Forms | React Hook Form with Zod | Typed validation; map API `ProblemDetails` onto fields |
| API client | Generated from the OpenAPI snapshots | No hand-written DTOs; see below |
| Styling | CSS custom properties from [design-system.md](design-system.md), plus CSS Modules | No heavy UI framework; tokens stay the single source of truth |
| Linting | oxlint with the React, jsx-a11y, TypeScript, import and Vitest rules | Default linter of the Vite React template; accessibility rules built in |
| Unit and component tests | Vitest, Testing Library, axe | Fast feedback, accessibility checks included |
| E2E tests | Playwright (planned) | Journeys against the full stack |

Treat the choices as defaults. Replacing one is fine if the reason is recorded in the ADR.

## Where it lives

```text
src/
└── Frontend/
    └── TimeForCode.Frontend/        # the SPA (package.json, vite.config.ts)
        ├── src/
        │   ├── api/                 # generated clients (do not edit by hand)
        │   ├── app/                 # shell, routing, providers
        │   ├── components/          # design-system components
        │   ├── features/            # projects/, profile/, admin/, auth/
        │   ├── lib/                 # small helpers
        │   └── messages/            # all user-facing strings
        └── tests/                   # shared test setup and helpers (axe)
```

Organise by feature, not by file type. A feature folder owns its pages, queries, and tests. Shared building blocks go in `components/`.

## Project setup

Implemented in Phase 0 (#110); commands are in the project [README](../../src/Frontend/TimeForCode.Frontend/README.md).

- **Node version**: Node 26, pinned in `.nvmrc` and `package.json` `engines`. `.npmrc` sets `engine-strict`, so installing with another major version fails. CI reads `.nvmrc`.
- **Lock file**: `package-lock.json` is committed. CI installs with `npm ci --ignore-scripts`, so no package install scripts run. Dependency versions are pinned exactly.
- **Scripts**: `lint`, `typecheck`, `test:coverage` and `build` are the checks the pull request workflow runs. Lint warnings fail the check (`--deny-warnings`).
- **Coverage**: `test:coverage` writes `coverage/lcov.info` with paths relative to the repository root. The `frontend` CI job uploads it, and the .NET job passes it to SonarCloud (`sonar.javascript.lcov.reportPaths`). Test files, `tests/`, `src/main.tsx` and `vite.config.ts` are excluded from coverage.
- **Strictness**: `strict` and `noUncheckedIndexedAccess` are on. `tsc` is the native TypeScript 7 compiler; Vite and Vitest transpile without it, so only the type-check depends on it.
- **Accessibility tests**: component tests run axe through the small `axeViolations` helper in `tests/axe.ts` (axe-core directly, no wrapper library). jsdom cannot compute colour contrast, so contrast is checked in the E2E suite or by hand.

Choices that differ from the latest versions or the usual defaults, with the reason:

| Default | Chosen | Reason |
| --- | --- | --- |
| Latest jsdom (30) | jsdom 29 | jsdom 30 needs Node 24.15 or later; 29 supports every Node 24 release |
| ESLint | oxlint | The Vite React template ships oxlint; it has the React, hooks and jsx-a11y rules built in, while the ESLint jsx-a11y plugin does not support ESLint 10 yet |

## Authentication and the BFF

This is the most important design constraint. Read it before writing any code.

**Facts about the current backend** (verified in code):

- Login sets the access and refresh tokens as `HttpOnly`, `Secure`, `SameSite=Strict` cookies from the Authorization API.
- The cookie values are JSON documents, not bare JWTs.
- The Donation API validates only a **bearer token in the `Authorization` header**. It does not read cookies.
- Server-side code can bridge the two: `CookieAuthorizationHandler` (Shared) reads the cookie and attaches the bearer header, and `RefreshTokenForwardingHandler` forwards the refresh cookie when refreshing.
- [ADR-005](../reference/adr/0005-http-only-cookies.md) forbids tokens in JavaScript-accessible storage.

**Consequence**: a browser SPA cannot call the Donation API directly. JavaScript cannot read the HttpOnly cookie to build the bearer header, and a `SameSite=Strict` cookie will not be sent from a different site anyway.

A **BFF** (backend for frontend) is a small server that exists only to serve one front-end and sits between it and the backend APIs. An **SPA** (single-page application) is a website that loads once and then updates itself with JavaScript, fetching data from APIs.

**Decision**: serve the SPA from, and call all APIs through, a thin **backend-for-frontend (BFF)** on the same origin. The BFF is a thin ASP.NET host, `src/Frontend/TimeForCode.Frontend.Bff` (added in #182, tests in `tst/Frontend/TimeForCode.Frontend.Bff.Tests`), that does only the following:

1. Serve the built SPA static files, with a fallback to `index.html` for client routes, and set the security headers.
2. Reverse-proxy `/api/auth/*` to the Authorization API and `/api/donation/*` to the Donation API with [YARP](https://github.com/dotnet/yarp), adding the bearer token from the access-token cookie with the shared `CookieAuthorizationHandler`. Cookies pass through in both directions, so refresh and logout work without the refresh-forwarding handler.
3. Optionally render meta tags for public pages (see below).

```mermaid
graph LR
    Browser["Browser (SPA)"]
    BFF["BFF\n(ASP.NET host)"]
    AuthAPI["Authorization API"]
    DonationAPI["Donation API"]

    Browser -->|same-origin /api/*, cookies| BFF
    BFF -->|forwards cookie as bearer| DonationAPI
    BFF -->|forwards cookies| AuthAPI
```

The diagram shows that the browser only ever talks to one origin. The BFF translates cookies into bearer tokens so the backend APIs stay unchanged. Because everything is same-origin, the CORS configuration added for the admin flow is not needed for the SPA, and `SameSite=Strict` keeps working.

**History** ([ADR-012 amendments](../reference/adr/0012-react-spa-frontend.md#amendment-2026-11-08-the-bff-replaces-nginx)): nginx served the SPA and the Donation route until #182, when the BFF replaced it, just before sign-in (#113) needs the cookie turned into a bearer header. The SPA only calls relative paths on its own origin, so it did not change:

| SPA path | Forwarded to | Route in the BFF |
| --- | --- | --- |
| `/api/auth/*` | Authorization API `/api/*` | `auth` |
| `/api/donation/*` | Donation API `/api/*` | `donation` |

For example, `/api/donation/v1/Project` goes to the Donation API's `/api/v1/Project`. The routes and their destinations are in the BFF's `appsettings.json` (`ReverseProxy`); `docker-compose.yaml` sets the destinations with `ReverseProxy__Clusters__<route>__Destinations__api__Address`. Forwarded responses get `Cache-Control: no-store`, and an API's own absolute `Location` (`http://donation-api:8080/api/v1/Project/42`) is rewritten to the route (`/api/donation/v1/Project/42`); other locations, such as the GitHub login or the SPA redirect, stay as they are.

Alternatives considered (recorded in the ADR): changing the Donation API to accept cookies (backend change, wider CSRF surface), or hosting the SPA on a sibling subdomain with a shared cookie domain (weakens `SameSite=Strict`). Both widen the scope of this work.

### CSRF

Cookie authentication means state-changing requests need CSRF protection. `SameSite=Strict` is the first layer. The second (#182): the BFF answers 403 to any request other than `GET`, `HEAD` and `OPTIONS` that lacks the `X-Requested-With` header (any value), before it reaches an API. A cross-site form cannot set the header, and a cross-origin script that tries needs a CORS preflight the BFF does not approve. The SPA's API clients send the header from the first state-changing call (sign-out, #113).

### Login redirect

Login starts with a browser navigation to the Authorization API login endpoint (through the BFF), which redirects to GitHub and back. The callback sets the cookies and redirects to the SPA. The allowed redirect targets are an allow-list in the Authorization API configuration; add the new front-end origin there. Never accept an arbitrary return URL.

### Session handling in the SPA

- Determine who is signed in with `GET /api/v1/User` on app start (cache with TanStack Query).
- On a 401, the API layer tries one refresh (`POST /api/v1/Authentication/refresh`), then retries the request once. If that fails, the user is treated as signed out.
- Roles for UI decisions come from the user endpoint or token claims exposed by the BFF. The backend remains the enforcer.

## API client generation

- Source of truth: the committed OpenAPI snapshots next to each API (see [ADR-007](../reference/adr/0007-swagger-snapshot-gate.md)).
- `npm run generate:api` generates `src/api/donation.d.ts` (and the Authorization and Reporting files, see below) from the snapshots with `openapi-typescript`. It runs through `npx` with exact versions of `openapi-typescript` and TypeScript 5, because `openapi-typescript` needs the JavaScript compiler API that the native TypeScript 7 compiler of this project does not have. The requests go through `openapi-fetch`, so request and response types come from the generated file only.
- **Decision (#112): the generated output is committed**, and CI fails when it is stale. `npm run check:api` regenerates it and fails on any `git diff` (step "API types up to date" in the `frontend` job). Committing keeps the Docker build free of files outside the front-end folder and shows API changes in the pull request diff.
- When a backend change alters a snapshot, `check:api` fails until the types are regenerated and committed; after that `typecheck` shows what the front-end must change. That is the contract gate for the front-end.
- The Authorization and Reporting snapshots have their own generated files (`authorization.d.ts`, `reporting.d.ts`, scripts `generate:api:<name>`). No typed client exists for them yet: it is added with the first feature that calls them (sign-in, #113); the BFF route `/api/auth/*` exists since #182.
- The generated paths are the API's own (`/api/v1/Project`); `src/api/donationClient.ts` adds the `/api/donation` prefix of the front-end origin to each request.

## Dev server against the local stack

1. Start the stack: `scripts/start-local.ps1` (Donation API on `http://localhost:8082`).
2. In `src/Frontend/TimeForCode.Frontend`: `npm run dev` and open the printed URL.

`vite.config.ts` proxies `/api/donation/*` to `http://localhost:8082/api/*`, the same route the BFF has in the image, so the SPA code is identical in both setups. The front-end container (`http://localhost:8084`) needs the Donation API container to be running.

## Rendering strategy

- Default: a client-rendered SPA.
- **Public pages** (`/`, `/projects`, `/projects/:id`) need good titles, descriptions, and Open Graph tags for sharing and search. The simplest start is for the BFF to inject per-route meta tags into `index.html` using the public API. Full server-side rendering or static prerendering is an upgrade if measurements show it is needed.
- **Open question**: decide when the first public page is built, and record it here.

## Configuration

- No environment-specific values in the build. The SPA uses relative `/api/...` URLs.
- Any runtime configuration (for example the repository URL for the footer) is served by the BFF as a small JSON endpoint.

## Build and deployment

- `Dockerfile.frontend` builds the SPA (Node), publishes the BFF (.NET SDK) and runs the BFF on the ASP.NET runtime image as its non-root user, port 8080, with the SPA build output as its web root (`/app/wwwroot`). Deep links fall back to `index.html` (`Cache-Control: no-cache`); files under `/assets/` are cached for a year (`immutable`), and a missing file returns 404.
- The browser only talks to the front-end origin, so the APIs need no CORS. YARP sends `X-Forwarded-For`, `-Host` and `-Proto` for when an API reads them.
- The CSRF check (`X-Requested-With` on every write) relies on that: it is only safe while the APIs allow no cross-origin requests. Enabling CORS on an API, for example for a second front-end, means revisiting the check first.
- The BFF drops any `Authorization` header the browser sends; the bearer token comes only from the HttpOnly cookie. An `/api/...` path without a proxy route returns 404, not the SPA.
- Every forwarded API response carries `Cache-Control: no-store`, because the answers depend on the signed-in user and must not be kept by a browser or a shared cache. The price is that public data, such as the project list, is fetched again on every request.
- The front-end build, lint, type-check, and unit tests run in the `frontend` job of the pull request and main-branch workflows (`.github/workflows/`). The same job builds the front-end image from `Dockerfile.frontend`; it is not pushed until the front-end has a deployment. A failing check fails the job; on `main` it also blocks Release Please.
- `scripts/smoke-test.ps1` (section 5) checks the running front-end in the local stack: the SPA at `/` and on a deep link, a built asset, the 404 for a missing asset, the security headers on each of those responses, the routes `/api/donation/v1/Project` and `/api/auth/v1/user` (anonymous 401), and that a write without the CSRF header is refused. Section 3 calls `/api/auth/v1/user` through the front-end with only the login's access-token cookie, which proves the cookie-to-bearer forwarding.
- Target topology: one front-end container hosting the SPA. See [Deployment Status](../current/deployment-status.md).

## Testing strategy

| Level | Tooling | Scope |
| --- | --- | --- |
| Unit | Vitest | Helpers, mapping of API errors to form errors |
| Component | Testing Library, axe | Components and pages with a mocked API layer |
| E2E | Playwright (planned) | Journeys against the full stack; keep `data-testid` values |

Follow [testing-strategy](../current/testing.md) and ADR-008: E2E tests stay out of CI and run on demand.

## Security checklist

- No tokens in JavaScript-accessible storage; no tokens in URLs.
- Never render API-provided HTML. React escapes by default; do not use `dangerouslySetInnerHTML`.
- External links use `rel="noopener noreferrer"`.
- Security headers on every response, forwarded API responses and errors included, set by the BFF (`SecurityHeaders.cs`):
  - `Content-Security-Policy` that allows only the SPA's own origin and no framing: `default-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'`. No inline scripts or styles and no `data:` URLs; React's `style` prop is allowed because it sets styles from script. Allowing another source (for example GitHub avatars) is a deliberate change to `src/Frontend/TimeForCode.Frontend.Bff/SecurityHeaders.cs`, its tests and the smoke test's check.
  - `X-Content-Type-Options: nosniff`.
  - `Referrer-Policy: strict-origin-when-cross-origin`.
- Dependencies are pinned, and Dependabot or the existing dependency-update skill keeps them current.
