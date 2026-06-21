# ADR-007 — Swagger snapshot gate for API contract review

**Date**: 2026-06-21
**Status**: Accepted

## Context

The Authorization and Donation APIs expose an OpenAPI (Swagger) specification. Without a gate, internal refactors can silently alter the public API surface — breaking generated clients or external callers — without any code review step.

## Decision

Each `*.Api.Tests` project contains a single snapshot test (`SwaggerTests.cs`) that generates the OpenAPI JSON at test time and compares it against a committed `.verified.txt` file using the [Verify](https://github.com/VerifyTests/Verify) library. A mismatch fails the test and writes a `.received.txt` diff file.

This failure is **intentional**: it forces every API surface change to be reviewed before the snapshot is updated. The snapshot update procedure is:

```powershell
Copy-Item `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.received.txt `
  tst/<Module>/TimeForCode.<Module>.Api.Tests/SwaggerTests.Verify_GeneratedSwaggerFile_ShouldNotChangeUnlessIntended.verified.txt `
  -Force
```

The updated `.verified.txt` must be committed alongside the code change in the same pull request so the diff is reviewable.

## Consequences

**Positive**: Breaking changes to the public API surface are caught immediately in CI. Reviewers can see the exact OpenAPI diff in the pull request. The NSwag-generated client (`TimeForCode.Authorization.Api.Client`) is naturally included in the review checklist.

**Negative**: The snapshot file must be updated manually on every intentional API change. Developers unfamiliar with the pattern may be confused by a failing test that is expected to fail.
