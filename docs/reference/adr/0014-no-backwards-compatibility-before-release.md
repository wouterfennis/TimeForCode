# ADR-014 — No backwards compatibility before the first production release

**Date**: 2026-10-16
**Status**: Accepted

## Context

TimeForCode has no production deployment and no external consumers. Keeping legacy data shapes, deprecated endpoints, migration code and compatibility shims alive costs effort and hides the intended design. Issue #104 is the first case: the refresh-token hardening replaces the stored token value with a hash and changes `refresh` and `logout` from GET to POST, and carrying the old shapes along would only add dead code.

## Decision

Until the first production release, changes do not preserve backwards compatibility:

- Data shapes may change without migration code. Existing local or test data is dropped or recreated, and a breaking change lists that cleanup in its release or issue notes.
- API contracts may change in place without a new version, deprecation period or dual support. The Swagger snapshot review (ADR-007) still applies, and the diff must show the change is intended.
- Code kept only for compatibility (shims, fallbacks, old field names) is removed instead of retained.

This ADR is superseded by a new ADR at the first production release, which introduces a compatibility policy.

## Consequences

**Positive**: Smaller, simpler code. The design follows the requirements, not history.

**Negative**: Environments with existing data (local databases, any shared test environment) must be reset after a breaking change. For example, the `RefreshToken` collection must be dropped after #104, because old documents have no `TokenHash` and break the unique index.
