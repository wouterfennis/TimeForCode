# ADR-018 — Reporting reads Donation over HTTP with the caller's token

**Date**: 2026-11-01
**Status**: Accepted

## Context

The Reporting context (issue #80) turns the donations and contributions of a donor organization into an impact report and a contribution history export. The data belongs to the Donation context. Reporting needs a way to read it that keeps the bounded contexts isolated ([ADR-010](0010-bounded-context-entity-duplication.md), [Shared project coupling boundary](../shared-project-coupling-boundary.md)) and does not create a second place that decides who may see an organization's data.

Options considered:

1. **Read the Donation MongoDB collections directly** — simple, but couples Reporting to the Donation storage model and duplicates the organization access check.
2. **Host the reports inside the Donation API** — no new service, but mixes a read-only reporting concern into the Donation context.
3. **Reporting calls the Donation API with its own service credential** — Reporting would have to repeat the access check, or every caller would see everything the service may see.
4. **Reporting calls the Donation API and forwards the caller's own bearer token.**

## Decision

Reporting is a separate, stateless API service with no database. Its Infrastructure layer reads from the Donation API over HTTP (the generated `TimeForCode.Donation.Api.Client`, base address `DonationApiOptions:BaseUri`) and forwards the caller's own access token on every call. Reporting never writes to the Donation API.

- **The Donation API decides who may read.** It applies the organization access check (affiliated contributors and administrators) to the forwarded token. Reporting passes the `401`, `403` and `404` answers on and maps any other Donation failure to `502`.
- **One token, two audiences.** The Authorization API lists both the Donation and the Reporting API as audiences of user and admin tokens (`TokenCreationOptions__Audiences__1` and `__2`).
- **Only the Donation client in Infrastructure.** An architecture test limits references to the Donation client to `TimeForCode.Reporting.Infrastructure`; Domain and Application see only the `IDonationDataSource` port.
- **Lazy configuration check.** A missing or invalid `BaseUri` fails the first request that needs the Donation API, not the start of the service, because the OpenAPI build step starts the host.

## Consequences

**Positive**:

- There is a single access-control decision for organization data, in the context that owns it, and Reporting cannot show more than the caller may read.
- Reporting and Donation stay independently deployable and testable; Reporting tests use an in-memory fake of the port.
- No shared database, no migration coupling and no new credential to manage.

**Negative**:

- Reporting is only as available and as fast as the Donation API, and each report costs one or more HTTP calls.
- The caller's token is sent to whatever `DonationApiOptions:BaseUri` points at, so that setting must be trusted configuration.
- Report totals inherit the limits of the Donation read endpoints: the lists are capped (DEBT-21), so totals can be incomplete, and organization membership is self-declared (DEBT-19).
- Reporting cannot run without a user or admin token; there is no scheduled or service-to-service report. That would need a new decision.
