# Arc42 Section 8 — Crosscutting Concepts

Status: Mixed

This section describes concepts, patterns, and rules that apply across all bounded contexts in TimeForCode.

---

## Authentication and Authorization

All API endpoints (except public project listings and the login flow) require a valid internal JWT. The token is issued by the Authorization API and validated by each downstream service independently.

**Token validation rules** (all services):

1. Verify the RS256 signature against the Authorization API's JWKS endpoint. The key is selected by the token's `kid` header; the JWKS lists the current and the previous signing key so a rotation does not invalidate live tokens.
2. Verify the token has not expired (`exp` claim).
3. Verify the `iss` (issuer) matches the Authorization API base URL.
4. Verify the `aud` (audience) includes this service's URL.
5. Extract role claims and apply policy-based authorization.

**Token storage**: HttpOnly, Secure cookies only. Never localStorage or sessionStorage.

**Refresh tokens**:

- Only a SHA-256 hash of the token is persisted (`TokenHash`); the readable value exists only in the cookie.
- Each refresh rotates the token. The old record is kept and marked `RotatedAt` through a conditional update, so exactly one of two concurrent requests wins the rotation.
- Presenting a rotated token again (replay, or the losing side of a race) fails and deletes all refresh tokens of that user, which ends every session of the user.
- Records are removed by a MongoDB TTL index on `ExpiresAfter` (stored as a BSON date), including rotated ones.
- `refresh` and `logout` are POST only. Logout expires both cookies with the same cookie attributes they were set with.
- The admin session (user id `admin`, no account record) refreshes to a token with role and scope `admin`; every other user gets scope `user`.
- CSRF: the state-changing endpoints rely on `SameSite=Strict` cookies; there is no separate anti-forgery token.
- Test-only admin access: `POST /api/v1/test-admin-access/token` with `{ "secret": "..." }` returns an admin token so live tests can drive admin actions without a passkey. It answers `404` unless `TestAdminAccess:Enabled` is `true` (default `false`), `401` for a wrong secret (constant-time comparison). Startup throws when it is enabled outside the `Development` and `Test` environments or with a secret shorter than 16 characters. Only `docker-compose.yaml` enables it (secret from `TEST_ADMIN_ACCESS_SECRET`, with a local default); `deploy/` runs as `Production` and never sets it.

See [authorization and roles](../../target/authorization-and-roles.md) for the full role and permission model.

---

## Bounded Context Layering

Every bounded context follows the same dependency direction:

```text
Domain  ←  Application  ←  Infrastructure  ←  API
```

- **Domain**: pure C#, no framework dependencies, no external packages. Contains entities, value objects, and domain rules.
- **Application**: orchestrates domain operations; defines interfaces for infrastructure; contains MediatR command and query handlers.
- **Infrastructure**: implements interfaces defined in Application; contains database repositories, external HTTP clients, and key loading.
- **API**: ASP.NET Core controllers that dispatch MediatR commands; no business logic in controllers.

Architecture tests in both the Authorization and Donation contexts enforce these rules on every CI build. The Shared project coupling boundary (what it may and may not contain) is formally documented in [docs/reference/shared-project-coupling-boundary.md](../../reference/shared-project-coupling-boundary.md). The decision to keep `GithubEntity` and `DocumentEntity` per bounded context rather than moving them to Shared is recorded in [ADR-010](../../reference/adr/0010-bounded-context-entity-duplication.md).

---

## Error Handling

All API errors are returned as `ProblemDetails` (RFC 7807 format):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The githubUrl field is required."
}
```

Business rule violations (e.g. pledging hours to a non-active project) return `400 Bad Request` with a descriptive detail message. Authorization failures return `401 Unauthorized` or `403 Forbidden`. Not-found responses return `404 Not Found`.

Domain exceptions should not propagate to the API layer as unhandled exceptions. Each handler is responsible for mapping domain errors to appropriate HTTP responses.

MediatR handlers return a `Result<T>` when an operation has a response payload. Commands such as donor-organization deletion, which succeed without a payload, return the non-generic `Result`; the API maps a successful payload-free result to `204 No Content`.

---

## Input Limits and the OpenAPI Contract

Limits on request input (paging bounds, string lengths, identifier and name patterns, e-mail and URL formats) are declared as validation attributes (`Range`, `StringLength`, `MaxLength`, `RegularExpression`, `EmailAddress`, `Url`) on controller parameters and request models, not as hand-written checks in the action. `[ApiController]` enforces them and answers `400` with validation problem details, and the OpenAPI generator turns them into `minimum`, `maximum`, `minLength`, `maxLength`, `pattern` and `format`, so generated clients see the same contract.

- Shared limit values and patterns live in the `ApiConstraints` class of each API project; the Donation API wraps the object-id pattern in an `[ObjectId]` attribute.
- `RegularExpressionAttribute` accepts a value only when the match spans the whole string, so a trailing newline cannot slip past a `$` anchor.
- `TimeForCode.Shared` adds what the generator omits (`OpenApiTransformers`): `format: email` for `[EmailAddress]` and a `429 Too Many Requests` response with a `Retry-After` header on every rate-limited operation. The limiter sets that header on rejection.
- A change to a limit changes the Swagger verified snapshot, which is reviewed before it is accepted (skill `swagger-snapshot-review`).

---

## Configuration Management

Configuration follows the ASP.NET Core options pattern:

- Each service reads strongly-typed options classes (e.g. `TokenCreationOptions`, `DbOptions`).
- In local development, values are supplied via Docker Compose environment variables.
- In production, values are supplied via Azure App Service Application Settings or Key Vault references.

**Sensitive values** (client secrets, signing keys, database credentials) must not be committed to source control. They are:

- Injected as environment variables in `docker-compose.yaml` for local use only.
- Stored in Azure Key Vault for production (target).

---

## Logging

All services use the ASP.NET Core `ILogger<T>` abstraction. Structured logging with log levels:

| Level | Use |
| --- | --- |
| `Debug` | Development-only detail; disabled in production |
| `Information` | Normal operation events (user logged in, project registered) |
| `Warning` | Unexpected but recoverable conditions |
| `Error` | Failures that require attention; always include exception details |

> **Target**: Centralised log aggregation (e.g. Azure Monitor / Application Insights) should be configured for production.

---

## API Versioning

All Donation API endpoints are versioned with a URL prefix (`/api/v1/`). The Authorization API does not currently use versioning but should adopt `/api/v1/` in a future refactor to be consistent.

---

## Testing

Each bounded context must have:

1. **Architecture tests** — enforce layering rules using ArchUnitNET.
2. **Unit tests** — cover domain logic and application handlers.
3. **Integration tests** — exercise API endpoints end-to-end.
4. **Acceptance / specification tests** — BDD-style tests for critical user flows.

Local test doubles (mocks, stubs, in-memory databases) are used in unit and integration tests. The Identity Provider Mock service provides a reusable test double for the full OAuth 2.0 flow.

---

## Third-Party Tokens and Data Protection

The GitHub access token is used once to fetch the profile and is then discarded; it is never persisted (see [ADR-013](../../reference/adr/0013-no-stored-github-tokens.md)). Public GitHub data is read with the application's own credentials, which are only sent over HTTPS, and every path segment taken from a request is validated before it is used in an outgoing URL.

ASP.NET Core Data Protection encrypts the admin passkey ceremony state that travels in a cookie. The key ring is stored in the MongoDB collection `DataProtectionKeyDocument` and uses a fixed application name, so every instance can read state written by another and keys survive restarts. A ceremony state that cannot be decrypted is rejected as a bad request, not a server error. Without a configured database connection (OpenAPI document generation, specifications) the framework's default local key ring is used.

The OAuth login state is durable and single-use (see [ADR-015](../../reference/adr/0015-durable-single-use-login-state.md)). Each login stores a record in the MongoDB collection `StateEntry` with a random key, the redirect URI, a PKCE code verifier and a 10-minute expiry; a TTL index removes expired records. The callback consumes the record atomically, so a state works once on any instance, and the token exchange sends the PKCE `code_verifier` that matches the `code_challenge` of the authorize request.

## Personal Data and Account Deletion

The stored personal data fields, their purpose and retention are listed in [personal data](../../reference/personal-data.md). A user removes the account with `DELETE /api/v1/user`. The Authorization API first calls the Donation API (`POST /api/v1/contributor/{userId}/anonymise`) with a service token of scope `service`, which replaces the contributor and the identity snapshot on the contributions with a generic deleted user and keeps the hours. Only then it deletes the refresh tokens and the account, so a Donation failure leaves everything in place and the deletion can be retried. Contributors are found by their platform user id, which the anonymisation replaces, therefore the contributions are rewritten before the contributor. Remaining risks are listed in chapter 11 (RISK-07 to RISK-10).

---

## Mermaid Diagram Convention

All architecture and workflow diagrams in this documentation use Mermaid. Every diagram must be accompanied by surrounding prose that explains intent, inputs, outputs, and key decision points. Diagrams alone are not sufficient.

See [docs/README.md](../../README.md) for the full authoring rules.
