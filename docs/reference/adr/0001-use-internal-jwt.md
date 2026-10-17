# ADR-001 — Use internal JWT instead of forwarding GitHub tokens

**Date**: 2025
**Status**: Accepted

## Context

TimeForCode uses GitHub OAuth 2.0 for authentication. After login, downstream services (e.g. the Donation API) need to authenticate requests from the front-end. The question was whether to forward the GitHub access token to downstream services or issue an internal token.

## Decision

The Authorization API issues its own internal JWT (RS256-signed) after a successful GitHub login. Downstream services validate this internal token. The GitHub access token is never forwarded outside the Authorization API.

## Consequences

**Positive**: Downstream services are decoupled from GitHub. Adding a second identity provider (e.g. Google) requires no changes to downstream services. Internal claims (roles, user ID) are controlled by the platform, not GitHub.

**Negative**: An additional token issuance step adds complexity to the login flow. The Authorization API becomes a trust anchor that must be kept secure.

## Update 2026-10-17: token decisions since this ADR

The decision stands; the hardening of issue #108 settled the details below.

- **GitHub token**: requested with no scope, used once for the profile and never stored ([ADR-013](0013-no-stored-github-tokens.md)).
- **Refresh token**: opaque, random, valid for 7 days and kept only as a SHA-256 hash. Every refresh rotates it; presenting a rotated token again deletes all refresh tokens of that user. Records leave the database through a TTL index at expiry.
- **Login state**: durable, single-use and protected by PKCE ([ADR-015](0015-durable-single-use-login-state.md)).
- **Signing keys**: the service fails to start without a signing key outside Development. Tokens carry a `kid`, the JWKS endpoint publishes the current and the previous key, and a rotation keeps tokens signed with the previous key valid until they expire.
- **Scopes**: user tokens carry scope `user` plus the `login` and `avatar_url` claims; the admin session carries role and scope `admin`.
- **Service calls**: when a user deletes the account, the Authorization API calls the Donation API to anonymise the contributor. It signs a short-lived token itself with sub `authorization-service`, role `service` and a **new scope `service`**, accepted only by the Donation policy `ApiService`. The `admin` scope was not reused: an admin token must not be able to rewrite contributors, and a leaked service token must not open the admin endpoints.
- **Account deletion**: removes the profile and all refresh tokens. An access token that was issued before the deletion stays valid until its expiry (60 minutes) but finds no profile; see [personal data](../personal-data.md) and risk RISK-07 in chapter 11.
