# ADR-012 — React single-page application behind a same-origin BFF

**Date**: 2026-10-06
**Status**: Proposed

## Context

The current Website is a Blazor Server application (see [Solution Strategy](../../architecture/arc42/04-solution-strategy.md), which chose Blazor to share types with the backend and avoid a separate TypeScript stack). We want a front-end with a real design system, better accessibility tooling, fast public pages, and first-class access to browser APIs (the admin WebAuthn flow already needs hand-written JavaScript interop).

Two backend facts constrain the design:

- Tokens are `HttpOnly`, `SameSite=Strict` cookies (ADR-005).
- The Donation API accepts only a bearer token in the `Authorization` header. Today the Blazor server bridges cookie to bearer.

## Decision

Build the new front-end as a **React + TypeScript single-page application** (Vite), and serve it from the existing ASP.NET Website project acting as a **same-origin backend-for-frontend (BFF)**. The BFF serves static files and reverse-proxies `/api/auth/*` and `/api/donation/*`, reusing the cookie-to-bearer handlers. API types are generated from the committed OpenAPI snapshots (ADR-007).

Details are in [docs/frontend/architecture.md](../../frontend/architecture.md).

### Alternatives considered

| Option | Why not chosen |
| --- | --- |
| Keep Blazor and restyle it | Does not address the ecosystem, accessibility tooling, or public-page performance goals |
| Blazor WebAssembly | Large download for public pages; still no access to the JavaScript design-system ecosystem |
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
- Dual running during migration adds temporary complexity (see the [migration plan](../../frontend/migration-plan.md)).

## Follow-up when accepted

- Update Arc42 section 04 (frontend row), 05 (building blocks), 07 (deployment), 09 (index), and 11 (risks).
- Update ADR-005 consequences with the CSRF approach.
- Update `docs/current/overview.md` at cutover.
