# ADR-009 — Use Result&lt;T&gt; for expected failures, exceptions for programming errors

**Date**: 2026
**Status**: Accepted

## Context

The application layers use two error-signalling approaches: `Result<T>` (a discriminated-union value type carrying either a success value or an error message) and C# exceptions. Without a documented rule, callers cannot predict which pattern a method uses, and error paths are tested inconsistently.

## Decision

- **Use `Result<T>` for expected, domain-level failure cases** — situations that are part of the normal business flow and that a caller must explicitly handle (e.g. "refresh token not found", "repository already published", "identity provider unavailable").
- **Use exceptions for programming errors and invariant violations** — situations that indicate a bug or an unexpected system state that no reasonable caller can recover from at runtime (e.g. `InvalidOperationException` when OAuth state is missing after it was verified to exist earlier in the same flow).

The boundary is: if a well-behaved caller can encounter this condition during normal operation, use `Result<T>`. If the condition should only arise due to a bug or misconfiguration, throw an exception.

## Consequences

**Positive**: Callers of Application-layer methods can see from the return type whether they need to handle an expected failure. `Result<T>` failures are logged as warnings; exceptions are treated as bugs and propagate to the global error handler.

**Negative**: Requires discipline to apply the rule consistently on every new method. Mixed usage in older code must be cleaned up over time.
