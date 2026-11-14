# Shared Project Coupling Boundary

**Owner**: team
**Status**: Active
**Review cadence**: per-release

---

## Purpose

This document defines the formal coupling boundary for `TimeForCode.Shared`. It specifies what the project may contain, what it must not contain, and the process that governs additions.

---

## What the Shared Project May Contain

`TimeForCode.Shared` is limited to **cross-cutting ASP.NET Core infrastructure** that is consumed by more than one bounded context's API or API client project and that has no domain-level knowledge.

Permitted categories:

| Category | Example |
| --- | --- |
| API pipeline extensions | `UseDefaultApiPipeline`, `AddTrustedForwardedHeaders`, `UseSecurityHeaders` |
| Strongly-typed token models shared by the APIs | `AccessToken` |
| Generic pipeline behaviours (MediatR) | `ValidationBehaviour<,>` |
| ASP.NET Core extension methods for common service registration | `AddSharedInfrastructure()` |

---

## What the Shared Project Must Not Contain

The following types of code must **never** be placed in `TimeForCode.Shared`:

| Prohibited category | Reason |
| --- | --- |
| Domain entities or value objects | Domain types belong to the bounded context that owns them. Shared domain types couple contexts and undermine bounded-context isolation (see [ADR-010](adr/0010-bounded-context-entity-duplication.md)). |
| Application commands, queries, or handlers | Business logic belongs in the Application layer of a specific context. |
| Repository interfaces or infrastructure implementations | These belong to the Application or Infrastructure layer of a specific context. |
| Context-specific configuration options classes | Options classes that reference a single context's settings must live in that context. |
| Feature flags or business rules | Business rules belong to the domain and application layers of the owning context. |

---

## Process for Adding to the Shared Project

Any addition to `TimeForCode.Shared` must pass all four of the following checks before it is committed:

1. **Consumer check** — the candidate type must be actively consumed by two or more bounded contexts or services. Types consumed by only one context must live in that context.
2. **Domain-free check** — the candidate type must contain no domain concepts (entity identifiers, business rules, domain events). If it mentions a bounded-context concept by name, it does not belong in Shared.
3. **Framework-only check** — the candidate type depends only on framework or infrastructure libraries (ASP.NET Core, `System.*`, MediatR interfaces), never on a domain or application assembly.
4. **ADR or issue reference** — the addition is accompanied by either an ADR or a GitHub Issue that records the rationale. A brief comment in the PR body linking to the ADR or Issue is sufficient.

If any check fails, the type stays in the bounded context that needs it, or a new ADR is raised to reconsider the boundary.

---

## Current Contents (as of 2026-11-14)

| Type | Layer | Consumed by |
| --- | --- | --- |
| `ForwardedHeadersExtensions` | API (service registration) | Authorization, Donation, Reporting APIs and the front-end BFF |
| `AccessToken` model | API (model) | Authorization API response models |
| `ValidationBehaviour<,>` | Application pipeline | Authorization Application, Donation Application |

All current contents satisfy the domain-free and framework-only checks.

---

## Rationale

The Shared project exists because the API pipeline (security headers, forwarded headers, rate limiting, OpenAPI defaults) is structurally identical across the APIs. Duplicating it in every API would introduce maintenance risk without any isolation benefit, since it has no domain knowledge and changes only when the cross-cutting policy changes.

This boundary is intentionally narrow. If the Shared project grows beyond cross-cutting infrastructure middleware, it risks becoming a "shared kernel" that couples bounded contexts — an anti-pattern in a context-first architecture.
