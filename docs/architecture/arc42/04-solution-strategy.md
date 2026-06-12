# Arc42 Section 4 — Solution Strategy

Status: Mixed

This section summarises the fundamental decisions and strategies that shape the architecture of TimeForCode.

---

## Technology Decisions

| Concern | Decision | Rationale |
| --- | --- | --- |
| Backend language | .NET / C# | Strong typing, mature ecosystem, team expertise |
| Frontend | Blazor (WebAssembly or Server) | Allows sharing domain types with the backend; avoids a separate TypeScript stack |
| Storage | MongoDB | Document model accommodates evolving schemas; well-supported in .NET |
| Authentication | Internal JWT over OAuth 2.0 (GitHub) | Decouples internal auth from external provider; allows future provider additions without changing downstream services |
| Infrastructure | Azure App Service + Docker containers | Portable container images; cost-effective for the current scale |
| IaC | Azure Bicep | Native Azure IaC; better tooling support than ARM templates |
| CI/CD | GitHub Actions | Already integrated with the repository; free for open-source projects |
| Code quality | SonarCloud | Automated quality gate on every pull request |

---

## Architectural Approach

### Domain-Driven Design

The platform is structured around bounded contexts, each with its own domain model, application layer, infrastructure layer, and API. Contexts do not share databases or domain models. Cross-context communication goes through APIs, not direct database access.

Current bounded contexts:

- **Authorization** — identity, authentication, token issuance.
- **Donation** — projects, donations, hour tracking, organisations.
- **Website** — Blazor frontend (drives both contexts via API clients).

### CQRS-Lite with MediatR

Commands and queries are dispatched via MediatR handlers. This separates the intent of an operation from its execution and makes testing straightforward. Full event sourcing is not used.

### Layering

Each bounded context follows this dependency direction:

```text
Domain ← Application ← Infrastructure ← API
```

The domain has no external dependencies. Infrastructure implements interfaces defined in the Application layer. Architecture tests enforce these rules on every build.

### Security Strategy

- Authentication is centralised in the Authorization API. No other service issues tokens.
- All services validate tokens against the Authorization API's JWKS endpoint.
- Tokens are stored in HttpOnly cookies to prevent XSS.
- Secrets are not committed to source control; they are supplied via environment variables in local development and via Azure Key Vault in production (target).

### Resilience Strategy

> **Target**: The Donation API must handle GitHub API unavailability gracefully. Project registration and metadata sync must not block critical donation operations if GitHub is unreachable. A timeout and fallback strategy should be implemented.

---

## Key Design Decisions

| Decision | Record |
| --- | --- |
| Use internal JWT instead of passing GitHub tokens to downstream services | [ADR-001](../../reference/adr/README.md) |
| Use MongoDB as the primary data store | [ADR-002](../../reference/adr/README.md) |
| Use Arc42 for architecture documentation | [ADR-003](../../reference/adr/README.md) |
