# ADR-010 — Keep GithubEntity and DocumentEntity per bounded context

**Date**: 2026-06-26
**Status**: Accepted

## Context

Both the Authorization and Donation bounded contexts contain a `DocumentEntity` base class that carries a MongoDB `[BsonId]` field. The Donation context additionally defines a `GithubEntity` base class for domain objects that carry a GitHub reference URI.

The two `DocumentEntity` implementations are byte-for-byte identical. This raises the question of whether the duplicated classes should be consolidated into the `TimeForCode.Shared` project to avoid repetition.

Options considered:

1. **Move both base classes to `TimeForCode.Shared`** — single definition, no duplication.
2. **Keep both base classes per bounded context** — each context owns its own definition; changes are isolated.

## Decision

Keep `GithubEntity` and `DocumentEntity` defined independently in each bounded context that needs them.

Rationale:

- **Bounded context isolation is a first-class principle.** Each context must be deployable, testable, and evolvable without depending on shared domain types. A shared base class in `TimeForCode.Shared` would create a coupling point that silently breaks this isolation.
- **The duplication is intentional, not accidental.** Although the implementations are currently identical, the Authorization and Donation contexts have different evolutionary trajectories. The Authorization `DocumentEntity` exists purely to satisfy MongoDB serialization for account data. The Donation `DocumentEntity` may acquire Donation-specific concerns (e.g. audit timestamps, tenant identifiers) in future iterations without those changes bleeding into Authorization.
- **The `TimeForCode.Shared` project is limited to cross-cutting infrastructure middleware** (see [Shared Project Coupling Boundary](../shared-project-coupling-boundary.md)). Domain base classes are not infrastructure middleware and do not belong there.
- **Shared domain types are a recognised bounded-context anti-pattern.** When a shared kernel is introduced, all teams sharing it must coordinate on every change. For a platform that may grow to more bounded contexts, this coordination cost is unacceptable.

## Consequences

**Positive**:

- Each bounded context can evolve its persistence base class at its own pace without risk of regression in the other context.
- No domain type crosses the bounded-context boundary; Authorization and Donation remain independently deployable and testable.
- Architecture tests in both contexts can continue to enforce that their respective Domain layers have no outward dependencies.

**Negative**:

- Two identical files must be maintained. If a genuinely cross-cutting concern is added to the base class, the change must be applied in both contexts.
- Code-review tools may flag the duplication as a smell; reviewers should reference this ADR as the accepted justification.
