# Arc42 Section 9 — Architecture Decisions

Status: Current

This section is an index of Architecture Decision Records (ADRs) for the TimeForCode platform. Each ADR captures the context, decision, and consequences of a significant architecture choice.

ADR documents live in [docs/reference/adr/](../../reference/adr/README.md).

---

## Decision Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| [ADR-001](../../reference/adr/0001-use-internal-jwt.md) | Use internal JWT instead of forwarding GitHub tokens | Accepted | 2025 |
| [ADR-002](../../reference/adr/0002-use-mongodb.md) | Use MongoDB as the primary data store | Accepted | 2025 |
| [ADR-003](../../reference/adr/0003-use-arc42.md) | Use Arc42 for architecture documentation | Accepted | 2026 |
| [ADR-004](../../reference/adr/0004-use-mediatr.md) | Use MediatR for command dispatch | Accepted | 2025 |
| [ADR-005](../../reference/adr/0005-http-only-cookies.md) | Store tokens in HttpOnly cookies, not localStorage | Accepted | 2025 |
| [ADR-006](../../reference/adr/0006-reqnroll-bdd.md) | Use Reqnroll + BDD for acceptance tests | Accepted | 2026-06-21 |
| [ADR-007](../../reference/adr/0007-swagger-snapshot-gate.md) | Swagger snapshot gate for API contract review | Accepted | 2026-06-21 |
| [ADR-008](../../reference/adr/0008-exclude-e2e-from-ci.md) | Exclude E2E tests from CI | Accepted | 2026-06-21 |
| [ADR-009](../../reference/adr/0009-result-type-error-handling.md) | Use Result&lt;T&gt; for expected failures, exceptions for programming errors | Accepted | 2026 |

---

## How to Add a New ADR

1. Create a new file in `docs/reference/adr/` named `NNNN-short-title.md` where `NNNN` is the next sequential number.
2. Use the ADR template in [docs/reference/adr/README.md](../../reference/adr/README.md).
3. Add the ADR to the index table above.
4. Link the ADR from the relevant Arc42 section(s) that the decision affects.
