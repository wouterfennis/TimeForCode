# Arc42 Section 9 — Architecture Decisions

Status: Current

This section is an index of Architecture Decision Records (ADRs) for the TimeForCode platform. Each ADR captures the context, decision, and consequences of a significant architecture choice.

ADR documents live in [docs/reference/adr/](../../reference/adr/README.md).

---

## Decision Index

| ID | Title | Status | Date |
| --- | --- | --- | --- |
| [ADR-001](../../reference/adr/README.md#adr-001) | Use internal JWT instead of forwarding GitHub tokens | Accepted | 2025 |
| [ADR-002](../../reference/adr/README.md#adr-002) | Use MongoDB as the primary data store | Accepted | 2025 |
| [ADR-003](../../reference/adr/README.md#adr-003) | Use Arc42 for architecture documentation | Accepted | 2026 |
| [ADR-004](../../reference/adr/README.md#adr-004) | Use MediatR for command dispatch | Accepted | 2025 |
| [ADR-005](../../reference/adr/README.md#adr-005) | Store tokens in HttpOnly cookies, not localStorage | Accepted | 2025 |

---

## How to Add a New ADR

1. Create a new file in `docs/reference/adr/` named `NNNN-short-title.md` where `NNNN` is the next sequential number.
2. Use the ADR template in [docs/reference/adr/README.md](../../reference/adr/README.md).
3. Add the ADR to the index table above.
4. Link the ADR from the relevant Arc42 section(s) that the decision affects.
