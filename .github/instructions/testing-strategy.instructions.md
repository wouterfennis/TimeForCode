---
applyTo: "**"
---

# Testing Strategy Instructions

This file defines the testing approach, conventions, and quality gates for the **TimeForCode** project. All agents and contributors must follow these rules when writing, modifying, or reviewing tests.

---

## Test Project Layout

| Layer | Test project | Location |
|-------|-------------|----------|
| Authorization API | `TimeForCode.Authorization.Api.Tests` | `tst/Authorization/TimeForCode.Authorization.Api.Tests/` |
| Authorization Infrastructure | `TimeForCode.Authorization.Infrastructure.Tests` | `tst/Authorization/TimeForCode.Authorization.Infrastructure.Tests/` |
| Authorization Architecture | `TimeForCode.Authorization.Architecture.Tests` | `tst/Authorization/TimeForCode.Authorization.Architecture.Tests/` |
| Donation API | `TimeForCode.Donation.Api.Tests` | `tst/Donation/TimeForCode.Donation.Api.Tests/` |
| Donation Architecture | `TimeForCode.Donation.Architecture.Tests` | `tst/Donation/TimeForCode.Donation.Architecture.Tests/` |
| Shared | `TimeForCode.Shared.Tests` | `tst/Shared/TimeForCode.Shared.Tests/` |

---

## Test Types and Scope

### Unit Tests

- **Location:** `*.Api.Tests` and `*.Application.Tests` projects
- **Scope:** Single class in isolation; all dependencies are mocked or stubbed
- **No I/O:** No file system access, no HTTP calls, no database access
- **Framework:** xUnit + Moq

### Integration Tests

- **Location:** `*.Infrastructure.Tests` projects
- **Scope:** A component and its real dependencies (database, external HTTP)
- **Must use:** Test containers or in-memory fakes; never production credentials
- **Framework:** xUnit + test containers

### Architecture Tests

- **Location:** `*.Architecture.Tests` projects
- **Scope:** Enforce layer boundaries and naming conventions using NetArchTest
- **Rule set must cover:**
  - Domain layer has no outward dependencies
  - Application layer does not import Infrastructure or API
  - All handlers follow `<Verb><Noun>Handler` naming

### Acceptance Tests (Reqnroll)

- **Location:** `*.AcceptanceTests` projects (to be created per bounded context as scenarios are added)
- **Scope:** Full feature scenarios from the Gherkin feature files
- **Step definition conventions:**
  - Subject personas: "The user", "The external platform", "The time for code platform"
  - No HTTP verbs, status codes, class names, or method signatures in step text
  - Step class naming: `<Feature>Steps`

---

## Naming Conventions

### Test method names

All test method names **must** follow the pattern:

```text
<MethodUnderTest>_<Condition>_<ExpectedBehaviour>
```

Examples:

- `Handle_ValidCommand_ReturnsSuccess`
- `Validate_EmptyTitle_ReturnsFailure`
- `RegisterProject_DuplicateId_ThrowsConflict`

### Test class names

```text
<ClassUnderTest>Tests
```

---

## Coverage Requirements

### Minimum gate (enforced in CI)

| Test type | Minimum pass rate |
|-----------|------------------|
| Unit tests | 100 % of committed tests pass |
| Integration tests | 100 % of committed tests pass |
| Architecture tests | 100 % of committed tests pass |

There is no numeric line-coverage requirement today, but every new public method introduced in a feature branch must have at least one positive-path unit test and one negative-path unit test.

### New feature requirement

When implementing a GitHub Issue, the implementation **must** include:

- At least one unit test per new handler
- At least one unit test per new validator (if applicable)
- At least one Reqnroll step definition per Gherkin scenario

---

## Failure Handling

### Build failure

If `dotnet build TimeForCode.sln --no-incremental` exits non-zero:

1. Read all compiler errors from the output
2. Fix every error before running tests
3. Do not post an implementation log until the build is clean

### Test failure

If `dotnet test TimeForCode.sln --no-build` exits non-zero:

1. Identify all failing test names and failure messages
2. Fix each failure before proceeding
3. If a test failure is pre-existing (not caused by the current branch), document it in the implementation log's Loose Ends section with a link to the failing test

### Flaky tests

If a test fails intermittently:

1. Do not commit a `[Ignore]` or `[Skip]` attribute without a tracking comment `// TODO(review): flaky – <reason>`
2. Add the skipped test to the Loose Ends section of the implementation log

---

## Test Data

- All test data must be defined inline or in a `TestData/` folder within the test project
- No shared mutable test state between tests
- Database seeds for integration tests must be deterministic (fixed GUIDs or known values)

---

## Quality Gates Summary

| Gate | Command | Pass condition |
|------|---------|----------------|
| Build | `dotnet build TimeForCode.sln --no-incremental` | Exit code 0 |
| Unit + Integration tests | `dotnet test TimeForCode.sln --no-build` | Exit code 0, 0 failures |
| Architecture tests | Included in above | All rule assertions pass |
| No skipped tests without comment | `grep -r "\[Skip\]\|\[Ignore\]" tst/` | Zero results, or all have `TODO(review):` comment |

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `team` |
| Status | `active` |
| Overlap risk | `none` |
| Review cadence | `per-release` |
