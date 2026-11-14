# ADR-019 — The browser session lives in the BFF

**Date**: 2026-11-14
**Status**: Accepted
**Supersedes**: the cookie design of [ADR-005](0005-http-only-cookies.md) (tokens in `HttpOnly` cookies set by the Authorization API)
**Amends**: [ADR-012](0012-react-spa-frontend.md) (the BFF), [ADR-015](0015-durable-single-use-login-state.md) (a second single-use store)

## Context

ADR-005 kept the access and refresh token in `HttpOnly` cookies that the Authorization API set. That tied the token issuer to one kind of client, spread cookie knowledge over several services (cookie names, flags, a JSON value that the BFF had to unpack) and left gaps in the protection: tokens in refresh response bodies, a refresh cookie sent with every request, a forwarded `Cookie` header, and a login `state` that was not bound to the browser (login CSRF). Issue #193 closed the first three inside the old design. This decision removes the cause.

The goal: every service speaks bearer, and the browser holds one opaque session cookie that only the BFF issues.

## Decision

**The Authorization API is a plain token issuer.** For the GitHub sign-in it sets and reads no cookies. The tokens are returned in JSON to the caller that proves it started the login:

1. `GET /api/v1/authentication/login` takes a PKCE `CodeChallenge` (S256, mandatory) and stores it with the login state.
2. After GitHub confirms the user, the callback redirects to the redirect URI with a single-use `code`. No token is in the redirect.
3. `POST /api/v1/authentication/token` takes the `code` and the PKCE `codeVerifier` and returns the tokens. The code is stored as a hash, lives 60 seconds (`AuthenticationOptions:LoginCodeLifetimeInSeconds`), is taken with one atomic delete (`FindOneAndDelete`, as the login state is, ADR-015), and is consumed even when the verifier is wrong, so a guess cannot be retried.
4. `POST .../refresh` and `POST .../logout` take the refresh token in the body. An unparsable body is a `400`.

**The BFF holds the session.** `GET /bff/login` creates the verifier, keeps it server-side with a short-lived opaque `__Host-login` cookie (`HttpOnly`, `Secure`, `SameSite=Lax`, because the browser returns from another site) and redirects to the Authorization API. `GET /bff/callback` exchanges the code with that verifier, opens a session and sets the only session credential: `__Host-session` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/`, no `Domain`), an opaque random id. A code obtained in another browser fails because that browser lacks the login cookie, and a replayed or forged code is refused by the Authorization API. Every sign-in creates a new session id, so there is no session fixation.

**The BFF adds the bearer header and refreshes.** For every forwarded call it drops `Cookie` and any `Authorization` header from the browser, looks up the session and sends `Authorization: Bearer <access token>`. An access token that expires within 30 seconds is refreshed first, under a lease on the session record (see below), so the SPA never handles a token or a refresh. A rejected refresh token ends the session. The BFF removes `Set-Cookie` from every upstream response, so the session cookie is the only cookie the browser holds.

**Sign-out** is `POST /bff/logout`: it revokes the refresh token at the Authorization API, deletes the session and clears the cookie. State-changing BFF routes need the `X-Requested-With` header (ADR-012) and refuse a request whose `Origin` is not this host.

**Timeouts.** A session ends after 30 idle minutes (`Session:IdleTimeoutMinutes`) or 8 hours in total (`Session:AbsoluteLifetimeHours`), whichever comes first. A `ExpiresAt` TTL index removes the record.

### Session storage: MongoDB

The session record (hash of the session id, tokens, timestamps, refresh lease) is stored in the BFF's own MongoDB database (`DbOptions`, collections `BffSessions` and `BffLoginAttempts`).

Options considered: server memory (simple, but one instance only, lost on restart, and the refresh lock only works in-process), an encrypted cookie (no server state, but a refresh race cannot be locked, sign-out cannot truly end the session, and the cookie then carries tokens) and MongoDB. MongoDB wins because of the refresh race: the Authorization API treats a second use of a refresh token as theft and revokes all tokens of the user, so two parallel requests that both refresh would sign the user out everywhere. The BFF takes a **refresh lease** with one atomic conditional update (`RefreshLeaseUntil`); the other requests wait, then use the rotated tokens. That works across several BFF instances and survives a restart.

The tokens are stored in the clear in that database. The database already holds the refresh token hashes of the Authorization API and the same trust boundary; encrypting them would need a key ring shared by all instances. Accepted for now; see the risks below.

### The token endpoints are not public API for browsers

The BFF exposes only `/api/auth/v1/user/...` of the Authorization API; every other path under `/api/auth/` is `404`. The code exchange, refresh and logout endpoints are called by the BFF only, over the internal network. They are not authenticated with a client secret: the credential is the single-use code together with its PKCE verifier, or the refresh token itself, so an exposed endpoint is not a bypass. Deployments should still keep these three paths off the public network. (A shared client secret was considered and declined to avoid one more secret to rotate.)

The login and callback endpoints stay publicly reachable: the browser is redirected to the login, and GitHub redirects the browser to the callback.

### Admin passkey login

The admin passkey login follows the same model since #196 ([ADR-011 amendment](0011-webauthn-admin-authentication.md#amendment-2026-11-14-196-the-ceremony-runs-through-the-bff-and-keeps-no-cookie)): the ceremony state is single use and server-side, the tokens go to the BFF, and the browser only gets the session cookie.

## Consequences

### Positive

- No access or refresh token is readable by script or present in any cookie, response body or browser storage. Not even the BFF's own session cookie carries a token.
- Login CSRF is closed: the verifier is bound to the browser that started the login.
- One place knows about browser cookies. The APIs, the Reporting forwarded-token flow ([ADR-018](0018-reporting-reads-donation-over-http-with-forwarded-token.md)) and any non-browser client use plain bearer tokens.
- The SPA no longer reacts to a `401` by refreshing; the BFF refreshes silently.

### Negative and accepted risks

- **Session riding after XSS.** A script that runs on the SPA origin can still call `/api/...`, and the browser attaches the session cookie. A BFF prevents token theft, not that. Mitigations: the CSP without `unsafe-inline` or `unsafe-eval` (#195), the `Origin` and `X-Requested-With` checks, and the idle and absolute timeouts.
- **Tokens at rest.** A read of the BFF database yields working tokens until they expire or are revoked. Mitigation: the access token lives at most 15 minutes (#195); protect the database like the Authorization database.
- **A new stateful component and dependency.** The BFF now needs MongoDB. Its sessions are lost when the database is reset, which signs everyone in again once.
- **`__Host-` cookies and Safari.** `Secure` cookies work on `http://localhost` in Chrome and Firefox but not in Safari; local development in Safari needs HTTPS.
- **Sign out everywhere** is out of scope: sign-out ends one session and revokes its refresh token only.
- **A request without an `Origin` header is accepted** by the CSRF check (a browser sends `Origin` on every cross-origin request and on same-origin POSTs, so its absence is not a cross-origin script); the `X-Requested-With` header and the `Strict` cookie still apply. The origin compared is the request host, after the trusted forwarded headers.
- **No rate limit on `/bff/login` and `/bff/callback` yet.** Each login attempt is a small record with a TTL index, so growth is bounded, and the Authorization API rate-limits its own login and token endpoints. A cross-site GET to the callback can use up the pending attempt of a victim, who then signs in again; it is a nuisance, not a way in.
- **A refresh runs under a 30 second lease and the Authorization API client times out after 10 seconds**, so a slow refresh ends before another request can take the lease over; saving the rotated tokens only succeeds for the holder of the lease. An outage keeps the session and the cookie (the call goes out without a bearer token); only a rejected refresh token ends the session.

**Breaking change (ADR-014).** The GitHub sign-in flow, the refresh and logout endpoints and the response models changed in place, and the old cookie code is removed. Everyone signs in again once; local data needs `scripts/start-local.ps1 -ResetData`.
