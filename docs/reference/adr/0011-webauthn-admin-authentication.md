# ADR-011 — WebAuthn passkey authentication for the admin role

**Date**: 2026-09-01
**Status**: Accepted

## Context

The platform defines an `Admin` role (`docs/target/authorization-and-roles.md`) but had no mechanism to create or authenticate as an administrator. The platform is single-operator: exactly one person will ever hold the admin role, and once claimed it never needs to be reassigned or shared.

A traditional username/password credential carries risks (credential stuffing, phishing, password reset flows) that are unnecessary for a single-operator role. WebAuthn (passkey) authentication with a device-bound platform authenticator is phishing-resistant, passwordless, and inherently tied to one physical device — a good match for this threat model.

Three implementation options were considered for the WebAuthn ceremony itself:

1. **A third-party FIDO2 library** (e.g. `Fido2NetLib`) — full control over the ceremony, but another dependency to track and a larger API surface to secure.
2. **Full adoption of ASP.NET Core Identity** (`AddIdentityCore<TUser>().AddEntityFrameworkStores<...>()` etc.) — gives passkeys "for free" via `SignInManager`/`UserManager`, but requires an EF Core user store, which conflicts with ADR-0001/0002 (internal JWT + MongoDB, no ASP.NET Core Identity/EF Core).
3. **ASP.NET Core's built-in passkey primitives used directly** — `IPasskeyHandler<TUser>` performs the WebAuthn ceremony (challenge validation, origin/RP-ID checks, signature verification, COSE public-key parsing) without requiring `SignInManager`/`UserManager` to own credential storage. The Microsoft Learn passkeys documentation explicitly describes implementing `IPasskeyHandler<TUser>` directly as the supported approach "when resolving users from a source other than `UserManager<TUser>`".

## Decision

Use ASP.NET Core's built-in passkey primitives (`IPasskeyHandler<TUser>`, `IdentityPasskeyOptions`, `PasskeyUserEntity`, and related ceremony types from `Microsoft.AspNetCore.Identity`, part of the `Microsoft.AspNetCore.App` shared framework) directly, bypassing `SignInManager`/`UserManager` for anything except the minimal `IUserStore<TUser>`/`IUserPasskeyStore<TUser>` surface the ceremony API requires. Credential storage stays in MongoDB (`AdminCredentialRepository`), consistent with ADR-0002.

### Isolation boundary

The admin passkey flow is a self-contained slice that can be swapped or removed later without touching the GitHub OAuth flow:

- `IPasskeyHandler<TUser>` is referenced in exactly one class, `PasskeyCeremonyService` (Infrastructure). Everything else — commands, handlers, controller, repository — depends only on our own `IPasskeyCeremonyService` seam and its own DTOs (`PasskeyCeremonyOptionsResult`, `AdminAttestationOutcome`, `AdminAssertionOutcome`). If the underlying library is ever replaced, only this one class and its DI registration change.
- A new `AdminAuthenticationController` under `/api/v1/admin-authentication/...` — not an extension of `AuthenticationController`.
- New, isolated namespaces/folders: `Commands.Admin`, `Application.Handlers.Admin`, `Application.Interfaces.Admin`, `Domain.Entities.AdminCredential`, `Infrastructure.Persistence.Database.Admin`, `Infrastructure.Services.Admin`.
- A separate `admin-auth` rate-limit policy and a standalone `AdminPasskeyOptions` configuration section.
- An additive-only `ITokenService.GenerateInternalToken(string userId, string role, string scope)` overload. The existing single-argument overload, and every GitHub-flow caller (`LoginHandler`, `CallbackHandler`, `LogoutHandler`, `RefreshHandler`), is unchanged.
- The only genuine reuse is three narrow, already-generic pieces of infrastructure: the RSA signing key, the HttpOnly-cookie mechanism (`CookieConstants`, ADR-0005), and the refresh-token store (keyed by an opaque `userId` string with no notion of "GitHub user").

### First-claim registration

Registration is available only while no admin credential exists. A bootstrap secret (configuration value) guards who may register, re-validated at both the start and completion of the ceremony. The credential is persisted via an atomic MongoDB insert against a fixed, well-known document id (`AdminCredential.SingletonId`) — MongoDB's unique `_id` constraint arbitrates a concurrent-registration race so only the first successful write claims the role, without any application-level locking.

### Accepted risk: no default attestation validation

ASP.NET Core's passkey implementation does not validate attestation statements by default (it verifies the ceremony's cryptographic integrity — challenge, origin, signature — but not the authenticator's attestation certificate chain). For a single-operator MVP this is an accepted risk: the bootstrap secret and the narrow registration window (only while unclaimed) are the primary defenses against an unauthorized device claiming the role, not attestation trust. This should be revisited if the admin role is ever extended to a multi-operator or higher-assurance scenario.

### Relying Party configuration

`AdminPasskeyOptions.ServerDomain` (mapped to `IdentityPasskeyOptions.ServerDomain`) is explicitly configured to the front-end's origin domain — where the browser actually calls `navigator.credentials` — rather than left to infer from the Authorization API's own request host header, since the front-end and API run on different hosts/subdomains.

## Consequences

**Positive**:

- No new third-party dependency; the ceremony is implemented entirely on top of the shared ASP.NET Core framework already referenced by the API.
- The admin slice can be deleted or replaced without touching any GitHub-flow class.
- Credential storage stays in MongoDB, consistent with the platform's established persistence pattern (ADR-0002).

**Negative**:

- `TimeForCode.Authorization.Infrastructure` needed a `<FrameworkReference Include="Microsoft.AspNetCore.App" />` even though it otherwise uses plain `Microsoft.NET.Sdk` — a minor deviation from that project's previous "no ASP.NET Core framework dependency" shape, scoped to the `Services/Admin`/`Persistence/Database/Admin` folders.
- A minimal `IUserStore<TUser>`/`IUserPasskeyStore<TUser>` adapter (`AdminUserStore`) is required to satisfy `UserManager<TUser>`, even though there is no real multi-user store behind it — a small amount of ceremony required purely to interoperate with the framework API shape.
- No default attestation validation is an accepted, explicitly-documented risk rather than a silent gap (see above).

## Amendment (2026-11-14, #196): the ceremony runs through the BFF and keeps no cookie

The admin passkey login follows the model of [ADR-019](0019-browser-session-in-the-bff.md):

- **No cookie at the Authorization API.** The ceremony state (the encrypted challenge state of `IPasskeyHandler`) is stored server-side in the MongoDB collection `CeremonyState`: the hash of an opaque ceremony id, the kind (registration or authentication), the state and an expiry (`AdminPasskeyOptions:CeremonyLifetimeInSeconds`, default 300 seconds, with a TTL index). The options call returns the ceremony id; the completing call sends it back. The state is taken with one atomic delete, so a ceremony works once, on any instance. A captured ceremony id together with a captured assertion cannot be replayed: the second use finds nothing. A state of the registration kind cannot complete a sign-in. The state is consumed even when the ceremony then fails.
- **Tokens go to the BFF only.** The completing endpoints return the access and refresh token in the body; the BFF opens the same session as for the GitHub sign-in and the browser only gets the opaque `__Host-session` cookie. The browser reaches the ceremony through `/bff/admin/...`, same-origin, so the admin CORS policy and `AllowCredentials` are removed from the Authorization API.
- **Signature counter.** The handler compares the counter of the assertion with the stored one and rejects the sign-in when it did not increase (a cloned authenticator or a replayed assertion). Authenticators that keep no counter report 0 every time (synced passkeys); 0 on both sides is accepted.
- `authentication/options` is now a `POST`, because it writes state.

Under ADR-014 the cookies, the CORS policy and the `RefreshCookiePath` setting were removed in place; the admin signs in again once.
