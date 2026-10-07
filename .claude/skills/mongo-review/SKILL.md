---
name: mongo-review
description: "Checklist for new or changed MongoDB collections, queries and indexes in Infrastructure code. Use when adding a repository, a collection, a filter or a sort."
allowed-tools: Read, Grep, Glob
---

# Mongo review

Apply to every new/changed repository in `src/<Module>/TimeForCode.<Module>.Infrastructure/`.

1. **Entity**: derives from the existing `DocumentEntity` pattern; collection name defined once, following existing naming; no leaking of Mongo types into Application/Domain.
2. **Queries**: typed `Builders<T>` filters only, never string-built filters; user input is never concatenated. Projections used for list endpoints; no unbounded `Find` (limit/paging).
3. **Indexes**: every filter/sort field on a growing collection has an index created at startup in the existing index-setup place; unique constraints for natural keys (and a handled duplicate-key failure returned as `Result<T>.Failure`).
4. **Writes**: idempotent where retried; `ReplaceOne` vs `UpdateOne` chosen deliberately; no read-modify-write races on counters (use `$inc`).
5. **Tests (TDD)**: Infrastructure test for each new query or index written first; deterministic data with fixed GUIDs; no production connection strings.
6. **Registration**: repository registered in `AddInfrastructureLayer` only.
7. **Docs**: new collection recorded in `docs/current/` data notes and arc42 chapter 05/08 when it adds a pattern.
