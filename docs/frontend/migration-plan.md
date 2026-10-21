# Migration Plan

Status: Target

The front-end is built incrementally: the foundation first, then one vertical slice at a time.

---

## Strategy

```mermaid
graph LR
    P0["Phase 0\nFoundation"] --> P1["Phase 1\nPublic pages"]
    P1 --> P2["Phase 2\nAuth + profile"]
    P2 --> P3["Phase 3\nAdmin"]
    P3 --> P4["Phase 4\nHardening + deployment"]
```

Each phase is independently shippable and ends with working, tested software, in line with the principles in the [roadmap](../target/roadmap.md#principles).

## Phases

### Phase 0 — Foundation

- Scaffold the SPA project, tooling, linting, and CI as described in [architecture](architecture.md).
- Implement design tokens, the app shell, and the core components from the [design system](design-system.md).
- Serve the SPA with nginx: static hosting with security headers (#111), then forwarding `/api/donation/*` for the public project list (#112).
- Introduce the BFF with sign-in (#113): it forwards both APIs and turns the token cookie into a bearer header ([ADR-012 amendment](../reference/adr/0012-react-spa-frontend.md#amendment-2026-10-21-nginx-first-the-bff-with-sign-in)).
- Generate the API clients from the OpenAPI snapshots.
- Done when: the SPA is served in Docker Compose, a signed-in API call works through the BFF, and CI builds and tests the SPA and its image.

### Phase 1 — Public pages

- Home, project list, project detail, not-found and error pages.
- Meta tags for the public pages.
- Done when: the project list and detail match the [page specs](page-specs.md), pass axe, and the project E2E scenarios pass.

### Phase 2 — Sign-in and profile

- GitHub sign-in and sign-out, session handling with silent refresh, profile page, publish-a-project flow.
- Done when: the authentication and profile E2E scenarios pass.

### Phase 3 — Admin

- Admin passkey sign-in and registration, admin home, donor organization CRUD.
- Done when: admin E2E scenarios pass, including the first-claim registration path.

### Phase 4 — Hardening and deployment

- Deploy the front-end to Azure (Bicep templates in `deploy/`).
- Update documentation: move the status of these documents to `Current`, update [Current Overview](../current/overview.md), the Arc42 sections listed in the ADR, and the glossary.

After Phase 3, new platform features (donations, contributors, reports) are built only in the new front-end, using [page-specs.md](page-specs.md).

## Completion checklist

A developer ticks an item only when it is verified in the new front-end.

- [ ] Home page, anonymous and signed-in variants
- [ ] Project list with paging
- [ ] Project detail, including unpublish for the owner
- [ ] GitHub sign-in, callback, sign-out
- [ ] New-user welcome message
- [ ] Silent token refresh
- [ ] Profile page, and the not-signed-in message
- [ ] Admin passkey sign-in
- [ ] Admin first-claim registration
- [ ] Admin home, visible only to the `admin` role
- [ ] Donor organization list, add, edit, delete
- [ ] Error and not-found pages
- [ ] All `data-testid` values preserved
- [ ] No serious or critical axe violations on any page
- [ ] Docker Compose and local start scripts (`scripts/start-local.ps1`) work with the new setup
- [ ] Production deployment tested (Bicep templates in `deploy/` updated if needed)

## What to keep, change, and remove

| Item | Action |
| --- | --- |
| Backend APIs and their OpenAPI snapshots | Keep unchanged (the allow-list for login redirects needs the new origin) |
| `CookieAuthorizationHandler`, `RefreshTokenForwardingHandler` | Keep and reuse in the BFF |
| Admin CORS policy in the Authorization API | Review once the BFF is in place; it may no longer be needed once everything is same-origin |

## Risks

| Risk | Mitigation |
| --- | --- |
| Cookie and bearer mismatch blocks direct API calls | BFF design above; validate in Phase 0 with a real authenticated call before building pages |
| Passkey Relying Party domain differs from the new origin | Configure `AdminPasskeyOptions.ServerDomain` for the new origin; test the ceremony in Phase 0 or early Phase 3 (ADR-011) |
| Scope creep into redesigning the product | Phases follow the page specs; new features wait for Phase 3 |
| Planned pages built against non-existent APIs | Only build pages whose endpoints exist; see the "Planned" markers |
| Arc42 and ADR-005 text conflicts with the new stack | Update them as part of the ADR (see ADR-012 follow-up list) |
