# ADR-017 — Test-only admin access on the local stack

**Date**: 2026-10-21
**Status**: Accepted

## Context

Admin actions (approve, request changes) require the admin session, and the only way to obtain it is a WebAuthn passkey ceremony ([ADR-011](0011-webauthn-admin-authentication.md)). A passkey cannot be driven against a running stack by a script, so the admin happy path could never be tested live. Issue #139 asks for a test-only way to get an admin token that cannot be enabled in production.

## Decision

The Authorization API has a test-only endpoint, `POST /api/v1/test-admin-access/token`, that returns an admin access token for a shared secret.

- **Off by default.** `TestAdminAccess:Enabled` defaults to `false`. When it is off the endpoint answers `404`; an empty or unparseable value counts as off.
- **Fails closed.** At startup the API throws if the feature is enabled in any environment other than `Development` or `Test` (allow-list, ordinal match), or if the secret is shorter than 16 characters.
- **Contained token.** The token carries the admin identity plus the claim `amr=test`, lasts at most 15 minutes and is issued without a refresh token or cookies, so a test session is recognisable and cannot be renewed.
- **Guarded secret.** The secret is compared in constant time; refused attempts are logged with the caller address; the endpoint is hidden from the OpenAPI document.
- **Enabled for the local stack only.** `docker-compose.yaml` turns it on only when `TEST_ADMIN_ACCESS_SECRET` is set (`scripts/start-local.ps1` generates one) and binds the Authorization API port to loopback. `deploy/` runs as `Production` and never sets the flag.

Alternatives rejected: a scripted virtual WebAuthn authenticator (heavy and fragile in CI), and a seeded admin credential (still needs the private key to sign assertions).

## Consequences

**Positive**: The admin happy path can be tested against a running stack, with a bounded, identifiable token and no production code path that can be switched on by configuration alone.

**Negative**: A test backdoor exists in the code base; its safety rests on the startup guard and the compose and deploy configuration. Nothing downstream rejects `amr=test` tokens, which is acceptable only while the feature cannot be enabled in production.
