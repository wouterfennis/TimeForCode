---
name: release-readiness
description: Runs a pre-release checklist for the TimeForCode project. Verifies that the build is clean, all tests pass, open critical issues are resolved, documentation is current, and the changelog is up to date. Use before tagging a release or merging a release branch.
---

# Release Readiness Skill

This skill validates that the repository is in a releasable state. Follow every step in order. Do not proceed to the next step if a blocking check fails.

---

## Inventory Metadata

| Field | Value |
|-------|-------|
| Owner | `release-manager` |
| Status | `active` |
| Overlap risk | `changelog-update` (changelog verification is delegated to that skill) |
| Review cadence | `per-release` |

---

## Trigger Conditions

Invoke this skill when:

- A release branch is being prepared for merge to `main`
- The Release Manager agent is running a pre-release validation pass
- A team member requests a release readiness report

---

## Required Inputs

| Input | Source |
|-------|--------|
| Target version string (e.g., `1.2.0`) | Provided by the calling agent or user |
| Target branch name | Provided by the calling agent or user |

---

## Expected Outputs

- A structured release readiness report
- A `READY` or `BLOCKED` decision with a list of blocking items

---

## Step 1 — Verify the Build

```powershell
dotnet build TimeForCode.sln --no-incremental 2>&1
```

**Pass condition:** Exit code 0, zero errors.

**If blocked:** Record `BUILD_FAILED` and list all compiler errors. Stop further steps that depend on compilation.

---

## Step 2 — Run the Full Test Suite

```powershell
dotnet test TimeForCode.sln --no-build --logger "console;verbosity=normal" 2>&1
```

**Pass condition:** Exit code 0, 0 failures, 0 errors.

Record:

- Total tests run
- Passed / Failed / Skipped counts
- Names of any failing tests

**If blocked:** Record `TESTS_FAILED` and list failing test names.

---

## Step 3 — Check for Open Critical Issues

```powershell
gh issue list --label "critical" --state open --json number,title,url
```

**Pass condition:** Zero open issues with the `critical` label.

**If blocked:** Record `OPEN_CRITICAL_ISSUES` and list each issue number, title, and URL.

---

## Step 4 — Check for Unresolved TODO(review) Markers

```powershell
grep -r "TODO(review)" src/ --include="*.cs" -n
```

**Pass condition:** Zero matches.

**If any found:** Record `UNRESOLVED_TODOS` and list each file path and line number.

---

## Step 5 — Verify Documentation Currency

Run the `doc-align` skill to verify that all current-state documentation matches the code.

If the doc-align skill reports any inaccuracies, record `DOCS_STALE` and list the affected files.

---

## Step 6 — Verify the Changelog

Run the `changelog-update` skill in verification mode (no writes) to confirm that `CHANGELOG.md` has an entry for the target version.

**Pass condition:** An entry for the target version exists.

**If blocked:** Record `CHANGELOG_MISSING` and note the target version.

---

## Step 7 — Compose the Release Readiness Report

```markdown
## Release Readiness Report

**Version:** <target version>
**Branch:** <branch>
**Date:** <ISO date>
**Decision:** READY / BLOCKED

---

### Checklist

| Check | Status | Notes |
|-------|--------|-------|
| Build | ✅ / ❌ | |
| All tests pass | ✅ / ❌ | N passed, N failed |
| No open critical issues | ✅ / ❌ | |
| No unresolved TODO(review) markers | ✅ / ❌ | |
| Documentation current | ✅ / ❌ | |
| Changelog updated | ✅ / ❌ | |

---

### Blocking Items

[List each BLOCKED flag with details, or write "None — release is ready." if all checks pass.]
```

---

## Error Handling

| Symptom | Action |
|---------|--------|
| `dotnet` not found | Stop and report .NET SDK is not installed |
| `gh` not authenticated | Stop and instruct user to run `gh auth login` |
| `grep` not available | Use `Select-String` on PowerShell as fallback |
| doc-align skill fails | Record `DOCS_ALIGN_ERROR` and continue with remaining checks |

---

## Stop Conditions

Stop and return `BLOCKED` if:

- The build fails (`BUILD_FAILED`)
- Any test fails (`TESTS_FAILED`)
- Open critical issues exist (`OPEN_CRITICAL_ISSUES`)

Continue and report but do not block release for:

- Unresolved TODO(review) markers (warning only)
- Documentation gaps that are marked as known debt
