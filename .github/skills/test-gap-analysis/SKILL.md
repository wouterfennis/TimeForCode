---
name: test-gap-analysis
description: Analyses the TimeForCode test suite to identify untested public methods, missing negative-path tests, and bounded contexts lacking architecture tests. Produces a prioritised gap report. Use after implementing a feature, as part of release readiness, or when the maintenance agent runs its hygiene pass.
---

# Test Gap Analysis Skill

This skill produces a structured report of test coverage gaps in the **TimeForCode** repository. It does not write or modify any files. Follow every step in order.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `maintenance` |
| Status | `active` |
| Overlap risk | `release-readiness` (release-readiness delegates coverage verification here) |
| Review cadence | `per-release` |

---

## Trigger Conditions

Invoke this skill when:

- A new feature has been implemented and test coverage needs verification
- The Maintenance agent runs its hygiene pass
- The Release Manager agent runs pre-release checks
- A contributor requests a test gap report

---

## Required Inputs

| Input | Source |
|-------|--------|
| Authenticated `dotnet` SDK | Verified before running any command |
| Repository root path | Working directory |

---

## Expected Outputs

- A prioritised gap report with three sections: Critical, Warning, Info
- A `GAP_FREE` or `GAPS_FOUND` decision returned to the calling agent

---

## Step 1 — Run the Full Test Suite

```powershell
dotnet test TimeForCode.sln --no-build --logger "console;verbosity=normal" 2>&1
```

Record:

- Total tests, passed, failed, skipped
- Names of any failing tests (these are pre-existing failures, not gaps)

If the build is not current, run:

```powershell
dotnet build TimeForCode.sln --no-incremental 2>&1
```

---

## Step 2 — Inventory Public Methods Without Tests

For each bounded context (`Authorization`, `Donation`, `Shared`):

1. List all public methods in handlers and validators under `src/`:

```powershell
Get-ChildItem -Recurse -Filter "*.cs" -Path src | Select-String -Pattern "public .* Handle\(|public .* Validate\(" | Select-Object Path, Line
```

1. List all test method names in the corresponding test project:

```powershell
Get-ChildItem -Recurse -Filter "*.cs" -Path tst | Select-String -Pattern "\[Fact\]|\[Theory\]" -Context 0,1 | Select-Object Path, Context
```

1. For each public handler or validator method, check whether there is a test method whose name starts with the method name.

Record any public method that has **no matching test method** as a `MISSING_TEST`.

---

## Step 3 — Check for Negative-Path Tests

For each handler or validator that has at least one test, verify that at least one test method name ends with `_ReturnsFailure`, `_ThrowsException`, or `_Invalid`.

If a class has only positive-path tests, record it as `MISSING_NEGATIVE_PATH`.

---

## Step 4 — Check Architecture Test Coverage

List all bounded contexts under `src/`:

```powershell
Get-ChildItem -Directory -Path src | Select-Object -ExpandProperty Name
```

For each bounded context, check whether a corresponding `*.Architecture.Tests` project exists under `tst/`:

```powershell
Get-ChildItem -Recurse -Filter "*.Architecture.Tests.csproj" -Path tst | Select-Object -ExpandProperty FullName
```

Record any bounded context that has no architecture test project as `MISSING_ARCH_TESTS`.

---

## Step 5 — Check Reqnroll Step Coverage

For each Gherkin scenario in `*.feature` files:

```powershell
Get-ChildItem -Recurse -Filter "*.feature" -Path tst | Select-String -Pattern "Scenario:" | Select-Object Path, Line
```

For each scenario title found, search for a matching step definition class:

```powershell
Get-ChildItem -Recurse -Filter "*Steps.cs" -Path tst | Select-String -Pattern "<scenario keyword>" | Select-Object Path
```

Record any scenario with no matching step definition as `MISSING_STEP_DEFINITION`.

---

## Step 6 — Produce the Gap Report

```markdown
## Test Gap Analysis Report

**Date:** <ISO date>
**Decision:** GAP_FREE / GAPS_FOUND

---

### 🔴 Critical Gaps

These gaps mean a production code path has zero test coverage.

| Bounded Context | Class | Method | Gap type |
|----------------|-------|--------|----------|
| Authorization | RegisterUserHandler | Handle | MISSING_TEST |
| … | … | … | … |

---

### 🟡 Warning Gaps

These gaps indicate missing negative-path or edge-case coverage.

| Bounded Context | Class | Gap type |
|----------------|-------|----------|
| Donation | RegisterProjectHandler | MISSING_NEGATIVE_PATH |
| … | … | … |

---

### 🔵 Info Gaps

These gaps are low-risk but should be addressed in a maintenance pass.

| Bounded Context | Gap type | Detail |
|----------------|----------|--------|
| Website | MISSING_ARCH_TESTS | No architecture test project found |
| … | … | … |

---

### Summary

- Critical: N
- Warning: N
- Info: N

[If GAP_FREE:] ✅ No test gaps found.
```

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `dotnet` not found | Stop and report .NET SDK is not installed |
| No `.feature` files found | Skip Step 5 and record as info: "No Gherkin scenarios defined yet" |
| `Get-ChildItem` not available | Use `find` as fallback for file listing |
