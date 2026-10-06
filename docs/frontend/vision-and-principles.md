# Vision and Principles

Status: Target

---

## Why replace the front-end

The current Website is a Blazor Server application. It works for login, a profile page, project listing and detail, and the admin donor-organization screens. It is a poor base for the product we want to build:

- **Interactive Server mode holds a live connection per visitor.** A public, read-mostly site (project discovery) pays this cost for no benefit.
- **The UI is the Bootstrap template defaults.** There is no product identity, and little structure for the richer journeys in [Personas and Journeys](../target/personas-and-journeys.md).
- **The admin WebAuthn flow needs a hand-written JavaScript interop file** (`wwwroot/js/webauthn-admin-interop.js`). Browser APIs are first-class in a JavaScript front-end.
- **The front-end ecosystem for design systems, accessibility tooling, and component testing is far larger** for TypeScript than for Blazor.

The backend is not the problem and does not change.

## Vision

A fast, welcoming website where a maintainer can list a project in a minute, a company can find a project and pledge hours in a few steps, and anyone can see the impact. It should feel like a trustworthy open-source community product, not an enterprise portal.

## Principles

1. **Public pages are fast and indexable.** Project listing and detail must load quickly on a mid-range phone and be shareable with a good title and description.
2. **Task-first, not data-first.** Each page answers one question for one persona. See the personas in [Personas and Journeys](../target/personas-and-journeys.md).
3. **The API is the contract.** The front-end contains no business rules that the backend also needs. Types come from the OpenAPI snapshots, never hand-written.
4. **Tokens never touch JavaScript.** Authentication stays in HttpOnly cookies ([ADR-005](../reference/adr/0005-http-only-cookies.md)). Do not add `localStorage` tokens, ever.
5. **Accessible by default.** WCAG 2.2 AA is the baseline, not a later phase. See [design-system.md](design-system.md).
6. **Honest states.** Every screen defines loading, empty, error, and unauthorized states. A blank page is a bug.
7. **Small, replaceable pieces.** Prefer a few well-known libraries over many. Every dependency needs a reason.
8. **Parity before polish.** Do not switch off the Blazor Website until the [parity checklist](migration-plan.md#parity-checklist) is complete.

## Non-goals

- Changing backend endpoints, the domain model, or the authentication design.
- A native mobile app.
- Server-side rendering of everything. Only the public pages need a prerendering story (see [architecture](architecture.md#rendering-strategy)).
- Multi-language support in the first release. Structure the code so adding it later is cheap (no hard-coded strings in components outside a single messages module), but do not translate yet.

## Success measures

| Measure | Target |
| --- | --- |
| Largest Contentful Paint on the project list (mid-range mobile, 4G) | under 2.5 s |
| Accessibility audit (axe) on every page | no serious or critical violations |
| Parity checklist | 100% before Blazor Website is retired |
| E2E journeys from the existing specifications | all pass against the new front-end |
