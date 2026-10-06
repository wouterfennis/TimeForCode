# Front-end Redesign

Status: Target

This folder is the starting point for building the new TimeForCode front-end. It tells a developer what to build, why, and in what order, so they do not have to rediscover decisions already made. The current Blazor Website stays in service until the new front-end reaches parity (see [migration plan](migration-plan.md)).

> **Draft decisions.** The stack choice is recorded as a proposed decision in [ADR-012](../reference/adr/0012-react-spa-frontend.md). The visual design (tokens, wireframes) is a starting proposal, not a finished brand. Where a document says *Open question*, the developer or designer should decide and record the answer here.

---

## Documents

| Document | Purpose |
| --- | --- |
| [Vision and Principles](vision-and-principles.md) | Why we are replacing the front-end and the principles that guide it |
| [Information Architecture](information-architecture.md) | Sitemap, routes, and who may see what |
| [Page Specifications](page-specs.md) | Per-page purpose, data, states, and wireframes |
| [Design System](design-system.md) | Design tokens, components, accessibility rules |
| [Design Mockups](mockups/README.md) | Rendered HTML mockups and screenshots of the main pages, with tooling to regenerate and check them |
| [Technical Architecture](architecture.md) | Stack, folder layout, API access, authentication, testing |
| [Migration Plan](migration-plan.md) | Strangler-style rollout, parity checklist, what to keep |

---

## Key terms

- **SPA (single-page application)**: a website that loads once in the browser and then updates the page with JavaScript, fetching data from APIs instead of requesting a new HTML page for each navigation.
- **BFF (backend for frontend)**: a small server that exists only to serve one front-end. Here it serves the SPA files and forwards the browser's API calls to the Authorization and Donation APIs, turning the HttpOnly login cookie into the bearer token those APIs expect. The browser therefore never handles tokens. See [Technical Architecture](architecture.md#authentication-and-the-bff).

Both terms are also in the [glossary](../reference/glossary.md).

---

## How to use these documents

1. Read the vision and principles first. They settle most debates.
2. Build the foundation described in [Technical Architecture](architecture.md) before any page.
3. Implement pages in the order given in the [migration plan](migration-plan.md).
4. When you finish a page, update its entry in [page-specs.md](page-specs.md) and the parity checklist.
5. If you change a decision, update the document and add or amend an ADR. Follow the rules in [docs/README.md](../README.md).

## What already exists and must be reused

- **Backend APIs** are not changing for this work. The OpenAPI snapshots committed next to each API are the contract: `src/Authorization/TimeForCode.Authorization.Api/TimeForCode.Authorization.Api.json` and `src/Donation/TimeForCode.Donation.Api/TimeForCode.Donation.Api.json`.
- **Product intent** lives in [Personas and Journeys](../target/personas-and-journeys.md) and [Authorization and Roles](../target/authorization-and-roles.md).
- **Current behaviour** lives in [Current Overview](../current/overview.md) and [API Surface](../current/api-surface.md).
- **Existing E2E tests** in `tst/Website/TimeForCode.Website.Specifications` describe behaviour that the new front-end must also satisfy. They select elements by `data-testid`, so keep those identifiers.
