# ADR-002 — Use MongoDB as the primary data store

**Date**: 2025
**Status**: Accepted

## Context

The platform needs a data store that can handle evolving schemas as the domain model grows, and must be straightforward to run locally in a Docker container.

## Decision

MongoDB is used as the primary data store for all services. Each bounded context owns its own MongoDB database.

## Consequences

**Positive**: Schema flexibility suits a rapidly evolving domain. Strong .NET driver support. Easy to run locally with Docker. Azure Cosmos DB for MongoDB API provides a managed cloud option.

**Negative**: Joins across contexts must be done in application code, not the database. Transactions are supported but more complex than in relational databases.
