# ADR-005 — Store tokens in HttpOnly cookies, not localStorage

**Date**: 2025
**Status**: Accepted

## Context

JWTs must be stored in the browser after login. The choices were localStorage, sessionStorage, or HttpOnly cookies.

## Decision

Tokens are stored exclusively in HttpOnly, Secure cookies. The access token and refresh token are both set as HttpOnly cookies by the Authorization API after a successful login.

## Consequences

**Positive**: Tokens cannot be accessed by JavaScript, preventing XSS-based token theft. Cookies are automatically sent with requests to the same origin.

**Negative**: Requires careful `SameSite` and CORS configuration. CSRF protection must be considered for state-changing API calls: for the React SPA the BFF requires the `X-Requested-With` header on them ([ADR-012](0012-react-spa-frontend.md#amendment-2026-11-08-the-bff-replaces-nginx)).

**Follow-up for the React SPA (#113)**: the SPA cannot read the cookies, so it cannot tell who is signed in from them. The BFF answers `GET /api/session` with the claims of the validated access token (login, avatar, admin role) and never the token itself. The GitHub callback goes to the front-end's address, so the cookies belong to the site the user is on.
