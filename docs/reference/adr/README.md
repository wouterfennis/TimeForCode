# Architecture Decision Records

This folder contains Architecture Decision Records (ADRs) for the TimeForCode platform. Each ADR captures the context, the decision made, and the consequences of that decision.

ADRs are referenced from [Arc42 Section 09 — Architecture Decisions](../architecture/arc42/09-architecture-decisions.md).

---

## Index

| ID | Title | Status |
| --- | --- | --- |
| [ADR-001](0001-use-internal-jwt.md) | Use internal JWT instead of forwarding GitHub tokens | Accepted |
| [ADR-002](0002-use-mongodb.md) | Use MongoDB as the primary data store | Accepted |
| [ADR-003](0003-use-arc42.md) | Use Arc42 for architecture documentation | Accepted |
| [ADR-004](0004-use-mediatr.md) | Use MediatR for command dispatch | Accepted |
| [ADR-005](0005-http-only-cookies.md) | Store tokens in HttpOnly cookies, not localStorage | Accepted |
| [ADR-006](0006-reqnroll-bdd.md) | Use Reqnroll + BDD for acceptance tests | Accepted |
| [ADR-007](0007-swagger-snapshot-gate.md) | Swagger snapshot gate for API contract review | Accepted |
| [ADR-008](0008-exclude-e2e-from-ci.md) | Exclude E2E tests from CI | Accepted |
| [ADR-009](0009-result-type-error-handling.md) | Use Result&lt;T&gt; for expected failures, exceptions for programming errors | Accepted |
| [ADR-010](0010-bounded-context-entity-duplication.md) | Keep GithubEntity and DocumentEntity per bounded context | Accepted |

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

Use the index above to open each ADR in its own file.
