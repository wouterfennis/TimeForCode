# Architecture Decision Records

This folder contains Architecture Decision Records (ADRs) for the TimeForCode platform. Each ADR captures the context, the decision made, and the consequences of that decision.

ADRs are referenced from [Arc42 Section 09 — Architecture Decisions](../architecture/arc42/09-architecture-decisions.md).

---

## Index

| ID | Title | Status |
| --- | --- | --- |
| [ADR-001](#adr-001) | Use internal JWT instead of forwarding GitHub tokens | Accepted |
| [ADR-002](#adr-002) | Use MongoDB as the primary data store | Accepted |
| [ADR-003](#adr-003) | Use Arc42 for architecture documentation | Accepted |
| [ADR-004](#adr-004) | Use MediatR for command dispatch | Accepted |
| [ADR-005](#adr-005) | Store tokens in HttpOnly cookies, not localStorage | Accepted |
| [ADR-006](#adr-006) | Use Result&lt;T&gt; for expected failures, exceptions for programming errors | Accepted |

---

## ADR Template

When adding a new ADR, use this structure:

```markdown
## ADR-NNN — Title

**Date**: YYYY-MM-DD
**Status**: Proposed | Accepted | Deprecated | Superseded by ADR-NNN

### Context
What is the situation or problem that requires a decision?

### Decision
What decision was made?

### Consequences
What are the positive and negative consequences of this decision?
```

---

## ADR-001

**Date**: 2025
**Status**: Accepted

### Context

TimeForCode uses GitHub OAuth 2.0 for authentication. After login, downstream services (e.g. the Donation API) need to authenticate requests from the Website. The question was whether to forward the GitHub access token to downstream services or issue an internal token.

### Decision

The Authorization API issues its own internal JWT (RS256-signed) after a successful GitHub login. Downstream services validate this internal token. The GitHub access token is never forwarded outside the Authorization API.

### Consequences

**Positive**: Downstream services are decoupled from GitHub. Adding a second identity provider (e.g. Google) requires no changes to downstream services. Internal claims (roles, user ID) are controlled by the platform, not GitHub.

**Negative**: An additional token issuance step adds complexity to the login flow. The Authorization API becomes a trust anchor that must be kept secure.

---

## ADR-002

**Date**: 2025
**Status**: Accepted

### Context

The platform needs a data store that can handle evolving schemas as the domain model grows, and must be straightforward to run locally in a Docker container.

### Decision

MongoDB is used as the primary data store for all services. Each bounded context owns its own MongoDB database.

### Consequences

**Positive**: Schema flexibility suits a rapidly evolving domain. Strong .NET driver support. Easy to run locally with Docker. Azure Cosmos DB for MongoDB API provides a managed cloud option.

**Negative**: Joins across contexts must be done in application code, not the database. Transactions are supported but more complex than in relational databases.

---

## ADR-003

**Date**: 2026
**Status**: Accepted

### Context

The platform needs an architecture documentation approach that can serve both human developers and AI agents building the system over time. Documentation must be structured, discoverable, and maintained alongside code.

### Decision

Arc42 is used as the architecture documentation template. All 12 Arc42 sections are maintained in `docs/architecture/arc42/`. Architecture changes require updating the relevant section(s) before merging.

### Consequences

**Positive**: Standardised structure that is well-known in the industry. Provides a clear home for every type of architecture information. AI agents can reliably navigate a known template.

**Negative**: Requires discipline to keep sections up to date. Empty or stale sections are worse than no documentation.

---

## ADR-004

**Date**: 2025
**Status**: Accepted

### Context

The Application layer needs a way to dispatch commands and decouple the API layer from the implementation of business logic without introducing a full event-driven architecture.

### Decision

MediatR is used for in-process command dispatch. Each operation is modelled as a command (e.g. `LoginCommand`) handled by a corresponding handler. No event sourcing or external messaging is used at this stage.

### Consequences

**Positive**: Clean separation between intent (command) and execution (handler). Handlers are independently testable. Easy to add cross-cutting concerns (logging, validation) as pipeline behaviours.

**Negative**: In-process only; does not support distributed scenarios. If distributed commands are needed in future, MediatR would need to be complemented with a message broker.

---

## ADR-005

**Date**: 2025
**Status**: Accepted

### Context

JWTs must be stored in the browser after login. The choices were localStorage, sessionStorage, or HttpOnly cookies.

### Decision

Tokens are stored exclusively in HttpOnly, Secure cookies. The access token and refresh token are both set as HttpOnly cookies by the Authorization API after a successful login.

### Consequences

**Positive**: Tokens cannot be accessed by JavaScript, preventing XSS-based token theft. Cookies are automatically sent with requests to the same origin.

**Negative**: Requires careful `SameSite` and CORS configuration. CSRF protection must be considered for state-changing API calls.

---

## ADR-006

**Date**: 2026
**Status**: Accepted

### Context

The application layers use two error-signalling approaches: `Result<T>` (a discriminated-union value type carrying either a success value or an error message) and C# exceptions. Without a documented rule, callers cannot predict which pattern a method uses, and error paths are tested inconsistently.

### Decision

- **Use `Result<T>` for expected, domain-level failure cases** — situations that are part of the normal business flow and that a caller must explicitly handle (e.g. "refresh token not found", "repository already published", "identity provider unavailable").
- **Use exceptions for programming errors and invariant violations** — situations that indicate a bug or an unexpected system state that no reasonable caller can recover from at runtime (e.g. `InvalidOperationException` when OAuth state is missing after it was verified to exist earlier in the same flow).

The boundary is: if a well-behaved caller can encounter this condition during normal operation, use `Result<T>`. If the condition should only arise due to a bug or misconfiguration, throw an exception.

### Consequences

**Positive**: Callers of Application-layer methods can see from the return type whether they need to handle an expected failure. `Result<T>` failures are logged as warnings; exceptions are treated as bugs and propagate to the global error handler.

**Negative**: Requires discipline to apply the rule consistently on every new method. Mixed usage in older code must be cleaned up over time.
