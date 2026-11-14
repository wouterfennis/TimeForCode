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

## Amendment (2026-11-14, #193): no token outside a cookie, and each cookie only where it is needed

Three places undid the `HttpOnly` protection and are closed:

- Sign-in and refresh responses (also the admin passkey login) carry only `expiresAfter` and `isNewUser`, never a token.
- The refresh token cookie has the `Path` of the session endpoints (refresh and logout, `AuthenticationOptions:RefreshCookiePath`), so the browser sends it to those two routes only. The path is the public one the browser sees, which behind the BFF is the BFF's prefix; browsers match it case-sensitively.
- The BFF forwards no `Cookie` header to the Donation API and only the `RefreshToken` cookie to the Authorization API. It turns the `AccessToken` cookie into the bearer header.

Under [ADR-014](0014-no-backwards-compatibility-before-release.md) the refresh and logout routes moved in place to `.../authentication/session/refresh` and `.../session/logout`; existing sessions' refresh cookies (path `/`) are not sent to the new routes, so every user signs in again once (data reset). A corrupt refresh cookie counts as none: refresh answers `400`, logout `204`. The follow-up #194 moves the session to the BFF and removes this cookie code from the Authorization API.
