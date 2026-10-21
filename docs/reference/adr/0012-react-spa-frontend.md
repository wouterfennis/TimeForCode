# ADR-012 — React single-page application behind a same-origin BFF

**Date**: 2026-10-06
**Status**: Proposed
**Amended**: 2026-10-21 (nginx serves the SPA until the BFF arrives with sign-in, see [Amendment](#amendment-2026-10-21-nginx-first-the-bff-with-sign-in))

## Context

We want a front-end with a real design system, better accessibility tooling, fast public pages, and first-class access to browser APIs (the admin WebAuthn flow already needs hand-written JavaScript interop).

Two backend facts constrain the design:

- Tokens are `HttpOnly`, `SameSite=Strict` cookies (ADR-005).
- The Donation API accepts only a bearer token in the `Authorization` header. Something must therefore bridge cookie to bearer on the way to the API.

## Decision

Build the new front-end as a **React + TypeScript single-page application** (Vite), and serve it from a thin ASP.NET host acting as a **same-origin backend-for-frontend (BFF)**. The BFF serves static files and reverse-proxies `/api/auth/*` and `/api/donation/*`, reusing the cookie-to-bearer handlers. API types are generated from the committed OpenAPI snapshots (ADR-007).

Details are in [docs/frontend/architecture.md](../../frontend/architecture.md).

### Alternatives considered

| Option | Why not chosen |
| --- | --- |
| SPA calling APIs directly, cookies sent cross-origin | Donation API ignores cookies; `SameSite=Strict` blocks cross-site cookies; would need backend changes |
| Change the Donation API to accept cookies | Widens CSRF exposure and changes a stable backend for a front-end concern |
| Angular or Vue instead of React | Viable; React chosen for ecosystem size. Revisit before Phase 0 if the team prefers otherwise |

## Consequences

**Positive**:

- Backend APIs and ADR-005 stay unchanged; tokens never reach JavaScript.
- Typed, generated API clients make backend contract changes visible at compile time.
- Public pages can be lean; the design system and accessibility checks are first-class.

**Negative**:

- A second technology stack (Node tooling) to build, test, and maintain.
- The BFF is a new moving part and needs CSRF protection (custom header) and a CSP.
- Public pages need a meta-tag or prerendering story for sharing and search.

## Amendment (2026-10-21): nginx first, the BFF with sign-in

Decided in [#111](https://github.com/wouterfennis/TimeForCode/issues/111): the BFF arrives when the first feature needs it, not at the start of Phase 0.

- **Until sign-in**: nginx serves the SPA (`Dockerfile.frontend`, `src/Frontend/nginx.conf`) and sets the security headers, including the Content Security Policy. Serving the files and the public project list (#112) need no sign-in, so nothing needs the cookie-to-bearer bridge yet.
- **From sign-in (#113) on**: the BFF is introduced. Authenticated calls need the token cookie turned into a bearer header. The cookie holds a JSON document, so nginx cannot do that, and that logic belongs in tested code rather than in nginx configuration. CSRF protection arrives with the BFF too: before #113 the SPA sends no authenticated, state-changing requests.
- **What keeps the switch cheap**: the SPA only calls relative paths on its own origin. Until #113 nginx forwards them (#112 adds `/api/donation/*`); from #113 on the BFF handles them, or nginx forwards them to the BFF. The SPA does not change.

| SPA path | Forwarded to |
| --- | --- |
| `/api/auth/*` | Authorization API `/api/*` |
| `/api/donation/*` | Donation API `/api/*` |

For example, `/api/donation/v1/Project` goes to the Donation API's `/api/v1/Project`.

## Follow-up when accepted

- Update Arc42 section 04 (frontend row), 05 (building blocks), 07 (deployment), 09 (index), and 11 (risks).
- Update ADR-005 consequences with the CSRF approach.
- Update `docs/current/overview.md` at cutover.
