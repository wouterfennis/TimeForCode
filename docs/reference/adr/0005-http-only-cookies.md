# ADR-005 — Store tokens in HttpOnly cookies, not localStorage

**Date**: 2025
**Status**: Accepted

## Context

JWTs must be stored in the browser after login. The choices were localStorage, sessionStorage, or HttpOnly cookies.

## Decision

Tokens are stored exclusively in HttpOnly, Secure cookies. The access token and refresh token are both set as HttpOnly cookies by the Authorization API after a successful login.

## Consequences

**Positive**: Tokens cannot be accessed by JavaScript, preventing XSS-based token theft. Cookies are automatically sent with requests to the same origin.

**Negative**: Requires careful `SameSite` and CORS configuration. CSRF protection must be considered for state-changing API calls.
