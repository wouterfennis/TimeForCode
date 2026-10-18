# ADR-016 — Allow MongoDB.Bson types in Domain and Application

**Date**: 2026-10-18
**Status**: Accepted

## Context

The onion layering is enforced by ArchUnitNET: Domain depends on nothing, Application depends on Domain only, and Infrastructure implements the interfaces of Application. A strict reading of that rule also keeps persistence technology out of Domain and Application.

Today both layers use the `MongoDB.Bson` package:

- Domain: the `DocumentEntity` base class of each bounded context (see [ADR-010](0010-bounded-context-entity-duplication.md)) and entities such as `Project`, `RefreshToken` and `StateEntry` use `ObjectId` as their identity, and a few properties carry `[BsonId]`, `[BsonRepresentation]` or `[BsonIgnoreExtraElements]`.
- Application: repository interfaces and some handlers and services (`IContributorRepository`, `IContributionRepository`, `IRefreshTokenRepository`, `LoginHandler`) pass `ObjectId` values.

Options considered:

1. **Remove all MongoDB types from Domain and Application** — typed ids in Domain, mapping and class maps in Infrastructure. Purest, but it needs an id strategy, a registry of BSON class maps and changes in every entity, repository and test.
2. **Allow `MongoDB.Bson`, forbid `MongoDB.Driver`** — the entities stay plain classes that the driver can map, while queries, filters, indexes and client types stay in Infrastructure.

## Decision

Take option 2 for now. This is a documented exception to the onion rule, not a general licence:

- Domain and Application may use `MongoDB.Bson` (`ObjectId` and the mapping attributes of the existing `DocumentEntity` pattern).
- Domain and Application must not use `MongoDB.Driver` (collections, filters, update definitions, clients). Application reaches the database only through repository interfaces.
- Domain must not use `Microsoft.AspNetCore` or `MediatR`.

ArchUnitNET tests in `Authorization.Architecture.Tests` and `Donation.Architecture.Tests` enforce the three limits (`DomainLayer_WithReferencesToMongoDbDriver_AreNotAllowed`, `DomainLayer_WithReferencesToWebFrameworkOrMediator_AreNotAllowed`, `ApplicationLayer_WithReferencesToMongoDbDriver_AreNotAllowed`).

State of entities stays encapsulated: lifecycle properties use `init` or a private setter and change only through methods of the entity (for example `Project.Approve()`, `RefreshToken.MarkRotated()`). The driver can still populate them when reading a document.

## Consequences

**Positive**:

- No mapping layer or duplicate persistence models; the entities are stored as they are.
- The boundary that matters in practice (driver, queries, indexes) stays in Infrastructure and is guarded by tests.

**Negative**:

- Domain identity is tied to MongoDB's `ObjectId`. Moving to another store, or to typed ids, touches entities, repository interfaces, handlers and tests.
- Reviewers may flag `using MongoDB.Bson` in Domain or Application; they should refer to this ADR.

**Follow-up**: replacing `ObjectId` with typed domain ids (mapped in Infrastructure) is tracked as DEBT-18 in [arc42 chapter 11](../../architecture/arc42/11-risks-and-technical-debt.md). Because of [ADR-014](0014-no-backwards-compatibility-before-release.md) it needs no data migration before the first release.
