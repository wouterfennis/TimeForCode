# Arc42 Section 8 — Crosscutting Concepts

Status: Mixed

This section describes concepts, patterns, and rules that apply across all bounded contexts in TimeForCode.

---

## Authentication and Authorization

All API endpoints (except public project listings and the login flow) require a valid internal JWT. The token is issued by the Authorization API and validated by each downstream service independently.

**Token validation rules** (all services):

1. Verify the RS256 signature against the Authorization API's JWKS endpoint.
2. Verify the token has not expired (`exp` claim).
3. Verify the `iss` (issuer) matches the Authorization API base URL.
4. Verify the `aud` (audience) includes this service's URL.
5. Extract role claims and apply policy-based authorization.

**Token storage**: HttpOnly, Secure cookies only. Never localStorage or sessionStorage.

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

Architecture tests in the Authorization context enforce these rules on every CI build. The Donation context does not yet have architecture tests — this is a known gap tracked in [DEBT in Section 11](11-risks-and-technical-debt.md).

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

## Mermaid Diagram Convention

All architecture and workflow diagrams in this documentation use Mermaid. Every diagram must be accompanied by surrounding prose that explains intent, inputs, outputs, and key decision points. Diagrams alone are not sufficient.

See [docs/README.md](../../README.md) for the full authoring rules.
